using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Vertex;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticlePlane : ParticleEmissionBase
    {
        private const int VERTEX_TOP_LEFT = 0;
        private const int VERTEX_TOP_RIGHT = 1;
        private const int VERTEX_BOTTOM_LEFT_ALT = 2;
        private const int VERTEX_BOTTOM_LEFT = 3;
        private const int VERTEX_TOP_RIGHT_ALT = 4;
        private const int VERTEX_BOTTOM_RIGHT = 5;

        protected readonly VertexPositionTextureColor[] Vertices = new VertexPositionTextureColor[6];

        public override void Initialize(Matrix emitPoint, Vector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitPoint, velocity, system, node, effectPart, effect);
        }

        public override void Release()
        {
            ObjectPoolManager.ParticlePlanePool.ReleaseObject(this);
            GameBase.RenderSystem.RemoveRenderEntity(this);
        }

        private void UpdateVertices()
        {
            if (EmissionData.TextureIndex == -1 || ParticleSystem.IsSimulating) return;

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
            Vertices[VERTEX_TOP_LEFT].Position.Z = 0f;
            Vertices[VERTEX_TOP_RIGHT].Position.X = scaleU_FirstVertex;
            Vertices[VERTEX_TOP_RIGHT].Position.Y = ScaleV;
            Vertices[VERTEX_TOP_RIGHT].Position.Z = 0f;
            Vertices[VERTEX_BOTTOM_LEFT].Position.X = -ScaleU;
            Vertices[VERTEX_BOTTOM_LEFT].Position.Y = -ScaleV;
            Vertices[VERTEX_BOTTOM_LEFT].Position.Z = 0f;
            Vertices[VERTEX_BOTTOM_RIGHT].Position.X = ScaleU;
            Vertices[VERTEX_BOTTOM_RIGHT].Position.Y = -ScaleV;
            Vertices[VERTEX_BOTTOM_RIGHT].Position.Z = 0f;

            //Translate vertices relatice to camera, according to RenderDepth
            //Vector3 cameraTranslation = CameraBase.TransformRelativeToCamera(Node.EmissionNode.Texture.RenderDepth);
            //Vertices[VERTEX_TOP_LEFT].Position += cameraTranslation;
            //Vertices[VERTEX_TOP_RIGHT].Position += cameraTranslation;
            //Vertices[VERTEX_BOTTOM_LEFT].Position += cameraTranslation;
            //Vertices[VERTEX_BOTTOM_RIGHT].Position += cameraTranslation;

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
            DrawThisFrame = true;
            EmissionData.Update();
            ParticleUV.Update(ParticleSystem.CurrentFrameDelta);

            if (Node.EmissionNode.VelocityOriented && Node.EmissionNode.BillboardType == ParticleBillboardType.Camera)
            {
                VelocityOrientedAdjustment = Matrix.CreateTranslation(new Vector3(0, (ScaleV + ScaleV_Variance) / 2f, 0));
            }
            else
            {
                VelocityOrientedAdjustment = Matrix.Identity;
            }

            StartUpdate();

            if (State == NodeState.Active)
            {
                UpdateRotation();
                UpdateVertices();

                //Update world matrix
                Matrix newWorld;

                if (Node.EmissionNode.BillboardType == ParticleBillboardType.Camera)
                {
                    Matrix attachBone = GetAttachmentBone();
                    float rotAmount = RandomDirection ? -RotationAmount : RotationAmount;

                    //Used for setting the translation component of the final billboard matrix
                    Matrix worldTranslation = Transform * Matrix.CreateScale(ParticleSystem.Scale) * attachBone;

                    if (Node.EmissionNode.VelocityOriented)
                    {
                        //Skip rendering all together if velocity is zero
                        if (Velocity == Vector3.Zero)
                        {
                            DrawThisFrame = false;
                        }

                        Matrix world = Transform * attachBone;

                        //This is not entirely correct.
                        //Matrix.CreateBillboard does not create the same result as in game. This method makes the particle always look at the current camera position, while in game it only cares about camera direction
                        newWorld = Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi) * Matrix.CreateConstrainedBillboard(world.Translation, CameraBase.CameraState.Position, world.Up, -Vector3.Up, null) * Matrix.CreateScale(ParticleSystem.Scale);
                        newWorld.Translation = worldTranslation.Translation;
                    }
                    else
                    {
                        newWorld = Matrix.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(-rotAmount)) * Matrix.Invert(CameraBase.ViewMatrix) * Matrix.CreateScale(ParticleSystem.Scale);
                        newWorld.Translation = worldTranslation.Translation;
                    }
                }
                else if (Node.EmissionNode.BillboardType == ParticleBillboardType.Front)
                {
                    Matrix attachBone = GetAttachmentBone();
                    float rotAmount = RandomDirection ? -RotationAmount : RotationAmount;
                    Matrix world = Transform * Matrix.CreateScale(ParticleSystem.Scale) * attachBone;

                    newWorld = Matrix.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(-rotAmount)) * Matrix.CreateBillboard(world.Translation, attachBone.Translation, Vector3.Up, null) * Matrix.CreateScale(ParticleSystem.Scale);
                    newWorld.Translation = world.Translation;
                }
                else
                {
                    //Is ParticleBillboardType.None
                    newWorld = GetRotationAxisWorld(false);
                }

                //Apply RenderDepth offset to world position. This translates the camera toward or away from the camera by the amount specified in RenderDepth.
                //This isn't exactly how the game handles this (it moves the vertex positions), but it produces the same result and is quicker to implement
                newWorld *= Matrix.CreateTranslation(CameraBase.TransformRelativeToCamera(newWorld.Translation, Node.EmissionNode.Texture.RenderDepth));
                AbsoluteTransform = newWorld;
            }

            UpdateChildrenNodes();
            EndUpdate();
        }

        public override void Draw()
        {
            DrawThisFrame = false;
            if (ParticleSystem == null) return;
            if (!ParticleSystem.DrawThisFrame) return;

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

                EmissionData.Material.World = AbsoluteTransform;

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
