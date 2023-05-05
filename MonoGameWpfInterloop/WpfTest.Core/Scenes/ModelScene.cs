using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System.IO;
using System.Windows;
using WpfTest.Components;

namespace WpfTest.Scenes
{
    /// <summary>
    /// Displays a model loaded from disk.
    /// </summary>
    public class ModelScene : WpfGame
    {
        private Matrix _projectionMatrix;
        private Matrix _viewMatrix;
        private Matrix _worldMatrix;
        private bool _disposed;
        private Model _model;
        private float _rotation;

        protected override void Initialize()
        {
            _disposed = false;
            new WpfGraphicsDeviceService(this);
            Components.Add(new FpsComponent(this));
            Components.Add(new TimingComponent(this));
            Components.Add(new TextComponent(this, File.ReadAllText("Content\\credits.txt"), new Vector2(1, 0), HorizontalAlignment.Right));

            float tilt = MathHelper.ToRadians(0);  // 0 degree angle
                                                   // Use the world matrix to tilt the cube along x and y axes.
            _worldMatrix = Matrix.CreateRotationX(tilt) * Matrix.CreateRotationY(tilt);
            _viewMatrix = Matrix.CreateLookAt(new Vector3(5, 5, 5), Vector3.Zero, Vector3.Up);

            RefreshProjection();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _model = Content.Load<Model>("StumpyTree");
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            Components.Clear();
            _disposed = true;

            base.Dispose(disposing);
        }

        /// <summary>
        /// Update projection matrix values, both in the calculated matrix <see cref="_projectionMatrix"/>.
        /// </summary>
        private void RefreshProjection()
        {
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45), // 45 degree angle
                (float)GraphicsDevice.Viewport.Width /
                (float)GraphicsDevice.Viewport.Height,
                1.0f, 100.0f);
        }

        protected override void Draw(GameTime gameTime)
        {
            //The projection depends on viewport dimensions (aspect ratio).
            // Because WPF controls can be resized at any time (user resizes window)
            // we need to refresh the values each draw call, otherwise cube will look distorted to user
            RefreshProjection();

            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            if (IsActive)
                _rotation += (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000 * MathHelper.TwoPi / 4;

            DrawModel(_model, _rotation);

            base.Draw(gameTime);
        }

        private void DrawModel(Model model, float modelRotation)
        {
            // Copy any parent transforms.
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            // Draw the model. A model can have multiple meshes, so loop.
            foreach (ModelMesh mesh in model.Meshes)
            {
                // This is where the mesh orientation is set, as well as our camera and projection.
                foreach (BasicEffect effect in mesh.Effects)
                {
                    SetPolygonalLighting(effect);
                    effect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(new Vector3(-5f, 0, -5f))
                                                                     * Matrix.CreateRotationY(modelRotation)
                                                                     * Matrix.CreateScale(0.01f)
                                                                     * _worldMatrix;
                    effect.View = _viewMatrix;
                    effect.Projection = _projectionMatrix;
                }
                // Draw the mesh, using the effects set above.
                mesh.Draw();
            }
        }

        private void SetPolygonalLighting(BasicEffect basicEffect)
        {
            // really ugly way of getting the model lit up (green color only), but does the job for now

            // primitive color
            basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            basicEffect.SpecularPower = 2.0f;
            basicEffect.Alpha = 1.0f;

            basicEffect.LightingEnabled = true;
            if (basicEffect.LightingEnabled)
            {
                // x direction
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0, 0.75f, 0);
                basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1, -1, 0));
                // points from the light to the origin of the scene
                basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

                basicEffect.DirectionalLight1.Enabled = true;
                // y direction
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0, 0.75f, 0);
                basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1, -1, 0));
                basicEffect.DirectionalLight1.SpecularColor = Vector3.One;
            }
        }
    }
}
