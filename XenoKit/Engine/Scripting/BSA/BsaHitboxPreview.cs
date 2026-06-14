using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Scripting.BSA
{
    // Visual preview only. This does not run BDM damage, impact, collision, or hit reaction logic.
    public class BsaHitboxPreview : EngineObject, IDisposable
    {
        private readonly BSA_Type3 hitbox;
        private readonly Func<Matrix4x4> getWorldMatrix;
        private readonly Func<int> getFrame;
        private readonly Func<SimdVector3> getFrameSweepDelta;
        private readonly Cube boundingBox;

        public BsaHitboxPreview(BSA_Type3 hitbox, Func<Matrix4x4> getWorldMatrix, Func<int> getFrame, Func<SimdVector3> getFrameSweepDelta)
        {
            this.hitbox = hitbox;
            this.getWorldMatrix = getWorldMatrix;
            this.getFrame = getFrame;
            this.getFrameSweepDelta = getFrameSweepDelta;
            boundingBox = new Cube(new Vector3(0.5f), new Vector3(-0.5f), new Vector3(0.5f), 0.5f, Color.Blue, true);

            UpdateHitbox();
        }

        public override void Update()
        {
            UpdateHitbox();
        }

        public override void Draw()
        {
            if (!IsContextValid())
                return;

            boundingBox.Draw(Extensions.ToXna(getWorldMatrix()));
        }

        public void Dispose()
        {
        }

        private void UpdateHitbox()
        {
            if (hitbox == null)
                return;

            bool useDefinedBounds = (hitbox.I_00 & 0x000F) != 0;

            if (useDefinedBounds)
                SetMinMaxBounds();
            else
                boundingBox.SetBounds(Vector3.Zero, Vector3.Zero, hitbox.F_20 / 2f, false);

            boundingBox.SetPosition(new Vector3(hitbox.F_08, hitbox.F_12, hitbox.F_16));
        }

        private void SetMinMaxBounds()
        {
            Vector3 rawMin = new Vector3(hitbox.F_36, hitbox.F_40, hitbox.F_44);
            Vector3 rawMax = new Vector3(hitbox.F_24, hitbox.F_28, hitbox.F_32);
            Vector3 size = new Vector3(hitbox.F_20 / 2f);

            Vector3 adjustedMax = rawMax;

            if (hitbox.I_04 == 1)
                GrowMaxBoundsWithMovement(ref adjustedMax, getFrameSweepDelta?.Invoke() ?? SimdVector3.Zero);

            Vector3 finalMin = Vector3.Min(rawMin, adjustedMax) - size;
            Vector3 finalMax = Vector3.Max(rawMin, adjustedMax) + size;
            boundingBox.SetBounds(finalMin, finalMax, 0f, true);
        }

        private static void GrowMaxBoundsWithMovement(ref Vector3 max, SimdVector3 delta)
        {
            max += new Vector3(delta.X, delta.Y, delta.Z);
        }

        private bool IsContextValid()
        {
            return hitbox != null && IsValidForCurrentFrame() && SettingsManager.Instance.Settings.XenoKit_HitboxSimulation;
        }

        private bool IsValidForCurrentFrame()
        {
            int frame = getFrame();

            if (frame < hitbox.StartTime)
                return false;

            return hitbox.Duration == 0 || frame < (int)hitbox.StartTime + hitbox.Duration;
        }
    }
}
