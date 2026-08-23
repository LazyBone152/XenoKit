using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Collision;
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
        private readonly Func<Matrix4x4> getDrawMatrix;
        private readonly Func<int> getFrame;
        private readonly Func<SimdVector3> getStartRelativeMovementDelta;
        private readonly HitboxVisual hitboxVisual;

        public BsaHitboxPreview(BSA_Type3 hitbox, Func<Matrix4x4> getDrawMatrix, Func<int> getFrame, Func<SimdVector3> getStartRelativeMovementDelta)
        {
            this.hitbox = hitbox;
            this.getDrawMatrix = getDrawMatrix;
            this.getFrame = getFrame;
            this.getStartRelativeMovementDelta = getStartRelativeMovementDelta;
            hitboxVisual = new HitboxVisual(new Color(255, 255, 0, 64), Color.Yellow);

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

            hitboxVisual.Draw(Extensions.ToXna(getDrawMatrix()));
        }

        public void Dispose()
        {
            hitboxVisual.Dispose();
        }

        private void UpdateHitbox()
        {
            hitboxVisual.Clear();

            if (hitbox == null)
                return;

            Vector3 position = new Vector3(hitbox.F_08, hitbox.F_12, hitbox.F_16);

            switch (hitbox.I_00)
            {
                case 0:
                    hitboxVisual.SetSphere(position, Math.Abs(hitbox.F_20));
                    break;
                case 1:
                    SetCapsule(position, Math.Abs(hitbox.F_20));
                    break;
                case 2:
                    Vector3 halfExtents = new Vector3(
                        hitbox.F_20,
                        hitbox.F_24,
                        hitbox.F_28);
                    hitboxVisual.SetBox(position, halfExtents);
                    break;
            }
        }

        private void SetCapsule(Vector3 position, float radius)
        {
            if (BsaHitboxGeometry.UsesDistanceRelativeGeometry(hitbox))
            {
                SimdVector3 movementDelta = getStartRelativeMovementDelta?.Invoke() ?? SimdVector3.Zero;
                Vector3 movement = Extensions.ToXna(movementDelta);
                hitboxVisual.SetCapsule(position, position + movement, radius);
                return;
            }

            Vector3 start = position + new Vector3(hitbox.F_24, hitbox.F_28, hitbox.F_32);
            Vector3 end = position + new Vector3(hitbox.F_36, hitbox.F_40, hitbox.F_44);
            hitboxVisual.SetCapsule(start, end, radius);
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
