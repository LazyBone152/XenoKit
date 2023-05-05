using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.View;

namespace XenoKit.Engine.Shapes
{
    public class Cube : Entity
    {
        //Vertex:
        private VertexPositionColorTexture[] Vertices { get; set; }
        private short[] Indices;
        private BasicEffect Material;

        //Cube Parameters:
        public Vector3 Scale;
        public Vector3 MinBounds;
        public Vector3 MaxBounds;
        private Color Color;

        //Settings:
        private readonly bool Wireframe = false;
        private readonly bool AlwaysVisible = false;
        private readonly float YAxisOffset = 0f;

        /// <summary>
        /// Initialize a default cube.
        /// </summary>
        public Cube(Vector3 position, Vector3 size, GameBase gameBase, Color _color, bool startAt_0 = true, bool _alwaysVisible = false) : base (gameBase)
        {
            GameBase = gameBase;
            AlwaysVisible = _alwaysVisible;
            Color = _color;
            Scale = size;
            SetPosition(position);
            Name = "Cube";
            YAxisOffset = startAt_0 ? 0.5f : 0f;

            MinBounds = new Vector3(-0.5f);
            MaxBounds = new Vector3(0.5f);

            ConstructCube();
        }

        /// <summary>
        /// Initialize a cube as a bounding box. The parameters of the bounding box can be updated by calling the <see cref="SetBounds(Vector3, Vector3, float)"/> and <see cref="SetPosition(Vector3)"/> methods.
        /// </summary>
        public Cube(Vector3 position, Vector3 minBounds, Vector3 maxBounds, float size, Color color, bool wireframe, GameBase gameBase) : base(gameBase)
        {
            Wireframe = wireframe;
            Color = color;
            Scale = new Vector3(1f);

            SetPosition(position);
            ConstructCube(false);
            SetBounds(minBounds, maxBounds, size, true);
        }

        private void ConstructCube(bool createVertices = true)
        {
            //Create the material this cube will use
            Material = new BasicEffect(GraphicsDevice);
            Material.Alpha = 1f;
            Material.VertexColorEnabled = true;

            Vertices = new VertexPositionColorTexture[8];
            Indices = new short[36]; //6 * 6
            CreateVertices();

            //Create vertex index
            //TOP
            Indices[0] = 0;
            Indices[1] = 1;
            Indices[2] = 2;
            Indices[3] = 1;
            Indices[4] = 2;
            Indices[5] = 3;

            //BOTTOM
            Indices[6] = 4;
            Indices[7] = 5;
            Indices[8] = 6;
            Indices[9] = 5;
            Indices[10] = 6;
            Indices[11] = 7;

            //LEFT
            Indices[12] = 0;
            Indices[13] = 2;
            Indices[14] = 4;
            Indices[15] = 0;
            Indices[16] = 4;
            Indices[17] = 6;

            //RIGHT
            Indices[18] = 1;
            Indices[19] = 3;
            Indices[20] = 5;
            Indices[21] = 1;
            Indices[22] = 5;
            Indices[23] = 7;

            //FRONT
            Indices[24] = 0;
            Indices[25] = 1;
            Indices[26] = 6;
            Indices[27] = 1;
            Indices[28] = 6;
            Indices[29] = 7;

            //BACK
            Indices[30] = 2;
            Indices[31] = 3;
            Indices[32] = 4;
            Indices[33] = 3;
            Indices[34] = 4;
            Indices[35] = 5;
        }

        private void CreateVertices()
        {
            // Calculate the position of the vertices on the top face.

            Vector3 topLeftFront = new Vector3(MinBounds.X, MaxBounds.Y + YAxisOffset, MinBounds.Z) * Scale;
            Vector3 topLeftBack = new Vector3(MinBounds.X, MaxBounds.Y + YAxisOffset, MaxBounds.Z) * Scale;
            Vector3 topRightFront = new Vector3(MaxBounds.X, MaxBounds.Y + YAxisOffset, MinBounds.Z) * Scale;
            Vector3 topRightBack = new Vector3(MaxBounds.X, MaxBounds.Y + YAxisOffset, MaxBounds.Z) * Scale;

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront = new Vector3(MinBounds.X, MinBounds.Y + YAxisOffset, MinBounds.Z) * Scale;
            Vector3 btmLeftBack = new Vector3(MinBounds.X, MinBounds.Y + YAxisOffset, MaxBounds.Z) * Scale;
            Vector3 btmRightFront = new Vector3(MaxBounds.X, MinBounds.Y + YAxisOffset, MinBounds.Z) * Scale;
            Vector3 btmRightBack = new Vector3(MaxBounds.X, MinBounds.Y + YAxisOffset, MaxBounds.Z) * Scale;

            // UV texture coordinates
            Vector2 textureTopLeft = new Vector2(1.0f * Scale.X, 0.0f * Scale.Y);
            Vector2 textureTopRight = new Vector2(0.0f * Scale.X, 0.0f * Scale.Y);
            Vector2 textureBottomLeft = new Vector2(1.0f * Scale.X, 1.0f * Scale.Y);
            Vector2 textureBottomRight = new Vector2(0.0f * Scale.X, 1.0f * Scale.Y);

            //Create vertices for top and bottom of cube
            Vertices[0] = new VertexPositionColorTexture(topLeftFront, Color, textureTopLeft);
            Vertices[1] = new VertexPositionColorTexture(topRightFront, Color, textureTopRight);
            Vertices[2] = new VertexPositionColorTexture(topLeftBack, Color, textureTopLeft);
            Vertices[3] = new VertexPositionColorTexture(topRightBack, Color, textureTopRight);

            Vertices[4] = new VertexPositionColorTexture(btmLeftBack, Color, textureBottomLeft);
            Vertices[5] = new VertexPositionColorTexture(btmRightBack, Color, textureBottomRight);
            Vertices[6] = new VertexPositionColorTexture(btmLeftFront, Color, textureBottomLeft);
            Vertices[7] = new VertexPositionColorTexture(btmRightFront, Color, textureBottomRight);
        }

