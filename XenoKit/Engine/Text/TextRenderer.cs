using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using SpriteFontPlus;
using System.IO;
using Microsoft.Xna.Framework;
using XenoKit.Editor;

namespace XenoKit.Engine.Text
{
    public class TextRenderer
    {
        private readonly SpriteFont Font;
        private readonly SpriteBatch SpriteBatch;

        //Render queue
        private const int MAX_TEXT_INSTANCES = 500;
        private List<TextInstance> RenderQueue = new List<TextInstance>(MAX_TEXT_INSTANCES);

        //Text settings:
        public Color DefaultTextColor = Color.Blue;

        public TextRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            //Load font from windows dir
            var fontBakeResult = TtfFontBaker.Bake(File.ReadAllBytes(String.Format(@"{0}{1}\Fonts\arial.ttf", Path.GetPathRoot(Environment.SystemDirectory), Environment.SpecialFolder.Windows)),
                20,
                1024,
                1024,
                new[]
                {
                    CharacterRange.BasicLatin,
                    CharacterRange.Latin1Supplement,
                    CharacterRange.LatinExtendedA,
                    CharacterRange.Cyrillic
                }
            );

            Font = fontBakeResult.CreateSpriteFont(graphicsDevice);
            SpriteBatch = spriteBatch;
        }

        public void Draw()
        {
            SpriteBatch.Begin();

            //Draw text
            foreach (var text in RenderQueue)
                SpriteBatch.DrawString(Font, text.Text, text.ScreenPosition, text.Color);

            RenderQueue.Clear();

            SpriteBatch.End();
        }

        public void DrawOnScreenText(string text, Vector2 pos)
        {
            DrawOnScreenText(text, pos, DefaultTextColor);
        }

        public void DrawOnScreenText(string text, Vector2 pos, Color color)
        {
            if(RenderQueue.Count > MAX_TEXT_INSTANCES)
            {
                Log.Add($"TextRenderer: Reached maximum rendered text limit ({MAX_TEXT_INSTANCES}).");
                return;
            }

            RenderQueue.Add(new TextInstance(text, color, pos));
        }
    }

}
