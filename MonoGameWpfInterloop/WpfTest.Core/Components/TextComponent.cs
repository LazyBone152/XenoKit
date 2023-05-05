using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Windows;

namespace WpfTest.Components
{
    /// <summary>
    /// Helper component that draws a specific text.
    /// </summary>
    public class TextComponent : WpfDrawableGameComponent
    {
        private SpriteBatch _spriteBatch;
        private readonly Vector2 _position;
        private readonly HorizontalAlignment _h;
        private readonly VerticalAlignment _v;
        private SpriteFont _font;

        /// <summary>
        /// Creates a new instance of text to be drawn in SCREEN COORDINATES.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="text"></param>
        /// <param name="position">NOT PIXELS, but SCREEN COORDINATES (0,0) is top left, (1,0) is top right, (1,1) bottom right, (0.5,0.5) is center, etc.</param>
        /// <param name="h"></param>
        /// <param name="v"></param>
        public TextComponent(WpfGame game, string text, Vector2 position, HorizontalAlignment h = HorizontalAlignment.Left, VerticalAlignment v = VerticalAlignment.Top) : base(game)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));
            if (h == HorizontalAlignment.Stretch || v == VerticalAlignment.Stretch)
                throw new NotSupportedException("Text cannot be stretched in monogame");

            Text = text;
            _position = position;
            _h = h;
            _v = v;
        }

        /// <summary>
        /// The text property of this instance, can be edited at any time and will reflect in the next draw call.
        /// </summary>
        public string Text { get; set; }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Game.Content.Load<SpriteFont>("DefaultFont");
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();
            var textSize = _font.MeasureString(Text);
            var screenSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            var p = RecomputePosition(_position * screenSize, textSize, _h, _v);
            _spriteBatch.DrawString(_font, Text, p, Color.White);
            _spriteBatch.End();
        }

        /// <summary>
        /// Calculates the positional change required to correctly display the object of a given size to align.
        /// Assumes that the object of size <see cref="size"/> would be drawn at the position <see cref="position"/>.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <param name="h"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private static Vector2 RecomputePosition(Vector2 position, Vector2 size, HorizontalAlignment h, VerticalAlignment v)
        {
            switch (h)
            {
                case HorizontalAlignment.Left:
                    // default behaviour, do nothing
                    break;
                case HorizontalAlignment.Center:
                    position.X -= size.X / 2f;
                    break;
                case HorizontalAlignment.Right:
                    position.X -= size.X;
                    break;
                case HorizontalAlignment.Stretch:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(h), h, null);
            }
            switch (v)
            {
                case VerticalAlignment.Top:
                    // default behaviour, do nothing
                    break;
                case VerticalAlignment.Center:
                    position.Y -= size.Y / 2f;
                    break;
                case VerticalAlignment.Bottom:
                    position.Y -= size.Y;
                    break;
                case VerticalAlignment.Stretch:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(v), v, null);
            }
            return position;
        }
    }
}