using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Engine.Lighting;
using XenoKit.Engine.Shader;
using Xv2CoreLib.EMM;

namespace XenoKit.Engine.Rendering
{
    public class ScreenSpacePointLight : EngineObject
    {
        private readonly PostShaderEffect Decal;
        private readonly Xv2ShaderEffect SSPL_BBL_RANGE_SQUARE_FAST_S15;

        private readonly VertexPosition[] Vertices;
        private readonly short[] Indices;
        private readonly VertexBuffer VertexBuffer;
        private readonly IndexBuffer IndexBuffer;

        private readonly SamplerState Sampler;

        public ScreenSpacePointLight()
        {
            Decal = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("Decal"));
            SSPL_BBL_RANGE_SQUARE_FAST_S15 = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(EmmMaterial.CreateDefaultMaterial("SSPL_BBL_RANGE_SQUARE_FAST_S15"));
            
            //Create box vertices and indices
            Vertices = new VertexPosition[8];

            Vertices[0] = new VertexPosition(new Vector3(-1, -1, -1));
            Vertices[1] = new VertexPosition(new Vector3(-1, 1, -1));
            Vertices[2] = new VertexPosition(new Vector3(1, -1, -1));
            Vertices[3] = new VertexPosition(new Vector3(1, 1, -1));
            Vertices[4] = new VertexPosition(new Vector3(1, -1, 1));
            Vertices[5] = new VertexPosition(new Vector3(1, 1, 1));
            Vertices[6] = new VertexPosition(new Vector3(-1, -1, 1));
            Vertices[7] = new VertexPosition(new Vector3(-1, 1, 1));

            Indices =
            [
                0, 1, 2, 2, 1, 3, 2, 3,
                4, 4, 3, 5, 4, 5, 6, 6,
                5, 7, 5, 3, 7, 7, 3, 1,
                6, 7, 0, 0, 7, 1, 0, 2,
                6, 6, 2, 4
            ];

            VertexBuffer = new VertexBuffer(GraphicsDevice, VertexPosition.VertexDeclaration, 8, BufferUsage.WriteOnly);
            VertexBuffer.SetData(Vertices);

            IndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, Indices.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(Indices);

            Sampler = new SamplerState()
            {
                Name = "State_ImageSampler0",
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Wrap,
                Filter = TextureFilter.Point,
                BorderColor = Color.White,
                ComparisonFunction = CompareFunction.Never
            };
        }

        public bool Apply()
        {
            //Apply SSPL pass

            if (LightSystem.CalculateValidStagePointLights(out int count, out PointLight[] pointLights, out var ssplParam))
            {
                //Main SSPL draw
                GraphicsDevice.SamplerStates[0] = Sampler;
                GraphicsDevice.Textures[0] = RenderSystem.GetSamplerAlphaDepthRT().RenderTarget;
                SSPL_BBL_RANGE_SQUARE_FAST_S15.ApplySSPLParameters(count, pointLights, ssplParam);

                foreach (EffectPass pass in SSPL_BBL_RANGE_SQUARE_FAST_S15.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    GraphicsDevice.SetVertexBuffer(VertexBuffer);
                    GraphicsDevice.Indices = IndexBuffer;
                    GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 12, count);
                }

                return true;
            }

            return false;
        }

        public void Apply(RenderTargetWrapper mergeTarget, RenderTargetWrapper depthBuffer)
        {
            //Apply SSPL pass

            /*
            if (LightSystem.CalculateValidStagePointLights(out int count, out PointLight[] pointLights, out var ssplParam))
            {
                //Main SSPL draw
                RenderSystem.SetRenderTargets(SsplRT.RenderTarget);
                GraphicsDevice.Clear(Color.Black);
                GraphicsDevice.SamplerStates[0] = Sampler;
                GraphicsDevice.Textures[0] = SamplerAlphaDepth.RenderTarget;
                SSPL_BBL_RANGE_SQUARE_FAST_S15.ApplySSPLParameters(count, pointLights, ssplParam);

                foreach (EffectPass pass in SSPL_BBL_RANGE_SQUARE_FAST_S15.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    GraphicsDevice.SetVertexBuffer(VertexBuffer);
                    GraphicsDevice.Indices = IndexBuffer;
                    GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 12, 15);
                }

                //Merge back to the main RT
                RenderSystem.SetRenderTargets(mergeTarget.RenderTarget);
                GraphicsDevice.SetDepthBuffer(depthBuffer.RenderTarget);
                GraphicsDevice.Textures[0] = SsplRT.RenderTarget;
                RenderSystem.PostFilter.Apply(Decal);

            }
            */
        }
    }
}