        public void SetPosition(Vector3 position)
        {
            Transform = Matrix.CreateWorld(position, Vector3.Forward, Vector3.Up);
        }

        #region Bounds
        /// <summary>
        /// Set the bounds for this cube and resize the vertices. 
        /// </summary>
        public void SetBounds(Vector3 min, Vector3 max, float size, bool useDefinedBounds)
        {
            if (useDefinedBounds)
            {
                //Calculate actual min and max bounds (the bounds defined in bac is arbitary and not in any order)
                MinBounds = new Vector3(GetMinBoundsX(min, max) - size, GetMinBoundsY(min, max) - size, GetMinBoundsZ(min, max) - size);
                MaxBounds = new Vector3(GetMaxBoundsX(min, max) + size, GetMaxBoundsY(min, max) + size, GetMaxBoundsZ(min, max) + size);
            }
            else
            {
                //Is uniform
                MinBounds = new Vector3(-size);
                MaxBounds = new Vector3(size);
            }
            
            //Resize cube
            CreateVertices();
        }

        private static float GetMinBoundsX(Vector3 MinBounds, Vector3 MaxBounds)
        {
            return (MinBounds.X < MaxBounds.X ? MinBounds.X : MaxBounds.X);
        }

        private static float GetMinBoundsY(Vector3 MinBounds, Vector3 MaxBounds)
        {
            return MinBounds.Y < MaxBounds.Y ? MinBounds.Y : MaxBounds.Y;
        }

        private static float GetMinBoundsZ(Vector3 MinBounds, Vector3 MaxBounds)
        {
            return MinBounds.Z < MaxBounds.Z ? MinBounds.Z : MaxBounds.Z;
        }

        private static float GetMaxBoundsX(Vector3 MinBounds, Vector3 MaxBounds)
        {
            return (MaxBounds.X > MinBounds.X ? MaxBounds.X : MinBounds.X);
        }

        private static float GetMaxBoundsY(Vector3 MinBounds, Vector3 MaxBounds)
        {
            return MaxBounds.Y > MinBounds.Y ? MaxBounds.Y : MinBounds.Y;
        }

        private static float GetMaxBoundsZ(Vector3 MinBounds, Vector3 MaxBounds)
        {
            return MaxBounds.Z > MinBounds.Z ? MaxBounds.Z : MinBounds.Z;
        }
        #endregion

        public override void Draw()
        {
            Material.Projection = CameraBase.ProjectionMatrix;
            Material.View = CameraBase.ViewMatrix;
            Material.World = Transform;

            DrawInternal();
        }

        public void Draw(Matrix world)
        {
            Material.Projection = CameraBase.ProjectionMatrix;
            Material.View = CameraBase.ViewMatrix;
            Material.World = Transform * world * Matrix.CreateScale(-1f, 1f, 1f);

            DrawInternal();
        }

        private void DrawInternal()
        {
            foreach (var pass in Material.CurrentTechnique.Passes)
            {
                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = AlwaysVisible ? DepthStencilState.None : DepthStencilState.Default;

                if (Wireframe)
                {
                    GraphicsDevice.RasterizerState = new RasterizerState()
                    {
                        FillMode = FillMode.WireFrame,
                        CullMode = CullMode.None
                    };
                }
                else
                {
                    GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                }

                pass.Apply();

                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, Vertices.Length, Indices, 0, 12);
            }
        }

