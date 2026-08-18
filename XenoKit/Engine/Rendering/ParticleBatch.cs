using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Vertex;
using XenoKit.Engine.Vfx.Particle;
using XenoKit.Helper.Find;
using Xv2CoreLib.EMP_NEW;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Rendering
{
    public class ParticleBatch : RenderObject
    {
        private ParticleBatchItem[] BatchItems;
        private VertexPositionTextureColor[] Vertices;

        private int batchIndex = 0; //The next index to add a batch to... and the number of items batched

        private bool _tryDownsize = false;
        private int _batchNumAtLastSlowUpdate = 0;
        private int _maxBatchNumSinceLastSlowUpdate = 0;
        private const int MinBatchItemCount = 64;

        // A file-defined material can reference a shader whose vertex inputs don't match VertexPositionTextureColor.
        // D3D then fails to build the input layout and throws when the particles are drawn. Materials that fail once
        // are tracked here and skipped, so one bad material can't take down the render loop.
        private static readonly HashSet<Xv2ShaderEffect> incompatibleMaterials = new HashSet<Xv2ShaderEffect>();

        public readonly ParticleNode ParticleNode;
        public readonly ParticleEmissionData EmissionData;
        public readonly bool NoGlare;

        public int NumBatch { get; private set; }
        public int MaxBatchSinceLastSlowUpdate => _maxBatchNumSinceLastSlowUpdate;

        public override int LowRezMode
        {
            get
            {
                if (EmissionData?.Material == null) return 0;
                if (ParticleEmissionBase.UsesSubtractiveMaterial(EmissionData.Material)) return 0;
                if (EmissionData.Material.MatParam.LowRez == 1) return 1;
                if (EmissionData.Material.MatParam.LowRezSmoke == 1) return 2;
                return 0;
            }
        }

        public ParticleBatch(ParticleEmissionData emissionData, ParticleNode particleNode, bool noGlare)
        {
            EmissionData = emissionData;
            ParticleNode = particleNode;
            NoGlare = noGlare;
            BatchItems = new ParticleBatchItem[MinBatchItemCount];
            Vertices = new VertexPositionTextureColor[MinBatchItemCount * 6];
        }

        private void ExpandIfNeeded(int targetSize)
        {
            if(targetSize >= BatchItems.Length)
            {
                int oldSize = BatchItems.Length;
                int newSize = oldSize + oldSize / 2; // grow by x1.5
                newSize = (newSize + 63) & (~63); // grow in chunks of 64.
                Array.Resize(ref BatchItems, newSize);
                Array.Resize(ref Vertices, newSize * 6);
            }
        }

        private void DownsizeIfNeeded(int targetSize)
        {
            if (targetSize < BatchItems.Length && BatchItems.Length > MinBatchItemCount)
            {
                int newSize = MathHelper.Clamp(((targetSize + 64 - 1) / 64) * 64, MathHelper.Max(64, NumBatch), int.MaxValue);

                if(newSize != BatchItems.Length)
                {
                    Array.Resize(ref BatchItems, newSize);
                    Array.Resize(ref Vertices, newSize * 6);
                }
            }
        }

        public void AddToBatch(ParticleBatchItem batch)
        {
            ExpandIfNeeded(batchIndex);
            BatchItems[batchIndex++] = batch;
        }

        public void SlowUpdate()
        {
            _tryDownsize = true;
        }

        private void TryDownsize()
        {
            if (_maxBatchNumSinceLastSlowUpdate < BatchItems.Length && NumBatch < _maxBatchNumSinceLastSlowUpdate)
            {
                //Resize to the max amount of ParticleBatchItems used since the last SlowUpdate
                DownsizeIfNeeded(_maxBatchNumSinceLastSlowUpdate + 1);
            }

            _batchNumAtLastSlowUpdate = NumBatch;
            _maxBatchNumSinceLastSlowUpdate = 0;
            _tryDownsize = false;
        }

        #region Draw

        public override void Update()
        {
            EmissionData.Update();
        }

        public override void Draw()
        {
            NumBatch = batchIndex;

            if (_tryDownsize)
            {
                TryDownsize();
            }
            else if (batchIndex > _maxBatchNumSinceLastSlowUpdate)
            {
                _maxBatchNumSinceLastSlowUpdate = batchIndex;
            }

            if (!RenderSystem.CheckDrawPass(EmissionData.Material) || batchIndex == 0) return;

            if (incompatibleMaterials.Contains(EmissionData.Material))
            {
                batchIndex = 0;
                return;
            }

            UpdateVertices();

            RenderSystem.MeshDrawCalls++;

            //Set samplers/textures
            for (int i = 0; i < EmissionData.Samplers.Length; i++)
            {
                GraphicsDevice.SamplerStates[EmissionData.Samplers[i].samplerSlot] = EmissionData.Samplers[i].state;

                if (EmissionData.Textures[i] != null)
                {
                    GraphicsDevice.Textures[EmissionData.Samplers[i].textureSlot] = EmissionData.Textures[i].Texture;
                }
            }

            EmissionData.Material.World = Matrix4x4.Identity;

            //Shader passes and vertex drawing
            foreach (EffectPass pass in EmissionData.Material.CurrentTechnique.Passes)
            {
                EmissionData.Material.SetGlareOutputAllowed(!NoGlare);
                pass.Apply();

                try
                {
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, Vertices, 0, batchIndex * 2);
                }
                catch (Exception ex)
                {
                    incompatibleMaterials.Add(EmissionData.Material);
                    Log.Add($"ParticleBatch: skipped drawing particles because material '{EmissionData.Material.Material?.Name}' is not compatible with the particle vertex format. {ex.Message}", LogType.Warning);
                    break;
                }
            }

            batchIndex = 0;
        }

        private void UpdateVertices()
        {
            const int VERTEX_TOP_LEFT = 0;
            const int VERTEX_TOP_RIGHT = 1;
            const int VERTEX_BOTTOM_LEFT_ALT = 2;
            const int VERTEX_BOTTOM_LEFT = 3;
            const int VERTEX_TOP_RIGHT_ALT = 4;
            const int VERTEX_BOTTOM_RIGHT = 5;

            for (int i = 0; i < batchIndex; i++)
            {
                int vertIdx = i * 6;

                //Position
                Vertices[vertIdx + VERTEX_TOP_LEFT].Position.X = -BatchItems[i].ScaleU_First;
                Vertices[vertIdx + VERTEX_TOP_LEFT].Position.Y = BatchItems[i].ScaleV;
                Vertices[vertIdx + VERTEX_TOP_LEFT].Position.Z = 0f;
                Vertices[vertIdx + VERTEX_TOP_RIGHT].Position.X = BatchItems[i].ScaleU_First;
                Vertices[vertIdx + VERTEX_TOP_RIGHT].Position.Y = BatchItems[i].ScaleV;
                Vertices[vertIdx + VERTEX_TOP_RIGHT].Position.Z = 0f;
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position.X = -BatchItems[i].ScaleU;
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position.Y = -BatchItems[i].ScaleV;
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position.Z = 0f;
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position.X = BatchItems[i].ScaleU;
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position.Y = -BatchItems[i].ScaleV;
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position.Z = 0f;

                //UV
                Vertices[vertIdx + VERTEX_TOP_LEFT].TextureUV.X = BatchItems[i].UV.ScrollU;
                Vertices[vertIdx + VERTEX_TOP_LEFT].TextureUV.Y = BatchItems[i].UV.ScrollV;
                Vertices[vertIdx + VERTEX_TOP_RIGHT].TextureUV.X = BatchItems[i].UV.ScrollU + BatchItems[i].UV.StepU;
                Vertices[vertIdx + VERTEX_TOP_RIGHT].TextureUV.Y = BatchItems[i].UV.ScrollV;
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].TextureUV.X = BatchItems[i].UV.ScrollU;
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].TextureUV.Y = BatchItems[i].UV.ScrollV + BatchItems[i].UV.StepV;
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].TextureUV.X = BatchItems[i].UV.ScrollU + BatchItems[i].UV.StepU;
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].TextureUV.Y = BatchItems[i].UV.ScrollV + BatchItems[i].UV.StepV;

                //Color
                Vertices[vertIdx + VERTEX_TOP_LEFT].SetColor(BatchItems[i].TopColor);
                Vertices[vertIdx + VERTEX_TOP_RIGHT].SetColor(BatchItems[i].TopColor);
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].SetColor(BatchItems[i].BottomColor);
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].SetColor(BatchItems[i].BottomColor);

                //Transform vertices into world space
                Vertices[vertIdx + VERTEX_TOP_LEFT].Position = Vector3.Transform(Vertices[vertIdx + VERTEX_TOP_LEFT].Position, BatchItems[i].World);
                Vertices[vertIdx + VERTEX_TOP_RIGHT].Position = Vector3.Transform(Vertices[vertIdx + VERTEX_TOP_RIGHT].Position, BatchItems[i].World);
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position = Vector3.Transform(Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position, BatchItems[i].World);
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position = Vector3.Transform(Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position, BatchItems[i].World);

                //Duplicate vertices
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT_ALT] = Vertices[vertIdx + VERTEX_BOTTOM_LEFT];
                Vertices[vertIdx + VERTEX_TOP_RIGHT_ALT] = Vertices[vertIdx + VERTEX_TOP_RIGHT];
            }
        }

        #endregion

    }

    public struct ParticleBatchItem
    {
        //Total size = 92 bytes

        public ParticleUV UV;
        public float ScaleU_First;
        public float ScaleU;
        public float ScaleV;
        public Color TopColor;
        public Color BottomColor;
        public Matrix4x4 World;
    }
}
