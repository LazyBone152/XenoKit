using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace XenoKit.Engine.Model
{
    public class CollisionMeshBatchDraw : Entity
    {
        struct MeshDrawData
        {
            public int VertexOffset;
            public int IndexOffset;
            public int PrimitiveCount;
            public Matrix World;
        }

        private VertexBuffer VertexBuffer;
        private IndexBuffer IndexBuffer;

        private int[] indices;
        private VertexPositionColor[] vertices;
        private MeshDrawData[] meshes;
        private CollisionMesh[] collisionMeshes;

        private static BasicEffect Material;
        private static RasterizerState RasterState;

        public CollisionMeshBatchDraw(GameBase game, List<CollisionMesh> collisionMeshes) : base(game)
        {
            SetMeshData(collisionMeshes);
            CreateBuffers();
            BasicSetup();
        }

        private void SetMeshData(List<CollisionMesh> collisionMeshes)
        {
            int vertexOffset = 0;
            int indexOffset = 0;

            List<VertexPositionColor> combinedVertices = new List<VertexPositionColor>();
            List<int> combinedIndices = new List<int>();
            List<MeshDrawData> meshDrawDatas = new List<MeshDrawData>();

            foreach (var mesh in collisionMeshes)
            {
                var drawData = new MeshDrawData();
                drawData.VertexOffset = vertexOffset;
                drawData.IndexOffset = indexOffset;
                drawData.PrimitiveCount = mesh.Indices.Length / 3;

                if(drawData.PrimitiveCount > 0)
                {
                    combinedVertices.AddRange(mesh.Vertices);

                    var offsetIndices = mesh.Indices.Select(i => i + vertexOffset);
                    combinedIndices.AddRange(offsetIndices);

                    vertexOffset += mesh.Vertices.Length;
                    indexOffset += mesh.Indices.Length;

                    drawData.World = mesh.World;
                    meshDrawDatas.Add(drawData);
                }
            }

            indices = combinedIndices.ToArray();
            vertices = combinedVertices.ToArray();
            meshes = meshDrawDatas.ToArray();
            this.collisionMeshes = collisionMeshes.ToArray();
        }

        private void CreateBuffers()
        {
            if (VertexBuffer != null)
                VertexBuffer.Dispose();

            if (IndexBuffer != null)
                IndexBuffer.Dispose();

            VertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
            IndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);

            VertexBuffer.SetData(vertices);
            IndexBuffer.SetData(indices);
        }

        private void BasicSetup()
        {
            if (Material == null)
            {
                Material = new BasicEffect(GraphicsDevice);
                Material.Alpha = 1f;
                Material.VertexColorEnabled = true;
            }

            if (RasterState == null)
            {
                RasterState = new RasterizerState()
                {
                    FillMode = FillMode.WireFrame,
                    CullMode = CullMode.None
                };
            }
        }

        public override void Draw()
        {
            foreach (var pass in Material.CurrentTechnique.Passes)
            {
                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.RasterizerState = RasterState;


                GraphicsDevice.SetVertexBuffer(VertexBuffer);
                GraphicsDevice.Indices = IndexBuffer;

                Material.Projection = CameraBase.ProjectionMatrix;
                Material.View = CameraBase.ViewMatrix;
                Material.World = Matrix.Identity;

                pass.Apply();

                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3);

                /*
                for (int i = 0; i < meshes.Length; i++)
                {
                    Material.World = meshes[i].World;
                    pass.Apply();

                    GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: meshes[i].VertexOffset,
                        startIndex: meshes[i].IndexOffset,
                        primitiveCount: meshes[i].PrimitiveCount
                    );
                }
                */
            }
        }
    }
}
