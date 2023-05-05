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
    public class Plane : Entity
    {
        public Vector3 Size;
        private Color color;

        public VertexPositionColorTexture[] _vertices { get; set; }
        private BasicEffect effect;

        public Plane(Vector3 position, Vector3 size, GraphicsDevice graphicsDevice, Color _color)
        {
            color = _color;
            Size = size;
            effect = new BasicEffect(graphicsDevice);
            effect.Alpha = 1f;
            effect.VertexColorEnabled = true;
            Transform = Matrix.CreateWorld(position, Vector3.Forward, Vector3.Up);
            Name = "Cube";
            this.graphicsDevice = graphicsDevice;
            ConstructPlane();
        }

        private void ConstructPlane()
        {
            _vertices = new VertexPositionColorTexture[6];

            Vector3 topLeftFront = new Vector3(-0.5f, 0.5f, -0.5f) * Size;
            Vector3 topLeftBack = new Vector3(-0.5f, 0.5f, 0.5f) * Size;
            Vector3 topRightFront = new Vector3(0.5f, 0.5f, -0.5f) * Size;
            Vector3 topRightBack = new Vector3(0.5f, 0.5f, 0.5f) * Size;
            
            Vector2 textureTopLeft = new Vector2(1.0f * Size.X, 0.0f * Size.Y);
            Vector2 textureTopRight = new Vector2(0.0f * Size.X, 0.0f * Size.Y);
            Vector2 textureBottomLeft = new Vector2(1.0f * Size.X, 1.0f * Size.Y);
            Vector2 textureBottomRight = new Vector2(0.0f * Size.X, 1.0f * Size.Y);


            _vertices[0] = new VertexPositionColorTexture(topLeftFront, color, textureBottomLeft);
            _vertices[1] = new VertexPositionColorTexture(topRightBack, color, textureTopRight);
            _vertices[2] = new VertexPositionColorTexture(topLeftBack, color, textureTopLeft);
            _vertices[3] = new VertexPositionColorTexture(topLeftFront, color, textureBottomLeft);
            _vertices[4] = new VertexPositionColorTexture(topRightFront, color, textureBottomRight);
            _vertices[5] = new VertexPositionColorTexture(topRightBack, color, textureTopRight);


        }




        public override void Draw(Camera camera)
        {
            effect.Projection = camera.ProjectionMatrix;
            effect.View = camera.ViewMatrix;
            effect.World = Transform;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                graphicsDevice.DepthStencilState = DepthStencilState.Default;
                graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                graphicsDevice.BlendState = BlendState.Opaque;
                graphicsDevice.DepthStencilState = DepthStencilState.Default;
                pass.Apply();

                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _vertices, 0, _vertices.Length / 3);
            }
        }
        
        public override void Update(GameTime gameTime)
        {
        }

    }
}
