using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using XenoKit.Engine.Pool;
using XenoKit.Engine.Vertex;
using XenoKit.Engine.Vfx.Asset;
using XenoKit.Engine.Vfx.Particle;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ETR;

namespace XenoKit.Engine.Vfx.Trace
{
    public class TracePlane : PooledEntity
    {
        public override EntityType EntityType => EntityType.VFX;

        private const int VERTEX_TOP_LEFT = 0;
        private const int VERTEX_TOP_RIGHT = 1;
        private const int VERTEX_BOTTOM_LEFT_ALT = 2;
        private const int VERTEX_BOTTOM_LEFT = 3;
        private const int VERTEX_TOP_RIGHT_ALT = 4;
        private const int VERTEX_BOTTOM_RIGHT = 5;

        protected readonly VertexPositionTextureColor[] Vertices = new VertexPositionTextureColor[6];

        private VfxTrace VfxTrace;
        private TraceNode Node;
        private ETR_File EtrFile;
        private ETR_Node EtrNode;
        private TraceSegment Segment;

        public override int AlphaBlendType
        {
            get
            {
                if (Node.EmissionData?.Material == null) return -1;
                if (Node.EmissionData.Material.MatParam.AlphaBlend == 0 || Node.EmissionData.Material.MatParam.AlphaBlendType == 3) return -1;
                return Node.EmissionData.Material.MatParam.AlphaBlendType;
            }
        }
        public override int LowRezMode
        {
            get
            {
                if (Node.EmissionData?.Material == null) return 0;
                if (Node.EmissionData.Material.MatParam.LowRez == 1) return 1;
                if (Node.EmissionData.Material.MatParam.LowRezSmoke == 1) return 2;
                return 0;
            }
        }
        
        public void Initialize(VfxTrace vfxTrace, TraceNode node, ETR_File etrFile, ETR_Node etrNode, TraceSegment segment)
        {
            Node = node;
            VfxTrace = vfxTrace;
            EtrFile = etrFile;
            EtrNode = etrNode;
            Segment = segment;
        }

        public override void ClearObjectState()
        {
            VfxTrace = null;
            EtrFile = null;
            Node = null;
            EtrNode = null;
        }

        public void Release()
        {
            ObjectPoolManager.TracePlanePool.ReleaseObject(this);
            GameBase.RenderSystem.RemoveRenderEntity(this);
        }

        public override void Update()
        {
            UpdateVertices();
        }

        public override void Draw()
        {
            DrawThisFrame = false;
            if (VfxTrace == null) return;
            if (!VfxTrace.DrawThisFrame) return;

            //Set samplers/textures
            for (int i = 0; i < Node.EmissionData.Samplers.Length; i++)
            {
                GraphicsDevice.SamplerStates[Node.EmissionData.Samplers[i].samplerSlot] = Node.EmissionData.Samplers[i].state;
                GraphicsDevice.VertexSamplerStates[Node.EmissionData.Samplers[i].samplerSlot] = Node.EmissionData.Samplers[i].state;

                if (Node.EmissionData.Textures[i] != null)
                {
                    GraphicsDevice.VertexTextures[Node.EmissionData.Samplers[i].textureSlot] = Node.EmissionData.Textures[i].Texture;
                    GraphicsDevice.Textures[Node.EmissionData.Samplers[i].textureSlot] = Node.EmissionData.Textures[i].Texture;
                }
            }

            Node.EmissionData.Material.World = AbsoluteTransform;

            //Shader passes and vertex drawing
            foreach (EffectPass pass in Node.EmissionData.Material.CurrentTechnique.Passes)
            {
                pass.Apply();

                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, Vertices, 0, 2);
            }

            base.Draw();
        }

