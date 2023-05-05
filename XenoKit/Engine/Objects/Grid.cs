using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine.View;

namespace XenoKit.Engine.Objects
{
    public class Grid
    {
        GraphicsDevice graphicsDevice;
        public VertexPositionColor[] Vertices;
        private BasicEffect effect;

        public int GridLines { get; set; } = 40;
        public float GridLineSpacing { get; set; } = 0.5f;
        public float AxisLength { get; set; } = 5.0f;
        public Color GridColor { get; set; } = new Color(25, 25, 25);

        public Grid(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
            effect = new BasicEffect(graphicsDevice);
            effect.Alpha = 1f;
            effect.VertexColorEnabled = true;
            CreateGrid();
        }

        public void CreateGrid()
        {
            var _vertices = new List<VertexPositionColor>();

            float startOffset = -((int)GridLines / 2) * GridLineSpacing;
            float size = (GridLines - 1) * GridLineSpacing;

            for (int i = 0; i < GridLines; ++i)
            {
                //VERTICAL
                float lineOffset = startOffset + GridLineSpacing * i;
                Vector3 vertStart = new Vector3(startOffset, 0, lineOffset);
                _vertices.Add(new VertexPositionColor(vertStart, GridColor));
                vertStart.X += size;
                _vertices.Add(new VertexPositionColor(vertStart, GridColor));

                //HORIZONTAL
                vertStart = new Vector3(lineOffset, 0, startOffset);
                _vertices.Add(new VertexPositionColor(vertStart, GridColor));
                vertStart.Z += size;
                _vertices.Add(new VertexPositionColor(vertStart, GridColor));
            }

            //*AXIS
            //_vertices.Add(new VertexPositionColor(new Vector3(0, 0.01f, 0), Color.DarkRed));
            //_vertices.Add(new VertexPositionColor(new Vector3(AxisLength, 0.01f, 0), Color.DarkRed));
            //_vertices.Add(new VertexPositionColor(new Vector3(0, 0.01f, 0), Color.DarkGreen));
            //_vertices.Add(new VertexPositionColor(new Vector3(0, AxisLength, 0), Color.DarkGreen));
            //_vertices.Add(new VertexPositionColor(new Vector3(0, 0.01f, 0), Color.DarkBlue));
            //_vertices.Add(new VertexPositionColor(new Vector3(0, 0.01f, AxisLength), Color.DarkBlue));

            this.Vertices = _vertices.ToArray();
        }


        public void Draw(Camera camera)
        {
            if (SceneManager.ShowWorldAxis)
            {
                effect.Projection = camera.ProjectionMatrix;
                effect.View = camera.ViewMatrix;
                effect.World = Matrix.Identity;

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    graphicsDevice.DepthStencilState = DepthStencilState.Default;
                    graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    graphicsDevice.BlendState = BlendState.Opaque;
                    graphicsDevice.DepthStencilState = DepthStencilState.Default;
                    pass.Apply();

                    graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, Vertices, 0, Vertices.Length / 2);
                }
            }
        }
    }
}
