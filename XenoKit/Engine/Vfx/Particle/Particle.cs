using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Vertex;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx.Particle
{
    public class Particle : ParticleNodeBase
    {
        private const int VERTEX_TOP_LEFT = 0;
        private const int VERTEX_TOP_RIGHT = 1;
        private const int VERTEX_BOTTOM_LEFT_ALT = 2;
        private const int VERTEX_BOTTOM_LEFT = 3;
        private const int VERTEX_TOP_RIGHT_ALT = 4;
        private const int VERTEX_BOTTOM_RIGHT = 5;

        private readonly VertexPositionTextureColor[] Vertices = new VertexPositionTextureColor[6];
        private readonly ParticleEmissionData EmissionData;

        private float ColorR_Variance = 0f;
        private float ColorG_Variance = 0f;
        private float ColorB_Variance = 0f;
        private float ColorA_Variance = 0f;
        private float ScaleBase_Variance = 0f;
        private float ScaleU_Variance = 0f;
        private float ScaleV_Variance = 0f;

        //Keyframed Values:
        private float ScaleBase = 0.5f;
        private float ScaleU = 0.5f;
        private float ScaleV = 0.5f;
        private float[] PrimaryColor = null;
        private float[] SecondaryColor = new float[4];

        //Texture Scroll:
        private int TextureIndex = 0;
        //private float TextureScrollU = 0f;
        //private float TextureScrollV = 0f;
        //private float TextureStepU = 1f;
        //private float TextureStepV = 1f;

        public Particle(Matrix localEmitMatrix, ParticleSystem system, ParticleNode node, EffectPart effectPart, GameBase gameBase) : base(localEmitMatrix, system, node, effectPart, gameBase)
        {
            EmissionData = CompiledObjectManager.GetCompiledObject<ParticleEmissionData>(node, gameBase);
            SetValues();
        }

        public void SetValues()
        {
            ColorR_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.R);
            ColorG_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.G);
            ColorB_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.B);
            ColorA_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.A);

            ScaleBase_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.ScaleBase_Variance);
            ScaleU_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.ScaleXY_Variance.X);
            ScaleV_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.ScaleXY_Variance.Y);

            TextureIndex = ParticleSystem.EmpFile.Textures.IndexOf(Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef);
        }

        private void UpdateColor()
        {
            PrimaryColor = Node.EmissionNode.Texture.Color1.GetInterpolatedValue(CurrentTimeFactor);
            PrimaryColor[3] = Node.EmissionNode.Texture.Color1_Transparency.GetInterpolatedValue(CurrentTimeFactor);

            PrimaryColor[0] += ColorR_Variance;
            PrimaryColor[1] += ColorG_Variance;
            PrimaryColor[2] += ColorB_Variance;
            PrimaryColor[3] += ColorA_Variance;

            if (Node.NodeFlags.HasFlag(NodeFlags1.EnableSecondaryColor))
            {
                SecondaryColor = Node.EmissionNode.Texture.Color2.GetInterpolatedValue(CurrentTimeFactor);
                SecondaryColor[3] = Node.EmissionNode.Texture.Color2_Transparency.GetInterpolatedValue(CurrentTimeFactor);

                SecondaryColor[0] += ColorR_Variance;
                SecondaryColor[1] += ColorG_Variance;
                SecondaryColor[2] += ColorB_Variance;
                SecondaryColor[3] += ColorA_Variance;
            }
        }

        private void UpdateScale()
        {
            ScaleBase = Node.EmissionNode.Texture.ScaleBase.GetInterpolatedValue(CurrentTimeFactor) + ScaleBase_Variance;

            if (Node.NodeFlags.HasFlag(NodeFlags1.EnableScaleXY))
            {
                float[] values = Node.EmissionNode.Texture.ScaleXY.GetInterpolatedValue(CurrentTimeFactor);

                ScaleU = values[0] + ScaleU_Variance;
                ScaleV = values[1] + ScaleV_Variance;
            }
            else
            {
                ScaleU = ScaleBase;
                ScaleV = ScaleBase;
            }
        }
        /*
        private void UpdateTextureScroll()
        {
            if (Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef == null) return;

            if (Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.Speed)
            {
                TextureScrollU += Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef.ScrollState.ScrollSpeed_U;
                TextureScrollV += Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef.ScrollState.ScrollSpeed_V;
                TextureStepU = 1f;
                TextureStepV = 1f;
            }
            else
            {
                //Keyframe path
                int keyframeIndex = 0;

                if (Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet)
                {
                    keyframeIndex = (int)((Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef.ScrollState.Keyframes.Count - 1) * CurrentTimeFactor);
                }

                TextureScrollU = Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef.ScrollState.Keyframes[keyframeIndex].ScrollU;
                TextureScrollV = Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef.ScrollState.Keyframes[keyframeIndex].ScrollV;
                TextureStepU = Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef.ScrollState.Keyframes[keyframeIndex].ScaleU;
                TextureStepV = Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef.ScrollState.Keyframes[keyframeIndex].ScaleV;
            }
        }
        */
        private void UpdateVertices()
        {
            if (TextureIndex == -1) return;

            UpdateScale();
            UpdateColor();
            //UpdateTextureScroll();

            float scaleU_FirstVertex = ScaleU;

            //Special case for when Scale XY is used. The first vertex still uses Scale Base for U, but not V (game bug? seems weird...)
            if (Node.NodeFlags.HasFlag(NodeFlags1.EnableScaleXY))
                scaleU_FirstVertex = ScaleBase;

            //Set positions
            Vertices[VERTEX_TOP_LEFT].Position.X = -scaleU_FirstVertex;
            Vertices[VERTEX_TOP_LEFT].Position.Y = ScaleV;
            Vertices[VERTEX_TOP_RIGHT].Position.X = scaleU_FirstVertex;
            Vertices[VERTEX_TOP_RIGHT].Position.Y = ScaleV;
            Vertices[VERTEX_BOTTOM_LEFT].Position.X = -ScaleU;
            Vertices[VERTEX_BOTTOM_LEFT].Position.Y = -ScaleV;
            Vertices[VERTEX_BOTTOM_RIGHT].Position.X = ScaleU;
            Vertices[VERTEX_BOTTOM_RIGHT].Position.Y = -ScaleV;

            //UV
            Vertices[VERTEX_TOP_LEFT].TextureUV.X = ParticleSystem.Textures[TextureIndex].ScrollU;
            Vertices[VERTEX_TOP_LEFT].TextureUV.Y = ParticleSystem.Textures[TextureIndex].ScrollV;
            Vertices[VERTEX_TOP_RIGHT].TextureUV.X = ParticleSystem.Textures[TextureIndex].ScrollU + ParticleSystem.Textures[TextureIndex].StepU;
            Vertices[VERTEX_TOP_RIGHT].TextureUV.Y = ParticleSystem.Textures[TextureIndex].ScrollV;
            Vertices[VERTEX_BOTTOM_LEFT].TextureUV.X = ParticleSystem.Textures[TextureIndex].ScrollU;
            Vertices[VERTEX_BOTTOM_LEFT].TextureUV.Y = ParticleSystem.Textures[TextureIndex].ScrollV + ParticleSystem.Textures[TextureIndex].StepV;
            Vertices[VERTEX_BOTTOM_RIGHT].TextureUV.X = ParticleSystem.Textures[TextureIndex].ScrollU + ParticleSystem.Textures[TextureIndex].StepU;
            Vertices[VERTEX_BOTTOM_RIGHT].TextureUV.Y = ParticleSystem.Textures[TextureIndex].ScrollV + ParticleSystem.Textures[TextureIndex].StepV;

            //Color
            if (Node.NodeFlags.HasFlag(NodeFlags1.EnableSecondaryColor))
            {
                Vertices[VERTEX_TOP_LEFT].SetColor(PrimaryColor);
                Vertices[VERTEX_TOP_RIGHT].SetColor(PrimaryColor);
                Vertices[VERTEX_BOTTOM_LEFT].SetColor(SecondaryColor);
                Vertices[VERTEX_BOTTOM_RIGHT].SetColor(SecondaryColor);
            }
            else
            {
                Vertices[VERTEX_TOP_LEFT].SetColor(PrimaryColor);
                Vertices[VERTEX_TOP_RIGHT].SetColor(PrimaryColor);
                Vertices[VERTEX_BOTTOM_LEFT].SetColor(PrimaryColor);
                Vertices[VERTEX_BOTTOM_RIGHT].SetColor(PrimaryColor);
            }

            //Duplicate vertices
            Vertices[VERTEX_BOTTOM_LEFT_ALT] = Vertices[VERTEX_BOTTOM_LEFT];
            Vertices[VERTEX_TOP_RIGHT_ALT] = Vertices[VERTEX_TOP_RIGHT];
        }

        public override void Update()
        {
            StartUpdate();

            if (State == NodeState.Active)
            {
                UpdateVertices();
            }

            UpdateChildrenNodes();
            EndUpdate();
        }

        public override void Draw()
        {
            if (State == NodeState.Active && !Node.NodeFlags.HasFlag(NodeFlags1.Hide))
            {
                //Set samplers/textures
                for (int i = 0; i < EmissionData.Samplers.Length; i++)
                {
                    GraphicsDevice.SamplerStates[EmissionData.Samplers[i].samplerSlot] = EmissionData.Samplers[i].state;
                    GraphicsDevice.VertexSamplerStates[EmissionData.Samplers[i].samplerSlot] = EmissionData.Samplers[i].state;

                    if (EmissionData.Textures[i] != null)
                    {
                        GraphicsDevice.VertexTextures[EmissionData.Samplers[i].textureSlot] = EmissionData.Textures[i].Texture;
                        GraphicsDevice.Textures[EmissionData.Samplers[i].textureSlot] = EmissionData.Textures[i].Texture;
                    }
                }


                if (Node.EmissionNode.BillboardType == ParticleBillboardType.Camera)
                {
                    //Billboard faces in the opposite direction with Matrix.CreateBillboard...?
                    //EmissionData.Material.World = Matrix.CreateBillboard(new Vector3(-Transform.Translation.X, Transform.Translation.Y, Transform.Translation.Z), GameBase.ActiveCameraBase.CameraState.Position, Vector3.Up, GameBase.ActiveCameraBase.CameraState.TargetPosition - GameBase.ActiveCameraBase.CameraState.Position);

                    //Dirty hack, but works at a basic level. Not very good for what is needed here tho...
                    EmissionData.Material.World = Matrix.Invert(GameBase.ActiveCameraBase.ViewMatrix);
                    EmissionData.Material.World.Translation = new Vector3(-Transform.Translation.X, Transform.Translation.Y, Transform.Translation.Z);

                }
                else
                {
                    EmissionData.Material.World = Transform * Matrix.CreateScale(-1f, 1, 1);
                }

                //Shader passes and vertex drawing
                foreach (EffectPass pass in EmissionData.Material.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, Vertices, 0, 2);
                }
            }

            base.Draw();
        }
    }
}
