using Microsoft.Xna.Framework;
using System.Collections.Generic;
using XenoKit.Engine.Vfx.Shape;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleConeExtrude : ParticleEmissionBase
    {
        private readonly EffectShapeMesh mesh = new EffectShapeMesh();
        private readonly List<EffectRibbonPoint> points = new List<EffectRibbonPoint>();
        private int duration;

        public override void Initialize(Matrix4x4 emitPoint, SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            points.Clear();
            duration = node.EmissionNode.ConeExtrude.Duration + Xv2CoreLib.Random.Range(0, node.EmissionNode.ConeExtrude.Duration_Variance);

            if (duration <= 0)
                duration = Lifetime;
        }

        public override void Release()
        {
            ViewportInstance.RenderSystem.RemoveRenderEntity(this);
        }

        public override void Update()
        {
            DrawThisFrame = true;
            EmissionData.Update();
            ParticleUV.Update(ParticleSystem.CurrentFrameDelta);

            StartUpdate();

            if (State == NodeState.Active)
            {
                UpdateRotation();
                UpdateScale();
                UpdateColor();
                UpdatePoints();

                Matrix4x4 world = CreateConeWorld();
                EffectShapeMeshData meshData = EffectShapeMeshBuilder.BuildConeExtrudeRibbonMesh(points, world, ParticleUV.ScrollU, ParticleUV.ScrollV, ParticleUV.StepU, ParticleUV.StepV);
                mesh.SetMeshData(meshData);
            }
            else
            {
                mesh.Clear();
            }

            UpdateChildrenNodes();
            EndUpdate();
        }

        public override void Draw()
        {
            if (!ParticleSystem.DrawThisFrame || State != NodeState.Active || (Node.NodeFlags & NodeFlags1.Hide) == NodeFlags1.Hide)
                return;

            if (!Viewport.Instance.DrawThisFrame)
                return;

            mesh.Draw(this, EmissionData.Material, EmissionData.Samplers, EmissionData.Textures, !SourceEffectPart.NoGlare);
        }

        private void UpdatePoints()
        {
            int step = Node.EmissionNode.ConeExtrude.StepDuration <= 0 ? 1 : Node.EmissionNode.ConeExtrude.StepDuration;
            int targetCount = GetTargetPointCount(step);

            points.Clear();

            int startIndex = System.Math.Max(0, Node.EmissionNode.ConeExtrude.Points.Count - targetCount);

            for (int i = startIndex; i < Node.EmissionNode.ConeExtrude.Points.Count; i++)
            {
                points.Add(CreatePoint(i));
            }
        }

        private EffectRibbonPoint CreatePoint(int index)
        {
            ConeExtrudePoint path = GetPathPoint(index);
            Color primary = new Color(PrimaryColor[0], PrimaryColor[1], PrimaryColor[2], PrimaryColor[3]);
            Color secondary = (Node.NodeFlags & NodeFlags1.EnableSecondaryColor) != 0 && (Node.NodeFlags & NodeFlags1.FlashOnGen) == 0
                ? new Color(SecondaryColor[0], SecondaryColor[1], SecondaryColor[2], SecondaryColor[3])
                : primary;

            float offset2 = Node.EmissionNode.ConeExtrude.I_08 == 1 ? -path.WorldOffsetFactor2 : path.WorldOffsetFactor2;
            float pointFactor = Node.EmissionNode.ConeExtrude.Points.Count <= 1 ? 0f : index / (float)(Node.EmissionNode.ConeExtrude.Points.Count - 1);
            float length = System.Math.Max(ScaleU, ScaleBase);
            float topWidth = ScaleU * path.WorldScaleFactor;
            float bottomWidth = ScaleBase * path.WorldScaleAdd;
            SimdVector3 position = new SimdVector3(offset2 * ScaleU, length * pointFactor, path.WorldOffsetFactor * ScaleV);
            float u = Node.EmissionNode.ConeExtrude.Points.Count <= 1 ? 0f : index / (float)(Node.EmissionNode.ConeExtrude.Points.Count - 1);

            if (Node.EmissionNode.ConeExtrude.I_08 == 1)
            {
                float swappedWidth = topWidth;
                topWidth = bottomWidth;
                bottomWidth = swappedWidth;
            }

            return new EffectRibbonPoint(position, System.Math.Max(0.0001f, topWidth), System.Math.Max(0.0001f, bottomWidth), primary, secondary, u);
        }

        private int GetTargetPointCount(int step)
        {
            int count = Node.EmissionNode.ConeExtrude.Points.Count;

            if (count == 0)
                return 0;

            if (duration <= 0)
                return count;

            float steppedFrame = (int)System.Math.Floor(CurrentFrame / step) * step;
            float factor = System.Math.Min(1f, (steppedFrame + step) / duration);
            return System.Math.Max(2, System.Math.Min(count, (int)System.Math.Floor((count - 1) * factor) + 1));
        }

        private Matrix4x4 CreateConeWorld()
        {
            if (Node.EmissionNode.BillboardType == ParticleBillboardType.None)
            {
                Matrix4x4 world = Rotation * Transform * Matrix4x4.CreateScale(ParticleSystem.Scale) * GetParticleAttachmentBone();
                return ParticleBillboard.ApplyRenderDepth(this, world, Node.EmissionNode.Texture.RenderDepth);
            }

            return ParticleBillboard.CreateWorld(this, Node.EmissionNode.BillboardType, Node.EmissionNode.VelocityOriented, GetParticleRotationAmount(), GetParticleRandomDirection(), Node.EmissionNode.Texture.RenderDepth);
        }

        private ConeExtrudePoint GetPathPoint(int index)
        {
            if (Node.EmissionNode.ConeExtrude.Points.Count == 0)
                return new ConeExtrudePoint(1f, 0f, 0f, 0f);

            return Node.EmissionNode.ConeExtrude.Points[System.Math.Min(index, Node.EmissionNode.ConeExtrude.Points.Count - 1)];
        }
    }
}
