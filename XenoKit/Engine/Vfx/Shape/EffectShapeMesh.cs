using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Shader.DXBC;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Vertex;

namespace XenoKit.Engine.Vfx.Shape
{
    internal enum EffectShapeVertexLayout
    {
        Particle,
        EtrTrail
    }

    internal sealed class EffectShapeMeshData
    {
        public EffectShapeMeshData(VertexPositionTextureColor[] vertices, ushort[] indices, PrimitiveType primitiveType, int primitiveCount)
        {
            Vertices = vertices ?? new VertexPositionTextureColor[0];
            TrailVertices = new VertexPositionNormalColorTexture[0];
            Indices = indices;
            PrimitiveType = primitiveType;
            PrimitiveCount = primitiveCount;
            VertexLayout = EffectShapeVertexLayout.Particle;
        }

        public EffectShapeMeshData(VertexPositionNormalColorTexture[] vertices, ushort[] indices, PrimitiveType primitiveType, int primitiveCount)
        {
            Vertices = new VertexPositionTextureColor[0];
            TrailVertices = vertices ?? new VertexPositionNormalColorTexture[0];
            Indices = indices;
            PrimitiveType = primitiveType;
            PrimitiveCount = primitiveCount;
            VertexLayout = EffectShapeVertexLayout.EtrTrail;
        }

        public VertexPositionTextureColor[] Vertices { get; }
        public VertexPositionNormalColorTexture[] TrailVertices { get; }
        public ushort[] Indices { get; }
        public PrimitiveType PrimitiveType { get; }
        public int PrimitiveCount { get; }
        public EffectShapeVertexLayout VertexLayout { get; }
    }

    public class EffectShapeMesh
    {
        // A file-defined material can reference a shader whose vertex inputs don't match VertexPositionTextureColor.
        // D3D then fails to build the input layout and throws when the mesh is drawn. Materials that fail once are
        // tracked here and skipped, so one bad material can't take down the render loop.
        private static readonly HashSet<Xv2ShaderEffect> incompatibleMaterials = new HashSet<Xv2ShaderEffect>();

        private VertexPositionTextureColor[] vertices = new VertexPositionTextureColor[0];
        private VertexPositionNormalColorTexture[] trailVertices = new VertexPositionNormalColorTexture[0];
        private ushort[] indices;
        private short[] drawIndices;
        private PrimitiveType primitiveType = PrimitiveType.TriangleList;
        private int primitiveCount;
        private EffectShapeVertexLayout vertexLayout = EffectShapeVertexLayout.Particle;

        public bool HasVertices => GetVertexCount() >= 3 && primitiveCount > 0;
        public BoundingBox Bounds { get; private set; } = new BoundingBox(Vector3.Zero, Vector3.Zero);
        public int VertexCount => GetVertexCount();
        public int TriangleCount => primitiveCount;

        public void SetVertices(VertexPositionTextureColor[] newVertices)
        {
            SetMeshData(new EffectShapeMeshData(newVertices, null, PrimitiveType.TriangleList, (newVertices?.Length ?? 0) / 3));
        }

        internal void SetMeshData(EffectShapeMeshData meshData)
        {
            vertices = new VertexPositionTextureColor[0];
            trailVertices = new VertexPositionNormalColorTexture[0];
            indices = null;
            drawIndices = null;
            primitiveType = PrimitiveType.TriangleList;
            primitiveCount = 0;
            vertexLayout = EffectShapeVertexLayout.Particle;

            if (meshData != null)
            {
                vertexLayout = meshData.VertexLayout;
                vertices = meshData.Vertices ?? new VertexPositionTextureColor[0];
                trailVertices = meshData.TrailVertices ?? new VertexPositionNormalColorTexture[0];
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
            trailVertices = new VertexPositionNormalColorTexture[0];
            indices = null;
            drawIndices = null;
            primitiveType = PrimitiveType.TriangleList;
            primitiveCount = 0;
            vertexLayout = EffectShapeVertexLayout.Particle;
            Bounds = new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        public void Draw(RenderObject owner, Xv2ShaderEffect material, SamplerInfo[] samplers, Xv2Texture[] textures, bool glareOutputAllowed = true)
        {
            if (!HasVertices || material == null || owner == null)
                return;

            Xv2ShaderEffect drawMaterial = material;

            if (drawMaterial == null || !owner.RenderSystem.CheckDrawPass(drawMaterial))
                return;

            if (incompatibleMaterials.Contains(drawMaterial))
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

                try
                {
                    if (drawIndices != null)
                    {
                        if (vertexLayout == EffectShapeVertexLayout.EtrTrail)
                            owner.GraphicsDevice.DrawUserIndexedPrimitives(primitiveType, trailVertices, 0, trailVertices.Length, drawIndices, 0, primitiveCount);
                        else
                            owner.GraphicsDevice.DrawUserIndexedPrimitives(primitiveType, vertices, 0, vertices.Length, drawIndices, 0, primitiveCount);
                    }
                    else
                    {
                        if (vertexLayout == EffectShapeVertexLayout.EtrTrail)
                            owner.GraphicsDevice.DrawUserPrimitives(primitiveType, trailVertices, 0, primitiveCount);
                        else
                            owner.GraphicsDevice.DrawUserPrimitives(primitiveType, vertices, 0, primitiveCount);
                    }
                }
                catch (Exception ex)
                {
                    incompatibleMaterials.Add(drawMaterial);
                    string vertexInputs = vertexLayout == EffectShapeVertexLayout.EtrTrail
                        ? "POSITION0, NORMAL0, COLOR0, TEXCOORD0"
                        : "POSITION0, COLOR0, TEXCOORD0, NORMAL0, TANGENT0";
                    Log.Add($"EffectShapeMesh: skipped drawing a trail/effect mesh because material '{drawMaterial.Material?.Name}' (shader '{drawMaterial.shaderProgram?.Name}') is not compatible with the effect vertex format. Shader needs [{GetShaderInputList(drawMaterial)}]; vertex provides [{vertexInputs}]. {ex.Message}", LogType.Warning);
                    return;
                }
            }
        }

        // Returns the vertex input semantics the shader requires, so the skip log can name the element the effect
        // vertex is missing (e.g. TEXCOORD1).
        private static string GetShaderInputList(Xv2ShaderEffect material)
        {
            DxbcInputSignature[] inputs = material?.shaderProgram?.VsParser?.InputSignature;

            if (inputs == null || inputs.Length == 0)
                return "unknown";

            return string.Join(", ", inputs
                .Where(x => x.SysValueType == 0)
                .Select(x => $"{x.Name}{x.SemanticIndex}"));
        }

        private void UpdateBounds()
        {
            int vertexCount = GetVertexCount();

            if (vertexCount == 0)
            {
                Bounds = new BoundingBox(Vector3.Zero, Vector3.Zero);
                return;
            }

            Vector3 min = GetVertexPosition(0);
            Vector3 max = min;

            for (int i = 1; i < vertexCount; i++)
            {
                Vector3 position = GetVertexPosition(i);
                min = Vector3.Min(min, position);
                max = Vector3.Max(max, position);
            }

            Bounds = new BoundingBox(min, max);
        }

        private int GetVertexCount()
        {
            return vertexLayout == EffectShapeVertexLayout.EtrTrail ? trailVertices.Length : vertices.Length;
        }

        private Vector3 GetVertexPosition(int index)
        {
            return vertexLayout == EffectShapeVertexLayout.EtrTrail ? trailVertices[index].Position : vertices[index].Position;
        }
    }
}
