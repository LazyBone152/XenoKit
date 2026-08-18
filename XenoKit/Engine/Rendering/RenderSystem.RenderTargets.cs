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
        public void RegisterRenderTarget(RenderTargetWrapper renderTarget)
        {
            registeredRenderTargets.Add(renderTarget);
        }

        public RenderTarget2D GetFinalRenderTarget()
        {
            return FinalRenderTarget.RenderTarget;
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

    }
}
