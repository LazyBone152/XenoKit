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
        private ParticleEmissionData EmissionData;
        private ParticleUV ParticleUV = new ParticleUV();

        private float ColorR_Variance = 0f;
        private float ColorG_Variance = 0f;
        private float ColorB_Variance = 0f;
        private float ColorA_Variance = 0f;
        private float ScaleBase_Variance = 0f;
        private float ScaleU_Variance = 0f;
        private float ScaleV_Variance = 0f;
        private float RandomRotX = 0f;
        private float RandomRotY = 0f;
        private float RandomRotZ = 0f;

        //Keyframed Values:
        private float ScaleBase = 0.5f;
        private float ScaleU = 0.5f;
        private float ScaleV = 0.5f;
        private float[] PrimaryColor = new float[4];
        private float[] SecondaryColor = new float[4];


        public override void Initialize(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            EmissionData = CompiledObjectManager.GetCompiledObject<ParticleEmissionData>(node, GameBase);
            EmissionData.EmpFile = system.EmpFile;
            SetValues();
        }

        public override void Release()
        {
            ObjectPoolManager.ParticlePool.ReleaseObject(this);
            GameBase.RenderDepthSystem.Remove(this);
        }

        public override void ClearObjectState()
        {
            base.ClearObjectState();
            EmissionData = null;
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

            if (Node.NodeFlags2.HasFlag(NodeFlags2.RandomUpVector))
            {
                RandomRotX = Xv2CoreLib.Random.Range(0, 1f);
                RandomRotY = Xv2CoreLib.Random.Range(0, 1f);
                RandomRotZ = Xv2CoreLib.Random.Range(0, 1f);
            }

            if(Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef != null)
            {
                ParticleUV.SetTexture(Node.EmissionNode.Texture.TextureEntryRef[0].TextureRef);
            }

            if (Node.EmissionNode.Texture.TextureEntryRef[1].TextureRef != null)
            {
                //Only a grand total of 2 EMPs use the second texture slot, hardly worth the effort of supporting it
                Log.Add($"WARNING: Particle Node ({Node.Name}) uses 2 textures. This is not supported and wont be reflected in the viewport!", LogType.Warning);
            }
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
            Vertices[VERTEX_TOP_LEFT].TextureUV.X = ParticleUV.ScrollU;
            Vertices[VERTEX_TOP_LEFT].TextureUV.Y = ParticleUV.ScrollV;
            Vertices[VERTEX_TOP_RIGHT].TextureUV.X = ParticleUV.ScrollU + ParticleUV.StepU;
            Vertices[VERTEX_TOP_RIGHT].TextureUV.Y = ParticleUV.ScrollV;
            Vertices[VERTEX_BOTTOM_LEFT].TextureUV.X = ParticleUV.ScrollU;
            Vertices[VERTEX_BOTTOM_LEFT].TextureUV.Y = ParticleUV.ScrollV + ParticleUV.StepV;
            Vertices[VERTEX_BOTTOM_RIGHT].TextureUV.X = ParticleUV.ScrollU + ParticleUV.StepU;
            Vertices[VERTEX_BOTTOM_RIGHT].TextureUV.Y = ParticleUV.ScrollV + ParticleUV.StepV;

            //Color
            if (Node.NodeFlags.HasFlag(NodeFlags1.EnableSecondaryColor) && !Node.NodeFlags.HasFlag(NodeFlags1.FlashOnGen))
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
            ParticleUV.Update(GameBase.IsPlaying, ParticleSystem.ActiveTimeScale);

            if (Node.EmissionNode.VelocityOriented && Node.EmissionNode.BillboardType == ParticleBillboardType.Camera)
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

                Matrix attachBone = GetAttachmentBone();

                float rotAmount = Xv2CoreLib.Random.Range(0, 2) == 1 && Node.NodeFlags2.HasFlag(NodeFlags2.RandomRotationDir) ? -RotationAmount : RotationAmount;

                if (Node.EmissionNode.BillboardType == ParticleBillboardType.Camera)
                {
                    Matrix world = Transform * Matrix.CreateScale(ParticleSystem.Scale) * attachBone * Matrix.CreateScale(-1f, 1, 1);

                    if (Node.EmissionNode.VelocityOriented)
                    {
                        //Skip rendering all together if velocity is zero
                        if (Velocity == Vector3.Zero) return;

                        //Billboard normally always faces the opposite direction for an unknown reason, this is why there is a rotation (180 degrees). TODO: More testing with this, removing for now
                        //EmissionData.Material.World = Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi) * Matrix.CreateConstrainedBillboard(world.Translation, CameraBase.CameraState.Position, world.Up, -Vector3.Up, null);
                        EmissionData.Material.World = Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi) * Matrix.CreateConstrainedBillboard(world.Translation, CameraBase.CameraState.Position, world.Up, -Vector3.Up, null);
                    }
                    else
                    {
                        //rotAmount and Camera Up vector need to be inverted... apparantly
                        EmissionData.Material.World = Matrix.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(-rotAmount)) * Matrix.CreateFromAxisAngle(Vector3.Left, MathHelper.Pi) * Matrix.CreateBillboard(world.Translation, CameraBase.CameraState.Position, -Vector3.Up, null);
                        EmissionData.Material.World.Translation = world.Translation;

                    }
                }
                else if(Node.EmissionNode.BillboardType == ParticleBillboardType.Front)
                {
                    Matrix world = Transform * Matrix.CreateScale(ParticleSystem.Scale) * attachBone * Matrix.CreateScale(-1f, 1, 1);

                    EmissionData.Material.World = Matrix.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(-rotAmount)) * Matrix.CreateBillboard(world.Translation, attachBone.Translation, Vector3.Up, null);
                    EmissionData.Material.World.Translation = world.Translation;
                }
                else
                {
                    //Is ParticleBillboardType.None
                    //For purposes of rendering, the orientation of all previous particle nodes is ignored. 
                    _ = Transform.Decompose(out Vector3 scale, out _, out Vector3 translation);
                    Matrix world = Matrix.CreateTranslation(translation) * Matrix.CreateScale(scale) * Matrix.CreateScale(ParticleSystem.Scale) * attachBone * Matrix.CreateScale(-1f, 1, 1);
                    Vector3 rotAxis;

                    if (Node.NodeFlags2.HasFlag(NodeFlags2.RandomUpVector))
                    {
                        rotAxis = new Vector3(RandomRotX + Node.EmissionNode.RotationAxis.X, RandomRotY + Node.EmissionNode.RotationAxis.Y, RandomRotZ + Node.EmissionNode.RotationAxis.Z) * rotAmount;
                        EmissionData.Material.World = Matrix.CreateFromYawPitchRoll(rotAxis.X, rotAxis.Y, rotAxis.Z) * Rotation * world;
                    }
                    else
                    {
                        rotAxis = new Vector3(Node.EmissionNode.RotationAxis.X, Node.EmissionNode.RotationAxis.Y, Node.EmissionNode.RotationAxis.Z);
                        EmissionData.Material.World = Matrix.CreateFromAxisAngle(rotAxis, MathHelper.ToRadians(rotAmount)) * Rotation * world;
                    }

                    //There is one case where this is different from the game:
                    //IF RotAxis is 0 and some rotation value is set, the plane will disappear in XenoKit, but still be visible ingame
                    //But it then disappears ingame anyway if a very low RotAxis is used
                    //Can be fixed with an additional check against RotZero being zero, then removing the rotation from the matrix multiplication, but not sure if worth it

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
