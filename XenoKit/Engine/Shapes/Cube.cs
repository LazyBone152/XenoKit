using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Engine.View;

namespace XenoKit.Engine.Shapes
{
    public class Cube : Entity
    {
        public Vector3 Size;
        private Color color;

        public VertexPositionColorTexture[] _vertices { get; set; }
        private short[] Indexes;
        private BasicEffect effect;
        private bool alwaysVisible = false;

        public Cube(Vector3 position, Vector3 size, GraphicsDevice graphicsDevice, Color _color, bool startAt_0_up = true, bool _alwaysVisible = false)
        {
            alwaysVisible = _alwaysVisible;
            color = _color;
            Size = size;
            effect = new BasicEffect(graphicsDevice);
            effect.Alpha = 1f;
            effect.VertexColorEnabled = true;
            Transform = Matrix.CreateWorld(position, Vector3.Forward, Vector3.Up);
            Name = "Cube";
            this.graphicsDevice = graphicsDevice;
            ConstructCube(startAt_0_up);
        }

        private void ConstructCube(bool startAt_0_up)
        {
            _vertices = new VertexPositionColorTexture[36];
            Indexes = new short[36];

            for(int i = 0; i < 36; i++)                                     //todo remove index /clean texture uv, Or code with it and reduce the number of vertex, or you could have texture on.
                Indexes[i] = (short)i;

            // Calculate the position of the vertices on the top face.

            float startAt_0_up_offset = startAt_0_up ? 0.5f : 0.0f;
            Vector3 topLeftFront    = new Vector3(-0.5f, 0.5f + startAt_0_up_offset, -0.5f) * Size;
            Vector3 topLeftBack     = new Vector3(-0.5f, 0.5f + startAt_0_up_offset, 0.5f) * Size;
            Vector3 topRightFront   = new Vector3(0.5f, 0.5f + startAt_0_up_offset, -0.5f) * Size;
            Vector3 topRightBack    = new Vector3(0.5f, 0.5f + startAt_0_up_offset, 0.5f) * Size;

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront    = new Vector3(-0.5f, -0.5f + startAt_0_up_offset, -0.5f) * Size;
            Vector3 btmLeftBack     = new Vector3(-0.5f, -0.5f + startAt_0_up_offset, 0.5f) * Size;
            Vector3 btmRightFront   = new Vector3(0.5f, -0.5f + startAt_0_up_offset, -0.5f) * Size;
            Vector3 btmRightBack    = new Vector3(0.5f, -0.5f + startAt_0_up_offset, 0.5f) * Size;

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


        
        
        public override void Draw(Camera camera)
        {
            effect.Projection = camera.ProjectionMatrix;
            effect.View = camera.ViewMatrix;
            effect.World = Transform;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                //graphicsDevice.DepthStencilState = DepthStencilState.Default;
                graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                graphicsDevice.BlendState = BlendState.Opaque;
                graphicsDevice.DepthStencilState = (alwaysVisible) ? DepthStencilState.None : DepthStencilState.Default;
                pass.Apply();

                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _vertices, 0, _vertices.Length / 3);
            }
        }

        public void Draw(Camera camera, Matrix world)
        {
            //Change Transform but keep translation
            //Transform = world * Matrix.CreateTranslation(Transform.Translation.X, Transform.Translation.Y, Transform.Translation.Z);
            Transform = world;
            Draw(camera);
        }

        public override void Update(GameTime gameTime)
        {
        }

    }
}
