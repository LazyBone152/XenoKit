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

        private void ProcessScreenshot(RenderTargetWrapper renderTarget)
        {
            ScreenshotRequested = false;

            string name = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string ext = LocalSettings.Instance.ScreenshotFormat.ToString().ToLower();
            string path = SettingsManager.Instance.GetAbsPathInAppFolder($"Screenshots/{name}.{ext}");
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            switch (ScreenshotType)
            {
                case ScreenshotType.TransparentBackground:
                    ProcessScreenshot(renderTarget, path, Color.Transparent);
                    break;
                case ScreenshotType.CustomBackgroundColor:
                    ProcessScreenshot(renderTarget, path, Viewport.ScreenshotBackgroundColor);
                    break;
            }

            Log.Add($"Screenshot saved in the XenoKit/Screenshots folder!");

        }

        private void ProcessScreenshot(RenderTargetWrapper renderTarget, string path, Color clearColor)
        {
            //Copying it to a seperate RT before saving allows changing of the background color
            SetRenderTargets(ScreenshotRT.RenderTarget);
            GraphicsDevice.Clear(clearColor);
            DisplayRenderTarget(renderTarget.RenderTarget);

            //Apply axis correction
            SetRenderTargets(ColorPassRT0.RenderTarget);
            SetTextures(ScreenshotRT.RenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            YBS.ApplyAxisCorrection();

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
