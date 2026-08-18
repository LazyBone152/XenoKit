using Microsoft.Xna.Framework.Graphics;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using Xv2CoreLib;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleMesh : ParticleEmissionBase
    {
        protected Xv2Submesh EmgSubmesh = null;
        private bool ChildrenWarning = false;

        public override void Initialize(Matrix4x4 emitPoint, SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitPoint, velocity, system, node, effectPart, effect);
        }

        public override void SetValues()
        {
            base.SetValues();
            EmgSubmesh = CompiledObjectManager.GetCompiledObject<Xv2Submesh>(Node.EmissionNode.Mesh.EmgFile);

        }

        public override void Release()
        {
            ObjectPoolManager.ParticleMeshPool.ReleaseObject(this);
            ViewportInstance.RenderSystem.RemoveRenderEntity(this);
        }

        public void UpdateVertices()
        {
            if (EmgSubmesh == null || ParticleSystem.IsSimulating) return;

            UpdateScale();
            UpdateColor();

            for (int i = 0; i < EmgSubmesh.Vertices.Length; i++)
            {
                EmgSubmesh.Vertices[i].SetColor(PrimaryColor[2], PrimaryColor[1], PrimaryColor[0], PrimaryColor[3]); //Have to flip colors because they appear as BGRA in character vertex, which Xv2Submesh is using, but are RGBA for particles

                if ((Node.NodeFlags & NodeFlags1.EnableScaleXY) == NodeFlags1.EnableScaleXY)
                {
                    EmgSubmesh.Vertices[i].Position.X = EmgSubmesh.Vertices[i].Position.X * ScaleBase;
                    EmgSubmesh.Vertices[i].Position.Y = EmgSubmesh.Vertices[i].Position.Y * ScaleV;
                    EmgSubmesh.Vertices[i].Position.Z = EmgSubmesh.Vertices[i].Position.Z * ScaleU;
                }
                else
                {
                    EmgSubmesh.Vertices[i].Position = EmgSubmesh.Vertices[i].Position * ScaleBase;
                }
            }
        }

        public override void Update()
        {
            DrawThisFrame = EmgSubmesh != null;
            EmissionData.Update();
            ParticleUV.Update(ParticleSystem.CurrentFrameDelta);

            StartUpdate();

            if (State == NodeState.Active)
            {
                UpdateRotation();
                UpdateVertices();

                AbsoluteTransform = GetRotationAxisWorld(true);
                AbsoluteTransform *= Matrix4x4.CreateTranslation(Camera.TransformRelativeToCamera(AbsoluteTransform.Translation, Node.EmissionNode.Texture.RenderDepth));
            }

            UpdateChildrenNodes();
            EndUpdate();
        }

        public override void Draw()
        {
            if (!RenderSystem.CheckDrawPass(EmissionData.Material)) return;

            if (!ParticleSystem.DrawThisFrame) return;

            if (State == NodeState.Active && (Node.NodeFlags & NodeFlags1.Hide) != NodeFlags1.Hide)
            {
                //Set samplers/textures
                for (int i = 0; i < EmissionData.Samplers.Length; i++)
                {
                    GraphicsDevice.SamplerStates[EmissionData.Samplers[i].samplerSlot] = EmissionData.Samplers[i].state;

                    if (EmissionData.Textures[i] != null)
                    {
                        GraphicsDevice.Textures[EmissionData.Samplers[i].textureSlot] = EmissionData.Textures[i].Texture;
                    }
                }

                EmissionData.Material.World = AbsoluteTransform;

                //Shader passes and vertex drawing
                foreach (EffectPass pass in EmissionData.Material.CurrentTechnique.Passes)
                {
                    EmissionData.Material.SetGlareOutputAllowed(!SourceEffectPart.NoGlare);
                    pass.Apply();

                    GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, EmgSubmesh.Vertices, 0, EmgSubmesh.Vertices.Length, EmgSubmesh.Indices, 0, EmgSubmesh.Indices.Length / 3);
                }
            }

            base.Draw();
        }

        protected override void Emit()
        {
            if(Node.ChildParticleNodes.Count > 0 && !ChildrenWarning)
            {
                Log.Add("ParticleSystem: Mesh emissions cannot have children nodes. The game will crash!", LogType.Error);
                ChildrenWarning = true;
            }
        }
    }
}
