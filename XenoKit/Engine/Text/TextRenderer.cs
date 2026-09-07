using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using XenoKit.Editor;
using XenoKit.Engine.Rendering;
using XenoKit.Engine.Textures;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Text
{
    public class TextRenderer
    {
        private readonly GraphicsDevice GraphicsDevice;
        private readonly FontSystem fontSystem;
        private readonly SpriteBatch spriteBatch;
        private readonly RenderTargetWrapper renderTarget;

        private readonly Texture2D backgroundTexture;

        private int FontSize = 20;
        private SpriteFontBase font;

        //Render queue
        private const int MAX_TEXT_INSTANCES = 2048;
        private TextInstance[] RenderQueue = new TextInstance[192];
        private int currentIndex = 0;

        //Text settings:
        public Color DefaultTextColor = Color.Blue;

        private int _textDrawThisFrame = 0;
        public int TextDrawThisFrame => _textDrawThisFrame;

        public TextRenderer()
        {
            GraphicsDevice = Viewport.Instance.GraphicsDevice;
            spriteBatch = new SpriteBatch(GraphicsDevice);
            fontSystem = new FontSystem();
            fontSystem.AddFont(File.ReadAllBytes(string.Format(@"{0}{1}\Fonts\arial.ttf", Path.GetPathRoot(Environment.SystemDirectory), Environment.SpecialFolder.Windows)));

            string texturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"XenoKit/Textures/textBackground.dds");

            if (File.Exists(texturePath))
            {
                backgroundTexture = TextureLoader.ConvertToTexture2D(texturePath);
            }
            else
            {
                Log.Add($"Could not find file \"{texturePath}\"", LogType.Error);
                backgroundTexture = new Texture2D(GraphicsDevice, 1, 1);
                backgroundTexture.SetData(new[] { Color.White });
            }

            renderTarget = new RenderTargetWrapper(Viewport.Instance.RenderSystem, 1f, SurfaceFormat.Color, false, "TextRender");
            Viewport.Instance.RenderSystem.RegisterRenderTarget(renderTarget);

            UpdateFont();
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;
            SettingsManager.SettingsSaved += SettingsManager_SettingsReloaded;
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            UpdateFont();
        }

        private void UpdateFont()
        {
            int scaledFontSize = (int)(FontSize * SettingsManager.settings.XenoKit_SuperSamplingFactor);
            font = fontSystem.GetFont(scaledFontSize);
        }

        public void Draw()
        {
            GraphicsDevice.SetRenderTarget(renderTarget.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin();

            //Draw text
            _textDrawThisFrame = currentIndex;

            for(int i = 0; i < currentIndex; i++)
            {
                if (RenderQueue[i].UseBackground)
                {
                    Vector2 margin = new Vector2(10, 7);
                    Vector2 size = font.MeasureString(RenderQueue[i].Text);
                    spriteBatch.Draw(backgroundTexture, new Rectangle((int)(RenderQueue[i].ScreenPosition.X - margin.X), (int)(RenderQueue[i].ScreenPosition.Y - margin.Y), (int)(size.X + margin.X * 2f), (int)(size.Y + margin.Y * 2f)), Color.Black);
                }

                spriteBatch.DrawString(font, RenderQueue[i].Text, RenderQueue[i].ScreenPosition, RenderQueue[i].Color);
            }

            currentIndex = 0;
            spriteBatch.End();
        }

        public void DrawOnScreenText(string text, Vector2 pos)
        {
            DrawOnScreenText(text, pos, DefaultTextColor, false);
        }

        public void DrawOnScreenText(string text, Vector2 pos, Color color, bool useBackground, bool flipHorizontal = false)
        {
            if(currentIndex == RenderQueue.Length)
            {
                if (!TryResizeQueue())
                    return;
            }

            //When using Unproject to get the screen pos of a object, the x axis will need to be flipped to account for the X-axis correction that XenoKit does. 

            Vector2 _pos = flipHorizontal ? new Vector2(GraphicsDevice.Viewport.Width - pos.X, pos.Y) : pos;
            RenderQueue[currentIndex++] = new TextInstance(text, color, _pos, useBackground);
        }

        private bool TryResizeQueue()
        {
            int oldSize = RenderQueue.Length;
            int newSize = oldSize + oldSize / 2;
            newSize = (newSize + 63) & (~63);

            if(newSize <= MAX_TEXT_INSTANCES && newSize > RenderQueue.Length)
            {
                Array.Resize(ref RenderQueue, newSize);
                return true;
            }

            return false;
        }

        public RenderTarget2D GetRenderTarget()
        {
            return renderTarget.RenderTarget;
        }
    }

}
