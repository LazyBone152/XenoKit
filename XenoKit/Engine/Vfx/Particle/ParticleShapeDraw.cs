using Microsoft.Xna.Framework;
using System.Collections.Generic;
using XenoKit.Engine.Vfx.Shape;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleShapeDraw : ParticleEmissionBase
    {
        private readonly EffectShapeMesh mesh = new EffectShapeMesh();

        public override void Initialize(Matrix4x4 emitPoint, SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitPoint, velocity, system, node, effectPart, effect);
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

                ShapeDrawStripMode stripMode = GetShapeDrawStripMode();
                Matrix4x4 world = CreateShapeDrawWorld(stripMode);
                List<EffectShapePoint> points = GetScaledShapePoints();

                Color primary = new Color(PrimaryColor[0], PrimaryColor[1], PrimaryColor[2], PrimaryColor[3]);
                Color secondary = (Node.NodeFlags & NodeFlags1.EnableSecondaryColor) != 0 && (Node.NodeFlags & NodeFlags1.FlashOnGen) == 0
                    ? new Color(SecondaryColor[0], SecondaryColor[1], SecondaryColor[2], SecondaryColor[3])
                    : primary;

                float depthWidth = GetShapeDrawDepthWidth(stripMode);
                EffectShapeMeshData meshData = EffectShapeMeshBuilder.BuildShapeDrawRibbonMesh(points, world, ScaleV, depthWidth, ParticleUV.ScrollU, ParticleUV.ScrollV, ParticleUV.StepU, ParticleUV.StepV, primary, secondary, ShouldClose(points), stripMode);
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

            mesh.Draw(this, EmissionData.Material, EmissionData.Samplers, EmissionData.Textures);
        }

        private ShapeDrawStripMode GetShapeDrawStripMode()
        {
            if (Node.NodeFlags2.HasFlag(NodeFlags2.Unk2) && Node.EmissionNode.BillboardType == ParticleBillboardType.Front)
                return ShapeDrawStripMode.PathNormalGroundBand;

            if (Node.NodeFlags2.HasFlag(NodeFlags2.Unk2))
                return ShapeDrawStripMode.PathNormalDepthBand;

            return Node.NodeFlags2 == 0
                ? ShapeDrawStripMode.PathNormalWidth
                : ShapeDrawStripMode.UprightWidth;
        }

        private float GetShapeDrawDepthWidth(ShapeDrawStripMode stripMode)
        {
            if (stripMode != ShapeDrawStripMode.PathNormalDepthBand && stripMode != ShapeDrawStripMode.PathNormalGroundBand)
                return 0f;

            return stripMode == ShapeDrawStripMode.PathNormalGroundBand ? ScaleV : ScaleV * 2f;
        }

        private Matrix4x4 CreateShapeDrawWorld(ShapeDrawStripMode stripMode)
        {
            if (stripMode == ShapeDrawStripMode.PathNormalGroundBand)
                return ApplyShapeDrawRenderDepth(CreateUprightYawWorldRaw());

            if (stripMode == ShapeDrawStripMode.PathNormalDepthBand)
                return ApplyShapeDrawRenderDepth(CreateRotationAxisWorldRaw());

            if (Node.EmissionNode.BillboardType != ParticleBillboardType.None)
            {
                return ParticleBillboard.CreateWorld(
                    this,
                    Node.EmissionNode.BillboardType,
                    Node.EmissionNode.VelocityOriented,
                    GetParticleRotationAmount(),
                    GetParticleRandomDirection(),
                    Node.EmissionNode.Texture.RenderDepth);
            }

            Matrix4x4 world = stripMode == ShapeDrawStripMode.PathNormalWidth
                ? CreateFullParticleWorldRaw()
                : CreateUprightYawWorldRaw();

            return ApplyShapeDrawRenderDepth(world);
        }

        private Matrix4x4 CreateUprightYawWorldRaw()
        {
            Matrix4x4 baseWorld = Transform * GetAttachmentBone();
            Matrix4x4.Decompose(Transform, out SimdVector3 scale, out _, out _);
            float angle = RandomDirection ? -RotationAmount : RotationAmount;

            return Matrix4x4.CreateRotationY(MathHelper.ToRadians(angle)) *
                   Matrix4x4.CreateScale(scale) *
                   Matrix4x4.CreateScale(ParticleSystem.Scale) *
                   Matrix4x4.CreateTranslation(baseWorld.Translation);
        }

        private Matrix4x4 CreateFullParticleWorldRaw()
        {
            return Rotation *
                   Transform *
                   Matrix4x4.CreateScale(ParticleSystem.Scale) *
                   GetParticleAttachmentBone();
        }

        private Matrix4x4 CreateRotationAxisWorldRaw()
        {
            return GetParticleRotationAxisWorld(false);
        }

        private Matrix4x4 ApplyShapeDrawRenderDepth(Matrix4x4 world)
        {
            return ParticleBillboard.ApplyRenderDepth(this, world, Node.EmissionNode.Texture.RenderDepth);
        }

        private static bool ShouldClose(IList<EffectShapePoint> points)
        {
            if (points == null || points.Count < 3)
                return false;

            EffectShapePoint first = points[0];
            EffectShapePoint last = points[points.Count - 1];
            float dx = first.X - last.X;
            float dy = first.Y - last.Y;

            return dx * dx + dy * dy <= 0.0001f;
        }

        private List<EffectShapePoint> GetScaledShapePoints()
        {
            List<EffectShapePoint> points = new List<EffectShapePoint>(Node.EmissionNode.ShapeDraw.Points.Count);

            foreach (ShapeDrawPoint point in Node.EmissionNode.ShapeDraw.Points)
                points.Add(new EffectShapePoint(point.X * ScaleU, point.Y * ScaleU));

            return points;
        }
    }
}
