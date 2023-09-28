using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using XenoKit.Engine.View;

namespace XenoKit.Engine.Objects
{
    public class WorldGrid : Entity
    {
        public VertexPositionColor[] Vertices;
        private BasicEffect effect;

        public int GridLines { get; private set; } = 60;
        public float GridLineSpacing { get; private set; } = 0.5f;
        public float AxisLength { get; private set; } = 5.0f;
        public Color GridColor { get; private set; } = new Color(80, 80, 80, 0);

        public WorldGrid(GameBase gameBase) : base(gameBase)
        {
            GameBase = gameBase;
            effect = new BasicEffect(GraphicsDevice);
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

        public override void Draw()
        {
            if (SceneManager.ShowWorldAxis)
            {
                effect.Projection = CameraBase.ProjectionMatrix;
                effect.View = CameraBase.ViewMatrix;
                effect.World = Matrix.Identity;

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    GraphicsDevice.BlendState = BlendState.Opaque;
                    GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    pass.Apply();

                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, Vertices, 0, Vertices.Length / 2);
                }
            }
        }
    }
}
