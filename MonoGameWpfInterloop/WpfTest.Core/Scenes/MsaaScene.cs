using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System.Windows;
using WpfTest.Components;

namespace WpfTest.Scenes
{
    /// <summary>
    /// Displays content that heavily benefits from msaa (or any sort of filtering really) and lets the user toggle msaa on and off.
    /// </summary>
    public class MsaaScene : WpfGame
    {
        private BasicEffect _basicEffect;
        private WpfKeyboard _keyboard;
        private KeyboardState _keyboardState;
        private Matrix _projectionMatrix;
        private VertexBuffer _vertexBuffer;
        private VertexDeclaration _vertexDeclaration;
        private Matrix _viewMatrix;
        private Matrix _worldMatrix;
        private bool _disposed;

        private float _rotation;
        private RasterizerState _wireframe;
        private RasterizerState _filled;
        private TextComponent _msaaMessage;
        private WpfGraphicsDeviceService _graphicsDeviceService;

        protected override void Initialize()
        {
            _disposed = false;
            _graphicsDeviceService = new WpfGraphicsDeviceService(this)
            {
                PreferMultiSampling = true
            };
            Components.Add(new FpsComponent(this));
            Components.Add(new TimingComponent(this));
            _msaaMessage = new TextComponent(this, GetMsaaMessage(), new Vector2(1, 0), HorizontalAlignment.Right);
            Components.Add(_msaaMessage);

            float tilt = MathHelper.ToRadians(0);  // 0 degree angle
                                                   // Use the world matrix to tilt the cube along x and y axes.
            _worldMatrix = Matrix.CreateRotationX(tilt) * Matrix.CreateRotationY(tilt);
            _viewMatrix = Matrix.CreateLookAt(new Vector3(25, 25, 25), Vector3.Zero, Vector3.Up);

            _basicEffect = new BasicEffect(GraphicsDevice);

            _basicEffect.World = _worldMatrix;
            _basicEffect.View = _viewMatrix;
            RefreshProjection();

            // primitive color
            _basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            _basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            _basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            _basicEffect.SpecularPower = 5.0f;
            _basicEffect.Alpha = 1.0f;

            // get a bunch of white cubes
            _basicEffect.LightingEnabled = false;

            _filled = new RasterizerState
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            };
            _wireframe = new RasterizerState
            {
                CullMode = CullMode.None,
                FillMode = FillMode.WireFrame
            };

            SetupCube();

            _keyboard = new WpfKeyboard(this);

            base.Initialize();
        }

        private string GetMsaaMessage() => $"MSAA is {(_graphicsDeviceService.PreferMultiSampling ? "On" : "Off")}, press space to toggle";

        private void SetupCube()
        {
            _vertexDeclaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            );