        /*
        Olganixs old cube construction code
        private void ConstructCube()
        {
            _vertices = new VertexPositionColorTexture[36];
            Indexes = new short[36];

            for(int i = 0; i < 36; i++)                                     //todo remove index /clean texture uv, Or code with it and reduce the number of vertex, or you could have texture on.
                Indexes[i] = (short)i;

            // Calculate the position of the vertices on the top face.

            Vector3 topLeftFront    = new Vector3(-0.5f, 0.5f, -0.5f) * Size;
            Vector3 topLeftBack     = new Vector3(-0.5f, 0.5f, 0.5f) * Size;
            Vector3 topRightFront   = new Vector3(0.5f, 0.5f, -0.5f) * Size;
            Vector3 topRightBack    = new Vector3(0.5f, 0.5f, 0.5f) * Size;

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront    = new Vector3(-0.5f, -0.5f, -0.5f) * Size;
            Vector3 btmLeftBack     = new Vector3(-0.5f, -0.5f, 0.5f) * Size;
            Vector3 btmRightFront   = new Vector3(0.5f, -0.5f, -0.5f) * Size;
            Vector3 btmRightBack    = new Vector3(0.5f, -0.5f, 0.5f) * Size;

            // UV texture coordinates
            Vector2 textureTopLeft      = new Vector2(1.0f * Size.X, 0.0f * Size.Y);
            Vector2 textureTopRight     = new Vector2(0.0f * Size.X, 0.0f * Size.Y);
            Vector2 textureBottomLeft   = new Vector2(1.0f * Size.X, 1.0f * Size.Y);
            Vector2 textureBottomRight  = new Vector2(0.0f * Size.X, 1.0f * Size.Y);

            // Add the vertices for the FRONT face.
            _vertices[0] = new VertexPositionColorTexture(topLeftFront, color, textureTopLeft);
            _vertices[1] = new VertexPositionColorTexture(btmLeftFront, color, textureBottomLeft);
            _vertices[2] = new VertexPositionColorTexture(topRightFront, color, textureTopRight);
            _vertices[3] = new VertexPositionColorTexture(btmLeftFront, color, textureBottomLeft);
            _vertices[4] = new VertexPositionColorTexture(btmRightFront, color, textureBottomRight);
            _vertices[5] = new VertexPositionColorTexture(topRightFront, color, textureTopRight);

            // Add the vertices for the BACK face.
            _vertices[6] = new VertexPositionColorTexture(topLeftBack, color, textureTopRight);
            _vertices[7] = new VertexPositionColorTexture(topRightBack, color, textureTopLeft);
            _vertices[8] = new VertexPositionColorTexture(btmLeftBack, color, textureBottomRight);
            _vertices[9] = new VertexPositionColorTexture(btmLeftBack, color, textureBottomRight);
            _vertices[10] = new VertexPositionColorTexture(topRightBack, color, textureTopLeft);
            _vertices[11] = new VertexPositionColorTexture(btmRightBack, color, textureBottomLeft);

            // Add the vertices for the TOP face.
            _vertices[12] = new VertexPositionColorTexture(topLeftFront, color, textureBottomLeft);
            _vertices[13] = new VertexPositionColorTexture(topRightBack, color, textureTopRight);
            _vertices[14] = new VertexPositionColorTexture(topLeftBack, color, textureTopLeft);
            _vertices[15] = new VertexPositionColorTexture(topLeftFront, color, textureBottomLeft);
            _vertices[16] = new VertexPositionColorTexture(topRightFront, color, textureBottomRight);
            _vertices[17] = new VertexPositionColorTexture(topRightBack, color, textureTopRight);

            // Add the vertices for the BOTTOM face. 
            _vertices[18] = new VertexPositionColorTexture(btmLeftFront, color, textureTopLeft);
            _vertices[19] = new VertexPositionColorTexture(btmLeftBack, color, textureBottomLeft);
            _vertices[20] = new VertexPositionColorTexture(btmRightBack, color, textureBottomRight);
            _vertices[21] = new VertexPositionColorTexture(btmLeftFront, color, textureTopLeft);
            _vertices[22] = new VertexPositionColorTexture(btmRightBack, color, textureBottomRight);
            _vertices[23] = new VertexPositionColorTexture(btmRightFront, color, textureTopRight);

            // Add the vertices for the LEFT face.
            _vertices[24] = new VertexPositionColorTexture(topLeftFront, color, textureTopRight);
            _vertices[25] = new VertexPositionColorTexture(btmLeftBack, color, textureBottomLeft);
            _vertices[26] = new VertexPositionColorTexture(btmLeftFront, color, textureBottomRight);
            _vertices[27] = new VertexPositionColorTexture(topLeftBack, color, textureTopLeft);
            _vertices[28] = new VertexPositionColorTexture(btmLeftBack, color, textureBottomLeft);
            _vertices[29] = new VertexPositionColorTexture(topLeftFront, color, textureTopRight);

            // Add the vertices for the RIGHT face. 
            _vertices[30] = new VertexPositionColorTexture(topRightFront, color, textureTopLeft);
            _vertices[31] = new VertexPositionColorTexture(btmRightFront, color, textureBottomLeft);
            _vertices[32] = new VertexPositionColorTexture(btmRightBack, color, textureBottomRight);
            _vertices[33] = new VertexPositionColorTexture(topRightBack, color, textureTopRight);
            _vertices[34] = new VertexPositionColorTexture(topRightFront, color, textureTopLeft);
            _vertices[35] = new VertexPositionColorTexture(btmRightBack, color, textureBottomRight);
        }
        */
    }
}
