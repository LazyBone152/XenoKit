using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;

namespace WpfTest.Components
{
    public class TimingComponent : WpfDrawableGameComponent
    {
        #region Fields
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private int _skipFrames = 5;
        private readonly List<TimeSpan> _last2Seconds;
        #endregion

        #region Constructors

        public TimingComponent(WpfGame game) : base(game)
        {
            _last2Seconds = new List<TimeSpan>();
        }

        #endregion

        #region Methods

        protected override void LoadContent()
        {
            _font = Game.Content.Load<SpriteFont>("DefaultFont");

            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public override void Update(GameTime gameTime)
        {
        }

        public override void Draw(GameTime gameTime)
        {
            if (_skipFrames > 0)
            {
                // ignore first few frames as they take longer than all others due to WPF startup
                _skipFrames--;
            }
            else
            {
                _last2Seconds.Add(gameTime.ElapsedGameTime);
                if (_last2Seconds.Sum(x => x.TotalMilliseconds) > TimeSpan.FromSeconds(2).TotalMilliseconds)
                {
                    _last2Seconds.RemoveAt(0);
                }
            }
            _spriteBatch.Begin();
            // accumulate average over last 2 seconds
            var avg = _last2Seconds.Sum(x => x.TotalMilliseconds) / _last2Seconds.Count;
            _spriteBatch.DrawString(_font, $"Average frame time: {avg:0.0}ms", new Vector2(5, 25), Color.White);
            _spriteBatch.End();
        }

        #endregion
    }
}