            Vector3 topLeftFront = new Vector3(-1.0f, 1.0f, 1.0f);
            Vector3 bottomLeftFront = new Vector3(-1.0f, -1.0f, 1.0f);
            Vector3 topRightFront = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 bottomRightFront = new Vector3(1.0f, -1.0f, 1.0f);
            Vector3 topLeftBack = new Vector3(-1.0f, 1.0f, -1.0f);
            Vector3 topRightBack = new Vector3(1.0f, 1.0f, -1.0f);
            Vector3 bottomLeftBack = new Vector3(-1.0f, -1.0f, -1.0f);
            Vector3 bottomRightBack = new Vector3(1.0f, -1.0f, -1.0f);

            Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 0.0f);
            Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);

            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            var cubeVertices = new VertexPositionNormalTexture[36];

            // Front face.
            cubeVertices[0] = new VertexPositionNormalTexture(topLeftFront, frontNormal, textureTopLeft);
            cubeVertices[1] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            cubeVertices[2] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);
            cubeVertices[3] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            cubeVertices[4] = new VertexPositionNormalTexture(bottomRightFront, frontNormal, textureBottomRight);
            cubeVertices[5] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);

            // Back face.
            cubeVertices[6] = new VertexPositionNormalTexture(topLeftBack, backNormal, textureTopRight);
            cubeVertices[7] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            cubeVertices[8] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            cubeVertices[9] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            cubeVertices[10] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            cubeVertices[11] = new VertexPositionNormalTexture(bottomRightBack, backNormal, textureBottomLeft);

            // Top face.
            cubeVertices[12] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            cubeVertices[13] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);
            cubeVertices[14] = new VertexPositionNormalTexture(topLeftBack, topNormal, textureTopLeft);
            cubeVertices[15] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            cubeVertices[16] = new VertexPositionNormalTexture(topRightFront, topNormal, textureBottomRight);
            cubeVertices[17] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);

            // Bottom face.
            cubeVertices[18] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            cubeVertices[19] = new VertexPositionNormalTexture(bottomLeftBack, bottomNormal, textureBottomLeft);
            cubeVertices[20] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            cubeVertices[21] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            cubeVertices[22] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            cubeVertices[23] = new VertexPositionNormalTexture(bottomRightFront, bottomNormal, textureTopRight);

            // Left face.
            cubeVertices[24] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);
            cubeVertices[25] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            cubeVertices[26] = new VertexPositionNormalTexture(bottomLeftFront, leftNormal, textureBottomRight);
            cubeVertices[27] = new VertexPositionNormalTexture(topLeftBack, leftNormal, textureTopLeft);
            cubeVertices[28] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            cubeVertices[29] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);

            // Right face.
            cubeVertices[30] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            cubeVertices[31] = new VertexPositionNormalTexture(bottomRightFront, rightNormal, textureBottomLeft);
            cubeVertices[32] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);
            cubeVertices[33] = new VertexPositionNormalTexture(topRightBack, rightNormal, textureTopRight);
            cubeVertices[34] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            cubeVertices[35] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);

            _vertexBuffer = new VertexBuffer(GraphicsDevice, _vertexDeclaration, cubeVertices.Length, BufferUsage.None);
            _vertexBuffer.SetData(cubeVertices);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            Components.Clear();
            _disposed = true;

            _vertexBuffer.Dispose();
            _vertexBuffer = null;

            _vertexDeclaration.Dispose();
            _vertexDeclaration = null;

            _basicEffect.Dispose();
            _basicEffect = null;

            base.Dispose(disposing);
        }

        /// <summary>
        /// Update projection matrix values, both in the calculated matrix <see cref="_projectionMatrix"/> as well as
        /// the <see cref="_basicEffect"/> projection.
        /// </summary>
        private void RefreshProjection()
        {
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45), // 45 degree angle
                (float)GraphicsDevice.Viewport.Width /
                (float)GraphicsDevice.Viewport.Height,
                1.0f, 100.0f);
            _basicEffect.Projection = _projectionMatrix;
        }

        protected override void Update(GameTime gameTime)
        {
            var previousKeyboardState = _keyboardState;
            _keyboardState = _keyboard.GetState();

            if (_keyboardState.IsKeyDown(Keys.Space) &&
                previousKeyboardState.IsKeyUp(Keys.Space))
            {
                _graphicsDeviceService.PreferMultiSampling = !_graphicsDeviceService.PreferMultiSampling;
                _graphicsDeviceService.ApplyChanges();

                _msaaMessage.Text = GetMsaaMessage();

            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime time)
        {
            //The projection depends on viewport dimensions (aspect ratio).
            // Because WPF controls can be resized at any time (user resizes window)
            // we need to refresh the values each draw call, otherwise cube will look distorted to user
            RefreshProjection();

            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.DepthStencilState = new DepthStencilState
            {
                DepthBufferEnable = true
            };
            GraphicsDevice.SetVertexBuffer(_vertexBuffer);

            // Rotate cube around up-axis.
            // only update cube when the game is active
            if (IsActive)
                _rotation += (float)time.ElapsedGameTime.TotalMilliseconds / 10000 * MathHelper.TwoPi;

            DrawCubes(_filled, Color.White);
            DrawCubes(_wireframe, Color.Black);

            base.Draw(time);
        }

        private void DrawCubes(RasterizerState rasterizerState, Color color)
        {
            GraphicsDevice.RasterizerState = rasterizerState;
            const float cubeSeperaration = 4;
            const int cubesPerRow = 20;
            _basicEffect.DiffuseColor = color.ToVector3();

            var offset = new Vector3(cubeSeperaration * cubesPerRow / 2, 0, cubeSeperaration * cubesPerRow / 2);
            // really bad performance, could use instancing to improve performance a lot
            for (int y = 0; y < cubesPerRow; y++)
            {
                for (int x = 0; x < cubesPerRow; x++)
                {
                    _basicEffect.World = Matrix.CreateRotationY(_rotation) * _worldMatrix *
                                         Matrix.CreateTranslation(offset - new Vector3(x, 0, y) * cubeSeperaration);

                    foreach (var pass in _basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
                    }
                }
            }
        }
    }
}
