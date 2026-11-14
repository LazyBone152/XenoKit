using Microsoft.Xna.Framework.Graphics;
using System.IO;
using XenoKit.Editor;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Rendering
{
    public partial class RenderSystem : RenderObject
    {
        /// <summary>
        /// Register a <see cref="RenderTargetWrapper"/> with this <see cref="RenderSystem"/> instance. Registered RenderTargets will be automatically updated when the viewport changes size.
        /// </summary>
        public void RegisterRenderTarget(RenderTargetWrapper renderTarget)
        {
            registeredRenderTargets.Add(renderTarget);
        }

        public RenderTarget2D GetFinalRenderTarget()
        {
            return FinalRenderTarget.RenderTarget;
        }

        public RenderTargetWrapper GetColorPassRT0()
        {
            return ColorPassRT0;
        }

        public RenderTargetWrapper GetShaderRT()
        {
            return ShadowPassRT0;
        }

        public RenderTargetWrapper GetNormalRT()
        {
            return NormalPassRT0;
        }

        public RenderTargetWrapper GetSamplerAlphaDepthRT()
        {
            return SamplerAlphaDepth;
        }

        public RenderTargetWrapper GetReflectionRT()
        {
            return ReflectionRT;
        }

        public RenderTargetWrapper GetSmallSceneRT()
        {
            return SmallSceneRT;
        }

        private void DumpRenderTargets()
        {
            DumpRenderTargetsNextFrame = false;
            bool dumpShadowMap = DumpShadowMapNextFrame;
            DumpShadowMapNextFrame = false;

            Directory.CreateDirectory(SettingsManager.Instance.GetAbsPathInAppFolder("RT_Dump"));

            foreach (RenderTargetWrapper renderTarget in registeredRenderTargets)
            {
                if (dumpShadowMap && renderTarget.Name != nameof(RenderSystem.ShadowPassRT0)) continue;

                if (!string.IsNullOrEmpty(renderTarget.Name))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        renderTarget.RenderTarget.SaveAsPng(ms, renderTarget.Width, renderTarget.Height);
                        try
                        {
                            File.WriteAllBytes(SettingsManager.Instance.GetAbsPathInAppFolder($"RT_Dump/{renderTarget.Name}.png"), ms.ToArray());
                        }
                        catch (IOException)
                        {
                            Log.Add("Dump failed, close the file first", LogType.Error);
                            return;
                        }
                    }
                }
            }

            Log.Add("Render Targets dumped", LogType.Info);
        }

    }
}
