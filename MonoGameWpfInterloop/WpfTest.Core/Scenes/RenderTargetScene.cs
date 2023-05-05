using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using MonoGame.Framework.WpfInterop.Input;
using System;

namespace WpfTest.Scenes
{
    /// <summary>
    /// A demo scene that renders content to individual rendertargets and then renders the rendertargets to screen.
    /// </summary>
    public class RenderTargetScene : WpfGame
    {
        private SpriteBatch _spriteBatch;
        private RenderTarget2D[] _renderTarget;
        private WpfMouse _mouse;
        private MouseState _mouseState;
        private Texture2D _pixel;

        protected override void Initialize()
        {
            new WpfGraphicsDeviceService(this);
            base.Initialize();
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            // no issue for a normal GPU but intel graphics will have trouble with this many rendertargets (and won't run at 60fps)
            _renderTarget = new RenderTarget2D[256];
            for (int i = 0; i < _renderTarget.Length; i++)
            {
                _renderTarget[i] = new RenderTarget2D(GraphicsDevice, 400, 400);
            }
            _mouse = new WpfMouse(this);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            _mouseState = _mouse.GetState();
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            // save old rendertarget and set custom rendertarget
            var rt = (RenderTarget2D)GraphicsDevice.GetRenderTargets()[0].RenderTarget;
            for (int i = 0; i < _renderTarget.Length; i++)
            {
                // draw the custom scenes into each rendertarget
                GraphicsDevice.SetRenderTarget(_renderTarget[i]);
                // give it a slight time offset so each rendertarget will have "unique" content
                DrawCustomScene((float)gameTime.TotalGameTime.TotalSeconds + (i / 100f));

            }
            // set back the old rendertarget (wpf frame)
            GraphicsDevice.SetRenderTarget(rt);

            // draw custom rendertargets as a textures onto screen
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteBatch.Begin();
            var pos = _mouseState.Position.ToVector2();
            var rotation = (float)gameTime.TotalGameTime.TotalSeconds;
            int ix = 0;
            for (int y = -15; y <= 15; y++)
            {
                var dy = y * 44;
                for (int x = -15; x <= 15; x++)
                {
                    var dx = x * 44;
                    var d = new Vector2(dx, dy);
                    _spriteBatch.Draw(_renderTarget[ix], pos + d, null, Color.White, rotation, new Vector2(_renderTarget[ix].Width, _renderTarget[ix].Height) / 2f, new Vector2(0.075f), SpriteEffects.None, 0);
                    // use manual indexer for rendertargets in case there are less rendertargets than x/y values
                    ix = (ix + 1) % _renderTarget.Length;
                }
            }
            _spriteBatch.End();
        }

        /// <summary>
        /// Draws a bar that moves up and down between 0-height as time passes.
        /// Assumes a rendertarget has been set prior to call.
        /// </summary>
        /// <param name="time"></param>
        private void DrawCustomScene(float time)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            // map cos from [-1;1] to [0;1]
            var percentage = (Math.Cos(time * 4f) + 1) / 2f;
            const int thickness = 20;
            // all rendertargets have same size, so just take size of first
            var posY = (int)(percentage * (_renderTarget[0].Height - thickness));
            _spriteBatch.Draw(_pixel, new Rectangle(0, posY, _renderTarget[0].Width, thickness), Color.Green);
            _spriteBatch.End();
        }
    }
}
