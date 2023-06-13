using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Editor;
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
        private float[] PrimaryColor = new float[4];
        private float[] SecondaryColor = new float[4];


        public Particle(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, GameBase gameBase) : base(emitPoint, velocity, system, node, effectPart, gameBase)
        {
            EmissionData = CompiledObjectManager.GetCompiledObject<ParticleEmissionData>(node, gameBase);
            EmissionData.EmpFile = system.EmpFile;
            SetValues();

        }

        public void SetValues()
        {
            ColorR_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.R);
            ColorG_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.G);
            ColorB_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.B);
            ColorA_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.Texture.Color_Variance.A);

            //Scale variances are a generated together, using the same factor. This is so they all scale uniformly
            float scaleFactor = Xv2CoreLib.Random.Range(0, 1f);
            ScaleBase_Variance = Node.EmissionNode.Texture.ScaleBase_Variance * scaleFactor;
            ScaleU_Variance = Node.EmissionNode.Texture.ScaleXY_Variance.X * scaleFactor;
            ScaleV_Variance = Node.EmissionNode.Texture.ScaleXY_Variance.Y * scaleFactor;

            StartRotation_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.StartRotation_Variance);
            ActiveRotation_Variance = Xv2CoreLib.Random.Range(0, Node.EmissionNode.ActiveRotation_Variance);
            RotationAmount = Node.EmissionNode.StartRotation + StartRotation_Variance;
        }

        private void UpdateColor()
        {
            float[] primaryColor = Node.EmissionNode.Texture.Color1.GetInterpolatedValue(CurrentTimeFactor);
            PrimaryColor[3] = Node.EmissionNode.Texture.Color1_Transparency.GetInterpolatedValue(CurrentTimeFactor);

            PrimaryColor[0] = MathHelper.Clamp(primaryColor[0] + ColorR_Variance, 0f, 1f);
            PrimaryColor[1] = MathHelper.Clamp(primaryColor[1] + ColorG_Variance, 0f, 1f);
            PrimaryColor[2] = MathHelper.Clamp(primaryColor[2] + ColorB_Variance, 0f, 1f);
            PrimaryColor[3] = MathHelper.Clamp(PrimaryColor[3] + ColorA_Variance, 0f, 1f);

            if (Node.NodeFlags.HasFlag(NodeFlags1.EnableSecondaryColor))
            {
                float[] secondaryColor = Node.EmissionNode.Texture.Color2.GetInterpolatedValue(CurrentTimeFactor);
                SecondaryColor[3] = Node.EmissionNode.Texture.Color2_Transparency.GetInterpolatedValue(CurrentTimeFactor);

                SecondaryColor[0] = MathHelper.Clamp(secondaryColor[0] + ColorR_Variance, 0f, 1f);
                SecondaryColor[1] = MathHelper.Clamp(secondaryColor[1] + ColorG_Variance, 0f, 1f);
                SecondaryColor[2] = MathHelper.Clamp(secondaryColor[2] + ColorB_Variance, 0f, 1f);
                SecondaryColor[3] = MathHelper.Clamp(SecondaryColor[3] + ColorA_Variance, 0f, 1f);
            }
        }

        private void UpdateScale()
        {
            ScaleBase = (Node.EmissionNode.Texture.ScaleBase.GetInterpolatedValue(CurrentTimeFactor) + ScaleBase_Variance) * ParticleSystem.Scale;

            if (Node.NodeFlags.HasFlag(NodeFlags1.EnableScaleXY))
            {
                float[] values = Node.EmissionNode.Texture.ScaleXY.GetInterpolatedValue(CurrentTimeFactor);

                ScaleU = (values[0] + ScaleU_Variance) * ParticleSystem.Scale;
                ScaleV = (values[1] + ScaleV_Variance) * ParticleSystem.Scale;
            }
            else
            {
                ScaleU = ScaleBase;
                ScaleV = ScaleBase;
            }
        }
        
        private void UpdateVertices()
        {
            if (EmissionData.TextureIndex == -1) return;

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
            Vertices[VERTEX_TOP_LEFT].TextureUV.X = ParticleSystem.Textures[EmissionData.TextureIndex].ScrollU;
            Vertices[VERTEX_TOP_LEFT].TextureUV.Y = ParticleSystem.Textures[EmissionData.TextureIndex].ScrollV;
            Vertices[VERTEX_TOP_RIGHT].TextureUV.X = ParticleSystem.Textures[EmissionData.TextureIndex].ScrollU + ParticleSystem.Textures[EmissionData.TextureIndex].StepU;
            Vertices[VERTEX_TOP_RIGHT].TextureUV.Y = ParticleSystem.Textures[EmissionData.TextureIndex].ScrollV;
            Vertices[VERTEX_BOTTOM_LEFT].TextureUV.X = ParticleSystem.Textures[EmissionData.TextureIndex].ScrollU;
            Vertices[VERTEX_BOTTOM_LEFT].TextureUV.Y = ParticleSystem.Textures[EmissionData.TextureIndex].ScrollV + ParticleSystem.Textures[EmissionData.TextureIndex].StepV;
            Vertices[VERTEX_BOTTOM_RIGHT].TextureUV.X = ParticleSystem.Textures[EmissionData.TextureIndex].ScrollU + ParticleSystem.Textures[EmissionData.TextureIndex].StepU;
            Vertices[VERTEX_BOTTOM_RIGHT].TextureUV.Y = ParticleSystem.Textures[EmissionData.TextureIndex].ScrollV + ParticleSystem.Textures[EmissionData.TextureIndex].StepV;

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
            EmissionData.Update();

            if (Node.EmissionNode.VisibleOnlyOnMotion)
            {
                ScaleAdjustment = Matrix.CreateTranslation(new Vector3(0, (ScaleV + ScaleV_Variance) / 2f, 0));
            }

            StartUpdate();

            if (State == NodeState.Active)
            {
                UpdateRotation();
                UpdateVertices();
            }

            UpdateChildrenNodes();
            EndUpdate();
        }

        public override void Draw()
        {
            if (State == NodeState.Active && !Node.NodeFlags.HasFlag(NodeFlags1.Hide))
            {
                //If the "Motion Only" flag is selected on the default plane, the particle should only be visible if there is any velocity
                if (Node.NodeSpecificType == NodeSpecificType.AutoOriented_VisibleOnSpeed && Velocity == Vector3.Zero) return;

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
                    //EmissionData.Material.World = Matrix.Invert(GameBase.ActiveCameraBase.ViewMatrix);
                    //Matrix world = Transform * GetAttachmentBone();
                    //EmissionData.Material.World.Translation = new Vector3(-world.Translation.X, world.Translation.Y, world.Translation.Z);

                    //try again
                    Matrix world = Transform * GetAttachmentBone() * Matrix.CreateScale(-1f, 1, 1);
                    //EmissionData.Material.World = Matrix.CreateBillboard(world.Translation, CameraBase.CameraState.Position, Vector3.Up, forward);

                    if (Node.EmissionNode.VisibleOnlyOnMotion)
                    {
                        //Billboard normally always faces the opposite direction for an unknown reason, this is why there is a rotation (180 degrees). TODO: More testing with this, removing for now
                        EmissionData.Material.World = Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi) * Matrix.CreateConstrainedBillboard(world.Translation, CameraBase.CameraState.Position, world.Up, -Vector3.Up, null);
                    }
                    else
                    {
                        float rotAmount = Xv2CoreLib.Random.Range(0, 2) == 1 && Node.NodeFlags2.HasFlag(NodeFlags2.RandomRotationDir) ? -RotationAmount : RotationAmount;

                        //rotAmount and Camera Up vector need to be inverted... apparantly
                        EmissionData.Material.World = Matrix.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(-rotAmount)) * Matrix.CreateFromAxisAngle(Vector3.Left, MathHelper.Pi) * Matrix.CreateBillboard(world.Translation, CameraBase.CameraState.Position, -Vector3.Up, null); // * Matrix.Invert(CameraBase.ViewMatrix) * Matrix.CreateRotationZ(MathHelper.ToRadians(RotationAmount)) * CameraBase.ViewMatrix
                        EmissionData.Material.World.Translation = world.Translation;

                    }
                }
                else
                {
                    EmissionData.Material.World = Transform * GetAttachmentBone() * Matrix.CreateScale(-1f, 1, 1);
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
