using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System.IO;
using System.Linq;

namespace WpfTest.Scenes
{
    /// <summary>
    /// A demo scene that renders a sprite at scale
    /// </summary>
    public class SpriteScene : WpfGame
    {
        private SpriteBatch _spriteBatch;
        private Texture2D _atlas;
        private int _index;
        private int _frames, _targetFrames = 10;
        private const int _textureSize = 48;

        protected override void Initialize()
        {
            new WpfGraphicsDeviceService(this);
            base.Initialize();
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            using (var file = File.OpenRead("Content/mech.png"))
                _atlas = Texture2D.FromStream(GraphicsDevice, file);
        }

        protected override void Update(GameTime gameTime)
        {
            if (++_frames >= _targetFrames)
            {
                _frames -= _targetFrames;
                _index = (_index + 1) % 3;
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            GraphicsDevice.Clear(Color.CornflowerBlue);
            for (int i = 0; i < 6; i++)
            {
                var scale = i + 1;
                var offsetX = _textureSize * Enumerable.Range(1, i).Sum();
                Draw(offsetX, 0, SamplerState.PointClamp, scale);
                Draw(offsetX, 36 * scale, SamplerState.LinearClamp, scale);
                Draw(offsetX, 36 * 2 * scale, SamplerState.AnisotropicClamp, scale);
            }
        }

        private void Draw(int x, int y, SamplerState sampler, float scale)
        {
            _spriteBatch.Begin(samplerState: sampler);
            _spriteBatch.Draw(_atlas, new Rectangle(x, y, (int)(_textureSize * scale), (int)(_textureSize * scale)), new Rectangle(_index * _textureSize, 0, _textureSize, _textureSize), Color.White);
            _spriteBatch.End();
        }
    }
}
