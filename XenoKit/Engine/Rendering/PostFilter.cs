using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Vertex;

namespace XenoKit.Engine.Rendering
{
    public class PostFilter : Entity
    {
        private VertexPositionTexture2[] vertices;
        private short[] indices = new short[] { 0, 1, 2, 3, 2, 1 };

        private int CurrentHeight;
        private int CurrentWidth;

        private float CurrentU;
        private float CurrentV;

        public PostFilter(GameBase game) : base(game)
        {
            vertices = new VertexPositionTexture2[4];
            CreateVertices();
        }

        private void CreateVertices()
        {
            Vector2 position = Vector2.Zero;
            Vector2 size = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            // Top-left vertex
            vertices[0] = new VertexPositionTexture2(
                new Vector3(position, 0),
                new Vector2(0, 0),
                new Vector2(CurrentU, CurrentV));

            // Top-right vertex
            vertices[1] = new VertexPositionTexture2(
                new Vector3(position + new Vector2(size.X, 0), 0),
                new Vector2(1, 0),
                new Vector2(CurrentU, CurrentV));

            // Bottom-left vertex
            vertices[2] = new VertexPositionTexture2(
                new Vector3(position + new Vector2(0, size.Y), 0),
                new Vector2(0, 1),
                new Vector2(CurrentU, CurrentV));

            // Bottom-right vertex
            vertices[3] = new VertexPositionTexture2(
                new Vector3(position + size, 0),
                new Vector2(1, 1),
                new Vector2(CurrentU, CurrentV));

            CurrentHeight = GraphicsDevice.Viewport.Height;
            CurrentWidth = GraphicsDevice.Viewport.Width;
        }

        private void SetUV2()
        {
            vertices[0].TextureCoordinate2 = new Vector2(CurrentU, CurrentV);
            vertices[1].TextureCoordinate2 = new Vector2(CurrentU, CurrentV);
            vertices[2].TextureCoordinate2 = new Vector2(CurrentU, CurrentV);
            vertices[3].TextureCoordinate2 = new Vector2(CurrentU, CurrentV);
        }

        public void SetTextureCoordinates(float u, float v)
        {
            if(CurrentU != u || CurrentV != v)
            {
                CurrentU = u;
                CurrentV = v;
                SetUV2();
            }
        }

        public override void DelayedUpdate()
        {
            if(GraphicsDevice.Viewport.Width != CurrentWidth || GraphicsDevice.Viewport.Height != CurrentHeight)
            {
                CreateVertices();
            }
        }

        public void DisplayPostFilter(PostShaderEffect effect)
        {
            for (int i = 0; i < effect.ImageSampler.Length; i++)
                GraphicsDevice.SamplerStates[i] = effect.ImageSampler[i];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                DrawQuad();
            }

        }

        private void DrawQuad()
        {
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
        }
    }
}
