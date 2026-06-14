using Microsoft.Xna.Framework;
using XenoKit.Engine.Rendering;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticlePlane : ParticleEmissionBase
    {
        protected BoundingBox AABB = new BoundingBox();

        private ParticleBatch Batch;

        public override void Initialize(Matrix4x4 emitPoint, SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            Batch = RenderSystem.ParticleBatcher.GetBatch(node, effectPart.NoGlare);
        }

        public override void Release()
        {
            ObjectPoolManager.ParticlePlanePool.ReleaseObject(this);
        }

        private void UpdateAABB()
        {
            float scaleU_FirstVertex = ((Node.NodeFlags & NodeFlags1.EnableScaleXY) != 0) ? ScaleBase : ScaleU;

            float aabbX = scaleU_FirstVertex > ScaleU ? scaleU_FirstVertex : ScaleU;
            Vector3 min = new Vector3(aabbX, ScaleV, 0f);
            Vector3 max = new Vector3(-aabbX, -ScaleV, 0f);

            AABB = new BoundingBox(min, max);
        }

        public override void Update()
        {
            DrawThisFrame = true;
            EmissionData.Update();
            ParticleUV.Update(ParticleSystem.CurrentFrameDelta);

            if (Node.EmissionNode.VelocityOriented && Node.EmissionNode.BillboardType == ParticleBillboardType.Camera)
            {
                VelocityOrientedAdjustment = Matrix4x4.CreateTranslation(new SimdVector3(0, (ScaleV + ScaleV_Variance) / 2f, 0));
            }
            else
            {
                VelocityOrientedAdjustment = Matrix4x4.Identity;
            }

            StartUpdate();

            if (State == NodeState.Active)
            {
                UpdateRotation();
                UpdateScale();
                UpdateColor();
                UpdateAABB();

                //Update world matrix
                Matrix4x4 newWorld;

                if (Node.EmissionNode.BillboardType == ParticleBillboardType.Camera)
                {
                    Matrix4x4 attachBone = GetAttachmentBone();
                    float rotAmount = RandomDirection ? -RotationAmount : RotationAmount;

                    //Used for setting the translation component of the final billboard matrix
                    Matrix4x4 worldTranslation = Transform * Matrix4x4.CreateScale(ParticleSystem.Scale) * attachBone;

                    if (Node.EmissionNode.VelocityOriented)
                    {
                        //Skip rendering all together if velocity is zero
                        if (Velocity == Vector3.Zero)
                        {
                            DrawThisFrame = false;
                        }

                        Matrix4x4 world = Transform * attachBone;

                        //This is not entirely correct.
                        //Matrix.CreateBillboard does not create the same result as in game. This method makes the particle always look at the current camera position, while in game it only cares about camera direction
                        //newWorld = Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi) * Matrix.CreateConstrainedBillboard(world.Translation, CameraBase.CameraState.Position, world.Up, -Vector3.Up, null) * Matrix.CreateScale(ParticleSystem.Scale);
                        newWorld = Matrix4x4.CreateConstrainedBillboard(world.Translation, Camera.CameraState.Position, world.GetUp(), -MathHelpers.Up, SimdVector3.Zero) * Matrix4x4.CreateScale(ParticleSystem.Scale);
                        
                        newWorld.Translation = worldTranslation.Translation;
                    }
                    else
                    {
                        newWorld = Matrix4x4.CreateFromAxisAngle(MathHelpers.Up, MathHelper.Pi) * Matrix4x4.CreateFromAxisAngle(MathHelpers.Forward, MathHelper.ToRadians(-rotAmount)) * MathHelpers.Invert(Camera.ViewMatrix) * Matrix4x4.CreateScale(ParticleSystem.Scale);
                        newWorld.Translation = worldTranslation.Translation;
                    }
                }
                else if (Node.EmissionNode.BillboardType == ParticleBillboardType.Front)
                {
                    Matrix4x4 attachBone = GetAttachmentBone();
                    float rotAmount = RandomDirection ? -RotationAmount : RotationAmount;
                    Matrix4x4 world = Transform * Matrix4x4.CreateScale(ParticleSystem.Scale) * attachBone;

                    newWorld = Matrix4x4.CreateFromAxisAngle(MathHelpers.Forward, MathHelper.ToRadians(-rotAmount)) * Matrix4x4.CreateBillboard(world.Translation, attachBone.Translation, MathHelpers.Up, SimdVector3.Zero) * Matrix4x4.CreateScale(ParticleSystem.Scale);
                    newWorld.Translation = world.Translation;
                }
                else
                {
                    //Is ParticleBillboardType.None
                    newWorld = GetRotationAxisWorld(false);
                }

                //Apply RenderDepth offset to world position. This translates the camera toward or away from the camera by the amount specified in RenderDepth.
                //This isn't exactly how the game handles this (it moves the vertex positions), but it produces the same result and is quicker to implement
                newWorld *= Matrix4x4.CreateTranslation(Camera.TransformRelativeToCamera(newWorld.Translation, Node.EmissionNode.Texture.RenderDepth));
                AbsoluteTransform = newWorld;
            }

            UpdateChildrenNodes();
            EndUpdate();
            DrawBatch();
        }

        public void DrawBatch()
        {
            if (!Viewport.Instance.DrawThisFrame) return;
            if (EmissionData == null || ParticleSystem == null) return;
            //if (!ParticleSystem.DrawThisFrame) return;
            //if (!RenderSystem.CheckDrawPass(EmissionData.Material) || !ParticleSystem.DrawThisFrame) return;

            if (!FrustumIntersects(AbsoluteTransform, AABB))
                return;

            //RenderSystem.MeshDrawCalls++;

            if (State == NodeState.Active && (Node.NodeFlags & NodeFlags1.Hide) != NodeFlags1.Hide)
            {
                if (Batch.IsDestroyed)
                {
                    Batch = RenderSystem.ParticleBatcher.GetBatch(Node, SourceEffectPart.NoGlare);
                }

                Batch.AddToBatch(CreateBatchItem());
                /*
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
                */
            }

        }

        private ParticleBatchItem CreateBatchItem()
        {
            //Special case for when Scale XY is used. The first vertex still uses Scale Base for U, but not V (game bug? seems weird...)
            float scaleU_FirstVertex = ((Node.NodeFlags & NodeFlags1.EnableScaleXY) != 0) ? ScaleBase : ScaleU;
            bool useBottomColor = (Node.NodeFlags & NodeFlags1.EnableSecondaryColor) != 0 && (Node.NodeFlags & NodeFlags1.FlashOnGen) == 0;
            
            return new ParticleBatchItem()
            {
                UV = ParticleUV,
                World = AbsoluteTransform,
                ScaleU = ScaleU,
                ScaleV = ScaleV,
                ScaleU_First = scaleU_FirstVertex,
                TopColor = new Color(PrimaryColor[0], PrimaryColor[1], PrimaryColor[2], PrimaryColor[3]),
                BottomColor = useBottomColor ? new Color(SecondaryColor[0], SecondaryColor[1], SecondaryColor[2], SecondaryColor[3]) : new Color(PrimaryColor[0], PrimaryColor[1], PrimaryColor[2], PrimaryColor[3])
            };
        }

    }
}
