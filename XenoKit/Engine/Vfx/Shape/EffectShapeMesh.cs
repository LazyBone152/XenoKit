using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using XenoKit.Engine;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Vertex;

namespace XenoKit.Engine.Vfx.Shape
{
    internal sealed class EffectShapeMeshData
    {
        public EffectShapeMeshData(VertexPositionTextureColor[] vertices, ushort[] indices, PrimitiveType primitiveType, int primitiveCount)
        {
            Vertices = vertices ?? new VertexPositionTextureColor[0];
            Indices = indices;
            PrimitiveType = primitiveType;
            PrimitiveCount = primitiveCount;
        }

        public VertexPositionTextureColor[] Vertices { get; }
        public ushort[] Indices { get; }
        public PrimitiveType PrimitiveType { get; }
        public int PrimitiveCount { get; }
    }

    public class EffectShapeMesh
    {
        private VertexPositionTextureColor[] vertices = new VertexPositionTextureColor[0];
        private ushort[] indices;
        private short[] drawIndices;
        private PrimitiveType primitiveType = PrimitiveType.TriangleList;
        private int primitiveCount;

        public bool HasVertices => vertices.Length >= 3 && primitiveCount > 0;
        public BoundingBox Bounds { get; private set; } = new BoundingBox(Vector3.Zero, Vector3.Zero);
        public int VertexCount => vertices.Length;
        public int TriangleCount => primitiveCount;

        public void SetVertices(VertexPositionTextureColor[] newVertices)
        {
            SetMeshData(new EffectShapeMeshData(newVertices, null, PrimitiveType.TriangleList, (newVertices?.Length ?? 0) / 3));
        }

        internal void SetMeshData(EffectShapeMeshData meshData)
        {
            vertices = new VertexPositionTextureColor[0];
            indices = null;
            drawIndices = null;
            primitiveType = PrimitiveType.TriangleList;
            primitiveCount = 0;

            if (meshData != null)
            {
                vertices = meshData.Vertices ?? new VertexPositionTextureColor[0];
                indices = meshData.Indices;
                primitiveType = meshData.PrimitiveType;
                primitiveCount = meshData.PrimitiveCount;

                if (indices != null)
                {
                    drawIndices = new short[indices.Length];

                    for (int i = 0; i < indices.Length; i++)
                        drawIndices[i] = unchecked((short)indices[i]);
                }
            }

            UpdateBounds();
        }

        public void Clear()
        {
            vertices = new VertexPositionTextureColor[0];
            indices = null;
            drawIndices = null;
            primitiveType = PrimitiveType.TriangleList;
            primitiveCount = 0;
            Bounds = new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        public void Draw(RenderObject owner, Xv2ShaderEffect material, SamplerInfo[] samplers, Xv2Texture[] textures, bool glareOutputAllowed = true)
        {
            if (!HasVertices || material == null || owner == null)
                return;

            Xv2ShaderEffect drawMaterial = material;

            if (drawMaterial == null || !owner.RenderSystem.CheckDrawPass(drawMaterial))
                return;

            for (int i = 0; samplers != null && i < samplers.Length; i++)
            {
                owner.GraphicsDevice.SamplerStates[samplers[i].samplerSlot] = samplers[i].state;

                Xv2Texture texture = textures != null && i < textures.Length && textures[i] != null ? textures[i] : Xv2Texture.DefaultTexture;

                if (texture != null)
                    owner.GraphicsDevice.Textures[samplers[i].textureSlot] = texture.Texture;
            }

            drawMaterial.World = System.Numerics.Matrix4x4.Identity;

            foreach (EffectPass pass in drawMaterial.CurrentTechnique.Passes)
            {
                drawMaterial.SetGlareOutputAllowed(glareOutputAllowed);
                pass.Apply();

                if (drawIndices != null)
                    owner.GraphicsDevice.DrawUserIndexedPrimitives(primitiveType, vertices, 0, vertices.Length, drawIndices, 0, primitiveCount);
                else
                    owner.GraphicsDevice.DrawUserPrimitives(primitiveType, vertices, 0, primitiveCount);
            }
        }

        private void UpdateBounds()
        {
            if (vertices.Length == 0)
            {
                Bounds = new BoundingBox(Vector3.Zero, Vector3.Zero);
                return;
            }

            Vector3 min = vertices[0].Position;
            Vector3 max = vertices[0].Position;

            for (int i = 1; i < vertices.Length; i++)
            {
                min = Vector3.Min(min, vertices[i].Position);
                max = Vector3.Max(max, vertices[i].Position);
            }

            Bounds = new BoundingBox(min, max);
        }
    }
}
