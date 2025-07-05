using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.FMP;
using MIConvexHull;
using System.Collections.Generic;
using XenoKit.Engine.Vertex;
using System.Linq;

namespace XenoKit.Engine.Model
{
    public class CollisionMesh : Entity
    {
        private VertexBuffer VertexBuffer;
        private IndexBuffer IndexBuffer;
        public Matrix World { get; set; }
        public VertexPositionColor[] Vertices { get; private set; }
        public short[] Indices { get; private set; }
        private Color color;
        private static BasicEffect Material;
        private static RasterizerState WireframeRasterState;
        private static RasterizerState RasterState;
        private BoundingBox AABB;

        private readonly bool UseWireframe = true;

        public CollisionMesh(GameBase game, FMP_CollisionVertexData fmpCollisionMesh, Color color) : base(game) 
        {
            Transform = Matrix.Identity;
            this.color = color;
            CreateVertices(fmpCollisionMesh);
            CreateBuffers();
            CreateAABB();
        }

        public CollisionMesh(GameBase game, System.Numerics.Vector3[] vertices, Color color, bool isConvex, bool wireframe = true) : base(game)
        {
            UseWireframe = wireframe;
            Transform = Matrix.Identity;
            this.color = color;

            if (isConvex)
            {
                CreateVerticesFromConvexHull(vertices);
            }
            else
            {
                CreateVertices(vertices);
            }

            if(Vertices.Length > 0)
            {
                CreateBuffers();
                CreateAABB();
            }
        }

        private void CreateVerticesFromConvexHull(System.Numerics.Vector3[] vertices)
        {
            var verts = vertices.Select(v => new ConvexVertex(v)).ToList();

            var hull = ConvexHull.Create(verts);
            // Extract triangle indices
            List<VertexPositionColor> vertexData = new List<VertexPositionColor>();
            List<short> indexData = new List<short>();

            var vertToIndex = new Dictionary<Vector3, int>();

            if(hull.Outcome == ConvexHullCreationResultOutcome.Success)
            {
                foreach (var face in hull.Result.Faces)
                {
                    // Each face is a triangle for 3D hulls
                    foreach (var vert in face.Vertices)
                    {
                        Vector3 pos = ((ConvexVertex)vert).Original;
                        if (!vertToIndex.TryGetValue(pos, out int index))
                        {
                            index = vertexData.Count;
                            vertexData.Add(new VertexPositionColor(pos, color));
                            vertToIndex[pos] = index;
                        }
                        indexData.Add((short)index);
                    }
                }
            }

            Vertices = vertexData.ToArray();
            Indices = indexData.ToArray();
        }

        private void CreateVertices(FMP_CollisionVertexData fmpCollisionMesh)
        {
            if (fmpCollisionMesh == null)
            {
                Vertices = new VertexPositionColor[0];
                Indices = new short[0];
                return;
            }

            Vertices = new VertexPositionColor[fmpCollisionMesh.Vertices.Count];
            Indices = new short[fmpCollisionMesh.Faces.Length];

            for (int i = 0; i < fmpCollisionMesh.Faces.Length; i++)
                Indices[i] = (short)fmpCollisionMesh.Faces[i];

            for(int i = 0; i < fmpCollisionMesh.Vertices.Count; i++)
            {
                Vertices[i] = new VertexPositionColor(new Vector3(fmpCollisionMesh.Vertices[i].Pos[0], fmpCollisionMesh.Vertices[i].Pos[1], fmpCollisionMesh.Vertices[i].Pos[2]), color);
            }
        }

        private void CreateVertices(System.Numerics.Vector3[] vertices)
        {
            if (vertices == null)
            {
                Vertices = new VertexPositionColor[0];
                Indices = new short[0];
                return;
            }

            Vertices = new VertexPositionColor[vertices.Length];
            Indices = new short[vertices.Length];

            for (int i = 0; i < Indices.Length; i++)
                Indices[i] = (short)i;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vertices[i] = new VertexPositionColor(new Vector3(vertices[i].X, vertices[i].Y, vertices[i].Z), color);
            }
        }

        private void CreateBuffers()
        {
            return; //disabled
            if (Vertices.Length == 0 || Indices.Length == 0) return;

            if (VertexBuffer == null)
                VertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), Vertices.Length, BufferUsage.WriteOnly);

            if (IndexBuffer == null)
                IndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, Indices.Length, BufferUsage.WriteOnly);

            VertexBuffer.SetData(Vertices);
            IndexBuffer.SetData(Indices);
        }

        internal static void CreateResources(GraphicsDevice device)
        {
            Material = new BasicEffect(device);
            Material.Alpha = 1f;
            Material.VertexColorEnabled = true;

            RasterState = new RasterizerState()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None
            };
            WireframeRasterState = new RasterizerState()
            {
                FillMode = FillMode.WireFrame,
                CullMode = CullMode.None
            };
        }

        private void CreateAABB()
        {
            Vector3 min = Vertices[0].Position;
            Vector3 max = Vertices[0].Position;

            foreach (var vertex in Vertices)
            {
                Vector3 pos = vertex.Position;

                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            AABB = new BoundingBox(min, max);
        }

        public void SetColliderMeshWorld(Matrix world)
        {
            World = world;
        }

        public override void Draw()
        {
            if (Vertices.Length == 0 || Indices.Length == 0) return;

            if (CameraBase.Frustum.Intersects(AABB.Transform(Transform)))
            {
                Material.Projection = CameraBase.ProjectionMatrix;
                Material.View = CameraBase.ViewMatrix;
                Material.World = Transform;

                DrawInternal();
            }
        }

        public void Draw(Matrix world)
        {
            if (Vertices.Length == 0) return;
            Material.World = Transform * world;

            if (CameraBase.Frustum.Intersects(AABB.Transform(Material.World)))
            {
                Material.Projection = CameraBase.ProjectionMatrix;
                Material.View = CameraBase.ViewMatrix;

                DrawInternal();
            }
        }

        private void DrawInternal()
        {
            RenderSystem.MeshDrawCalls++;

            foreach (var pass in Material.CurrentTechnique.Passes)
            {
                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.RasterizerState = UseWireframe ? WireframeRasterState : RasterState;

                pass.Apply();

                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, Vertices.Length, Indices, 0, Indices.Length / 3);
                //GraphicsDevice.SetVertexBuffer(VertexBuffer);
                //GraphicsDevice.Indices = IndexBuffer;
                //GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3);
            }
        }
    }
}
