using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Vertex;

namespace XenoKit.Engine.Rendering
{
    public class PostFilter : Entity
    {
        RenderSystem _renderSystem;

        private VertexPositionTexture5[] vertices;
        private short[] indices = new short[] { 0, 1, 2, 3, 2, 1 };

        private int CurrentHeight;
        private int CurrentWidth;

        private Vector2[] DefaultCoords1, Coords1, Coords2, Coords3, Coords4;

        public PostFilter(GameBase game, RenderSystem renderSystem) : base(game)
        {
            _renderSystem = renderSystem;
            vertices = new VertexPositionTexture5[4];
            DefaultCoords1 = new Vector2[4];
            Coords1 = new Vector2[4];
            Coords2 = new Vector2[4];
            Coords3 = new Vector2[4];
            Coords4 = new Vector2[4];

            DefaultCoords1[0] = Vector2.Zero;
            DefaultCoords1[1] = new Vector2(1f, 0f);
            DefaultCoords1[2] = new Vector2(0f, 1f);
            DefaultCoords1[3] = new Vector2(1f, 1f);

            CreateVertices();
        }

        private void CreateVertices()
        {
            Vector2 position = Vector2.Zero;
            Vector2 size = new Vector2(_renderSystem.CurrentRT_Width, _renderSystem.CurrentRT_Height);

            // Top-left vertex
            vertices[0] = new VertexPositionTexture5(
                new Vector3(position, 0),
                new Vector2(0, 0),
                Coords1[0], Coords2[0], Coords3[0], Coords4[0]);

            // Top-right vertex
            vertices[1] = new VertexPositionTexture5(
                new Vector3(position + new Vector2(size.X, 0), 0),
                new Vector2(1, 0),
                Coords1[1], Coords2[1], Coords3[1], Coords4[1]);

            // Bottom-left vertex
            vertices[2] = new VertexPositionTexture5(
                new Vector3(position + new Vector2(0, size.Y), 0),
                new Vector2(0, 1),
                Coords1[2], Coords2[2], Coords3[2], Coords4[2]);

            // Bottom-right vertex
            vertices[3] = new VertexPositionTexture5(
                new Vector3(position + size, 0),
                new Vector2(1, 1),
                Coords1[3], Coords2[3], Coords3[3], Coords4[3]);

            CurrentHeight = _renderSystem.CurrentRT_Height;
            CurrentWidth = _renderSystem.CurrentRT_Width;
        }

        private void SetUV(int index)
        {
            switch (index)
            {
                case 1:
                    vertices[0].TextureCoordinate2 = Coords1[0];
                    vertices[1].TextureCoordinate2 = Coords1[1];
                    vertices[2].TextureCoordinate2 = Coords1[2];
                    vertices[3].TextureCoordinate2 = Coords1[3];
                    break;
                case 2:
                    vertices[0].TextureCoordinate3 = Coords2[0];
                    vertices[1].TextureCoordinate3 = Coords2[1];
                    vertices[2].TextureCoordinate3 = Coords2[2];
                    vertices[3].TextureCoordinate3 = Coords2[3];
                    break;
                case 3:
                    vertices[0].TextureCoordinate4 = Coords3[0];
                    vertices[1].TextureCoordinate4 = Coords3[1];
                    vertices[2].TextureCoordinate4 = Coords3[2];
                    vertices[3].TextureCoordinate4 = Coords3[3];
                    break;
                case 4:
                    vertices[0].TextureCoordinate5 = Coords4[0];
                    vertices[1].TextureCoordinate5 = Coords4[1];
                    vertices[2].TextureCoordinate5 = Coords4[2];
                    vertices[3].TextureCoordinate5 = Coords4[3];
                    break;
                default:
                    throw new InvalidOperationException("SetTextureCoordinates: index must be between 1 to 4.");
            }
        }

        public void SetTextureCoordinates(float u, float v, int index = 1)
        {
            switch (index)
            {
                case 1:
                    Coords1[0] = Coords1[1] = Coords1[2] = Coords1[3] = new Vector2(u, v);
                    break;
                case 2:
                    Coords2[0] = Coords2[1] = Coords2[2] = Coords2[3] = new Vector2(u, v);
                    break;
                case 3:
                    Coords3[0] = Coords3[1] = Coords3[2] = Coords3[3] = new Vector2(u, v);
                    break;
                case 4:
                    Coords4[0] = Coords4[1] = Coords4[2] = Coords4[3] = new Vector2(u, v);
                    break;
                default:
                    throw new InvalidOperationException("SetTextureCoordinates: index must be between 1 to 4.");
            }

            SetUV(index);
        }

        public void SetTextureCoordinates(Vector2[] coords, int index)
        {
            if (coords.Length != 4) throw new ArgumentException("SetTextureCoordinates: expected coords to be of length 4.");

            switch (index)
            {
                case 1:
                    for (int i = 0; i < Coords1.Length; i++)
                        Coords1[i] = coords[i];
                    break;
                case 2:
                    for (int i = 0; i < Coords2.Length; i++)
                        Coords2[i] = coords[i];
                    break;
                case 3:
                    for (int i = 0; i < Coords3.Length; i++)
                        Coords3[i] = coords[i];
                    break;
                case 4:
                    for (int i = 0; i < Coords4.Length; i++)
                        Coords4[i] = coords[i];
                    break;
                default:
                    throw new InvalidOperationException("SetTextureCoordinates: index must be between 1 to 4.");
            }

            SetUV(index);
        }

        public void SetDefaultTexCord2()
        {
            SetTextureCoordinates(DefaultCoords1, 1);
        }

        public override void DelayedUpdate()
        {
            VerticeUpdateCheck();
        }

        private void VerticeUpdateCheck()
        {
            if (_renderSystem.CurrentRT_Width != CurrentWidth || _renderSystem.CurrentRT_Height != CurrentHeight)
            {
                CreateVertices();
            }
        }

        public void Apply(PostShaderEffect effect)
        {
            VerticeUpdateCheck();

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
