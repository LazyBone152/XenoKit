using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Shader.DXBC;
using Xv2CoreLib.EMD;

namespace XenoKit.Engine.Shader
{
    internal class Xv2Shader : Microsoft.Xna.Framework.Graphics.Shader
    {

        /// <summary>
        /// A hash value which can be used to compare shaders.
        /// </summary>
        internal int HashKey { get; private set; }

        public int[] GlobalSamplers { get; private set; }

        internal Xv2Shader(GraphicsDevice device, Xv2ShaderEffect effect, ShaderProgram shaderProgram, bool isVertexShader) : base(device)
        {
            GraphicsDevice = device;
            DxbcParser dxbcParser = (isVertexShader) ? shaderProgram.VsParser : shaderProgram.PsParser;

            Stage = isVertexShader ? ShaderStage.Vertex : ShaderStage.Pixel;
            Bytecode = isVertexShader ? shaderProgram.VS_Bytecode : shaderProgram.PS_Bytecode;

            //Add all other sampler slots that are present. These are "global" ones are will be shared between all shaders. We just need to get the slot number here as these will be loaded elsewhere.
            GlobalSamplers = new int[dxbcParser.ResourceBindings.Count(x => x.ShaderInputType == DxbcResourceBinding.ResourceBindingType.Sampler && x.BindPoint > 4)];
            int idx = 0;
            foreach(var sampler in dxbcParser.ResourceBindings.Where(x => x.ShaderInputType == DxbcResourceBinding.ResourceBindingType.Sampler && x.BindPoint > 4))
            {
                GlobalSamplers[idx] = sampler.BindPoint;
                idx++;
            }

            //Link ConstantBuffers to their index on Xv2Effect (which differs from the slot)
            var cbufferCount = dxbcParser.CBuffers.Length;
            CBuffers = new int[cbufferCount];
            for (var c = 0; c < cbufferCount; c++)
                CBuffers[c] = effect.IndexOfConstantBuffer(dxbcParser.CBuffers[c].Name);

        }

        public void SetGlobalSamplers()
        {
            foreach (int sampler in GlobalSamplers)
                ShaderManager.Instance.SetGlobalSampler(sampler, Stage == ShaderStage.Vertex);
        }
    }
}