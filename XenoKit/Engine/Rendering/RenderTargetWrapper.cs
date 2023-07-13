using Microsoft.Xna.Framework.Graphics;
using System;

namespace XenoKit.Engine.Rendering
{
    /// <summary>
    /// Wrapper class for <see cref="RenderTarget2D"/> instances that handles auto-updating and disposing of the targets when the viewport dimensions change.
    /// </summary>
    public class RenderTargetWrapper
    {
        private GraphicsDevice graphicsDevice;
        public RenderTarget2D RenderTarget { get; private set; }
        public string Name { get; private set; }

        private bool useDepthBuffer;
        private SurfaceFormat surfaceFormat;
        private int ResolutionScale;
        private int WidthAtInit;
        private int HeightAtInit;

        public int Width => RenderTarget.Width;
        public int Height => RenderTarget.Height;

        public RenderTargetWrapper(GraphicsDevice graphicsDevice, int resScale, SurfaceFormat surfaceFormat, bool depthBuffer, string name = "")
        {
            if (resScale == 0)
                throw new ArgumentException("RenderTargetWrapper: resScale cannot be 0.");

            Name = name;
            this.useDepthBuffer = depthBuffer;
            this.surfaceFormat = surfaceFormat;
            this.graphicsDevice = graphicsDevice;
            ResolutionScale = resScale;
            UpdateRenderTarget();
        }

        public void UpdateRenderTarget()
        {
            WidthAtInit = graphicsDevice.Viewport.Width;
            HeightAtInit = graphicsDevice.Viewport.Height;

            //Some RenderTargets are rendered at a lower internal resolution than the screen res, such as "LowRez" (1/2) and "LowRezSmoke" (1/4) material draw passes
            int height = graphicsDevice.Viewport.Height / ResolutionScale;
            int width = graphicsDevice.Viewport.Width / ResolutionScale;

            RenderTarget = new RenderTarget2D(graphicsDevice, width, height, false, surfaceFormat, useDepthBuffer ? DepthFormat.Depth24Stencil8 : DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            //Disposal of previous RenderTarget is handled in RenderSystem.cs
        }

        public bool ShouldUpdate()
        {
            return WidthAtInit != graphicsDevice.Viewport.Width || HeightAtInit != graphicsDevice.Viewport.Height;
        }
    }
}
