using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using XenoKit.Inspector.InspectorEntities;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Rendering
{
    public partial class RenderSystem : RenderObject
    {
        public void RequestScreenshot(ScreenshotType screenshotType)
        {
            if (!ShaderManager.IsExtShadersLoaded)
            {
                return;
            }
            ScreenshotRequested = true;
            ScreenshotType = screenshotType;
        }

        public bool HasPendingScreenshot()
        {
            return ScreenshotRequested;
        }

        public void ProcessPendingScreenshot(RenderTarget2D sceneRenderTarget, RenderTarget2D effectRenderTarget)
        {
            if (!ScreenshotRequested)
            {
                return;
            }

            ProcessScreenshot(sceneRenderTarget, effectRenderTarget);
        }

        private void ProcessScreenshot(RenderTarget2D sceneRenderTarget, RenderTarget2D effectRenderTarget)
        {
            ScreenshotRequested = false;

            string name = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string ext = LocalSettings.Instance.ScreenshotFormat.ToString().ToLower();
            string path = SettingsManager.Instance.GetAbsPathInAppFolder($"Screenshots/{name}.{ext}");
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            switch (ScreenshotType)
            {
                case ScreenshotType.TransparentBackground:
                    if (LocalSettings.Instance.ScreenshotFormat == ScreenshotFormat.PNG)
                    {
                        ProcessTransparentScreenshot(sceneRenderTarget, effectRenderTarget, path);
                    }
                    else
                    {
                        ProcessScreenshot(sceneRenderTarget, effectRenderTarget, path, Color.Black);
                    }
                    break;
                case ScreenshotType.CustomBackgroundColor:
                    //A fully transparent custom background means the user wants the same result as the transparent screenshot hotkey
                    if (Viewport.ScreenshotBackgroundColor.A == 0 && LocalSettings.Instance.ScreenshotFormat == ScreenshotFormat.PNG)
                    {
                        ProcessTransparentScreenshot(sceneRenderTarget, effectRenderTarget, path);
                    }
                    else
                    {
                        ProcessScreenshot(sceneRenderTarget, effectRenderTarget, path, Viewport.ScreenshotBackgroundColor);
                    }
                    break;
            }

            Log.Add($"Screenshot saved in the XenoKit/Screenshots folder!");

        }

        private void ProcessScreenshot(RenderTarget2D sceneRenderTarget, RenderTarget2D effectRenderTarget, string path, Color clearColor)
        {
            RenderScreenshot(sceneRenderTarget, effectRenderTarget, clearColor);

            using (MemoryStream ms = new MemoryStream())
            {
                if(LocalSettings.Instance.ScreenshotFormat == ScreenshotFormat.PNG)
                {
                    ColorPassRT0.RenderTarget.SaveAsPng(ms, ScreenshotRT.Width, ScreenshotRT.Height);
                }
                else
                {
                    ColorPassRT0.RenderTarget.SaveAsJpeg(ms, ScreenshotRT.Width, ScreenshotRT.Height);
                }
                File.WriteAllBytes(path, ms.ToArray());
            }
        }

        private void ProcessTransparentScreenshot(RenderTarget2D sceneRenderTarget, RenderTarget2D effectRenderTarget, string path)
        {
            RenderScreenshot(sceneRenderTarget, effectRenderTarget, Color.Black);
            Color[] blackPixels = GetScreenshotPixels();

            RenderScreenshot(sceneRenderTarget, effectRenderTarget, Color.White);
            Color[] whitePixels = GetScreenshotPixels();

            using (MemoryStream ms = new MemoryStream())
            {
                SaveTransparentScreenshotPng(blackPixels, whitePixels, ScreenshotRT.Width, ScreenshotRT.Height, ms);
                File.WriteAllBytes(path, ms.ToArray());
            }
        }

        private void RenderScreenshot(RenderTarget2D sceneRenderTarget, RenderTarget2D effectRenderTarget, Color clearColor)
        {
            SetRenderTargets(ScreenshotRT.RenderTarget);
            GraphicsDevice.Clear(clearColor);
            DisplayRenderTarget(sceneRenderTarget);
            DisplayRenderTarget(effectRenderTarget);

            SetRenderTargets(ColorPassRT0.RenderTarget);
            SetTextures(ScreenshotRT.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            YBS.ApplyAxisCorrection();
        }

        private Color[] GetScreenshotPixels()
        {
            Color[] pixels = new Color[ColorPassRT0.RenderTarget.Width * ColorPassRT0.RenderTarget.Height];
            ColorPassRT0.RenderTarget.GetData(pixels);
            return pixels;
        }

        private void SaveTransparentScreenshotPng(Color[] blackPixels, Color[] whitePixels, int width, int height, Stream stream)
        {
            Color[] pixels = new Color[blackPixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = GetTransparentScreenshotPixel(blackPixels[i], whitePixels[i]);
            }

            using (Texture2D texture = new Texture2D(GraphicsDevice, width, height, false, SurfaceFormat.Color))
            {
                texture.SetData(pixels);
                texture.SaveAsPng(stream, width, height);
            }
        }

        private static Color GetTransparentScreenshotPixel(Color blackPixel, Color whitePixel)
        {
            int redDiff = Math.Max(0, whitePixel.R - blackPixel.R);
            int greenDiff = Math.Max(0, whitePixel.G - blackPixel.G);
            int blueDiff = Math.Max(0, whitePixel.B - blackPixel.B);
            int matteAlpha = 255 - Math.Max(redDiff, Math.Max(greenDiff, blueDiff));
            int additiveAlpha = Math.Max(blackPixel.R, Math.Max(blackPixel.G, blackPixel.B));
            int alpha = Math.Max(matteAlpha, additiveAlpha);

            if (alpha == 0)
            {
                return Color.Transparent;
            }

            return new Color(
                GetStraightColor(blackPixel.R, alpha),
                GetStraightColor(blackPixel.G, alpha),
                GetStraightColor(blackPixel.B, alpha),
                alpha);
        }

        private static int GetStraightColor(int color, int alpha)
        {
            return Math.Min(255, (color * 255 + (alpha / 2)) / alpha);
        }
    }

    public enum ScreenshotType
    {
        TransparentBackground,
        CustomBackgroundColor
    }

    public enum ScreenshotFormat
    {
        PNG,
        JPG
    }

}
