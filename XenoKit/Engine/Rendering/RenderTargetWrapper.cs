using Microsoft.Xna.Framework.Graphics;
using System;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Rendering
{
    /// <summary>
    /// Wrapper class for <see cref="RenderTarget2D"/> instances that handles auto-updating and disposing of the targets when the viewport dimensions change.
    /// </summary>
    public class RenderTargetWrapper
    {
        private RenderSystem renderSystem;
        public RenderTarget2D RenderTarget { get; private set; }
        public string Name { get; private set; }

        private DepthFormat depthFormat;
        private SurfaceFormat surfaceFormat;
        private int ResolutionScale;
        private int MultiSampleCount;
        private int WidthAtInit;
        private int HeightAtInit;
        private bool FullLowRezAtInit = SettingsManager.settings.XenoKit_FullLowRez;

        private bool isShadowMap = false;
        public int Width => RenderTarget.Width;
        public int Height => RenderTarget.Height;

        public RenderTargetWrapper(RenderSystem renderSystem, int resScale, SurfaceFormat surfaceFormat, bool depthBuffer, string name = "", int msaaCount = 0)
        {
            if (resScale == 0)
                throw new ArgumentException("RenderTargetWrapper: resScale cannot be 0.");

            Name = name;
            this.depthFormat = depthBuffer ? DepthFormat.Depth24Stencil8 : DepthFormat.None;
            this.surfaceFormat = surfaceFormat;
            this.renderSystem = renderSystem;
            MultiSampleCount = msaaCount;
            ResolutionScale = resScale;
            UpdateRenderTarget();
        }

        private RenderTargetWrapper(RenderSystem renderSystem, SurfaceFormat surfaceFormat, DepthFormat depthFormat, string name = "", int msaaCount = 0) 
        {
            this.renderSystem = renderSystem;
            Name = name;
            this.surfaceFormat = surfaceFormat;
            this.depthFormat = depthFormat;
            MultiSampleCount = msaaCount;
            ResolutionScale = 1;
        }

        public static RenderTargetWrapper CreateShadowMap(RenderSystem renderSystem)
        {
            RenderTargetWrapper rt = new RenderTargetWrapper(renderSystem, SurfaceFormat.Single, DepthFormat.Depth16, nameof(RenderSystem.ShadowPassRT0));
            rt.WidthAtInit = SettingsManager.settings.XenoKit_ShadowMapRes;
            rt.HeightAtInit = SettingsManager.settings.XenoKit_ShadowMapRes;
            rt.isShadowMap = true;
            rt.UpdateRenderTarget();
            return rt;
        }

        public void UpdateRenderTarget()
        {
            int height, width;

            if (isShadowMap)
            {
                height = HeightAtInit;
                width = WidthAtInit;
            }
            else
            {
                WidthAtInit = (int)renderSystem.RenderResolution[0];
                HeightAtInit = (int)renderSystem.RenderResolution[1];

                int resScale = (SettingsManager.settings.XenoKit_FullLowRez) ? 1 : ResolutionScale;

                //Some RenderTargets are rendered at a lower internal resolution than the screen res, such as "LowRez" (1/2) and "LowRezSmoke" (1/4) material draw passes
                height = HeightAtInit / resScale;
                width = WidthAtInit / resScale;
            }

            RenderTarget = new RenderTarget2D(renderSystem.GraphicsDevice, width, height, false, surfaceFormat, depthFormat, MultiSampleCount, RenderTargetUsage.PreserveContents);

            FullLowRezAtInit = SettingsManager.settings.XenoKit_FullLowRez;
            //Disposal of previous RenderTarget is handled in RenderSystem.cs
        }

        public bool ShouldUpdate()
        {
            if (isShadowMap)
            {
                return WidthAtInit != SettingsManager.settings.XenoKit_ShadowMapRes || HeightAtInit != SettingsManager.settings.XenoKit_ShadowMapRes;
            }
            else
            {
                //If FullLowRez option has been changed and this RT has a ResScale applied, then it needs to be updated as well.
                if (ResolutionScale > 1 && FullLowRezAtInit != SettingsManager.settings.XenoKit_FullLowRez) return true;

                return WidthAtInit != renderSystem.RenderResolution[0] || HeightAtInit != renderSystem.RenderResolution[1];
            }
        }
    }
}