        private void UpdateVertices()
        {
            if (Node.EmissionData.TextureIndex == -1 || VfxTrace.IsSimulating) return;

            //time value is frameSegmentCreated / ExtrudeDuration?
            //float scale = EtrNode.Scale.ETR_InterpolationType == ETR_InterpolationType.ShapeStartToEnd ? EtrNode.Scale.GetInterpolatedValue(0) : Node.Scale;

            //Set positions
            Vertices[VERTEX_TOP_LEFT].Position.X = -Node.Scale;
            Vertices[VERTEX_TOP_LEFT].Position.Y = Node.Scale;
            Vertices[VERTEX_TOP_LEFT].Position.Z = 0f;
            Vertices[VERTEX_BOTTOM_LEFT].Position.X = 0;
            Vertices[VERTEX_BOTTOM_LEFT].Position.Y = -Node.Scale;
            Vertices[VERTEX_BOTTOM_LEFT].Position.Z = 0f;

            Vertices[VERTEX_TOP_RIGHT].Position.X = Node.Scale;
            Vertices[VERTEX_TOP_RIGHT].Position.Y = Node.Scale;
            Vertices[VERTEX_TOP_RIGHT].Position.Z = 0f;
            Vertices[VERTEX_BOTTOM_RIGHT].Position.X = Node.Scale;
            Vertices[VERTEX_BOTTOM_RIGHT].Position.Y = -Node.Scale;
            Vertices[VERTEX_BOTTOM_RIGHT].Position.Z = 0f;

            Vertices[VERTEX_TOP_LEFT].Position = Vector3.Transform(Vertices[VERTEX_TOP_LEFT].Position, Segment.endPoint);
            Vertices[VERTEX_BOTTOM_LEFT].Position = Vector3.Transform(Vertices[VERTEX_BOTTOM_LEFT].Position, Segment.endPoint);

            Vertices[VERTEX_TOP_RIGHT].Position = Vector3.Transform(Vertices[VERTEX_TOP_RIGHT].Position, Segment.originPoint);
            Vertices[VERTEX_BOTTOM_RIGHT].Position = Vector3.Transform(Vertices[VERTEX_BOTTOM_RIGHT].Position, Segment.originPoint);

            //UV
            Vertices[VERTEX_TOP_LEFT].TextureUV.X = Node.ParticleUV.ScrollU;
            Vertices[VERTEX_TOP_LEFT].TextureUV.Y = Node.ParticleUV.ScrollV;
            Vertices[VERTEX_BOTTOM_LEFT].TextureUV.X = Node.ParticleUV.ScrollU;
            Vertices[VERTEX_BOTTOM_LEFT].TextureUV.Y = Node.ParticleUV.ScrollV + Node.ParticleUV.StepV;
            Vertices[VERTEX_TOP_RIGHT].TextureUV.X = Node.ParticleUV.ScrollU + Node.ParticleUV.StepU;
            Vertices[VERTEX_TOP_RIGHT].TextureUV.Y = Node.ParticleUV.ScrollV;
            Vertices[VERTEX_BOTTOM_RIGHT].TextureUV.X = Node.ParticleUV.ScrollU + Node.ParticleUV.StepU;
            Vertices[VERTEX_BOTTOM_RIGHT].TextureUV.Y = Node.ParticleUV.ScrollV + Node.ParticleUV.StepV;

            //Color
            if (EtrNode.Flags.HasFlag(ETR_Node.ExtrudeFlags.NoDegrade))
            {
                Vertices[VERTEX_TOP_LEFT].SetColor(Node.PrimaryColor);
                Vertices[VERTEX_BOTTOM_LEFT].SetColor(Node.PrimaryColor);
                Vertices[VERTEX_TOP_RIGHT].SetColor(Node.PrimaryColor);
                Vertices[VERTEX_BOTTOM_RIGHT].SetColor(Node.PrimaryColor);
            }
            else
            {
                Vertices[VERTEX_TOP_LEFT].SetColor(Node.PrimaryColor);
                Vertices[VERTEX_BOTTOM_LEFT].SetColor(Node.SecondaryColor);
                Vertices[VERTEX_TOP_RIGHT].SetColor(Node.PrimaryColor);
                Vertices[VERTEX_BOTTOM_RIGHT].SetColor(Node.SecondaryColor);
            }

            //Duplicate vertices
            Vertices[VERTEX_BOTTOM_LEFT_ALT] = Vertices[VERTEX_BOTTOM_LEFT];
            Vertices[VERTEX_TOP_RIGHT_ALT] = Vertices[VERTEX_TOP_RIGHT];
        }
    }
}
