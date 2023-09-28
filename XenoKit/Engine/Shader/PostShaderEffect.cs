using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Xv2CoreLib.EMM;

namespace XenoKit.Engine.Shader
{
    public class PostShaderEffect : Xv2ShaderEffect
    {
        private enum PostProccessShader
        {
            Undefined,
            AGE_TEST_EDGELINE_MRT, //The post shader that adds the black outline around characters
            BIRD_BG_EDGELINE_RGB_HF, //Adds black outline around the stage enviroment edges
            DepthToDepth, //Copies depth from the input render target onto the current
            NineConeFilter, //Blur filter
            AGE_MERGE_AddLowRez_AddMrt, //Merges the "LowRez" render targets into another (the primary RT)
            Sampler0, //Copy one RT (input) onto another (current), and turns alpha black (?). Probably not needed since SpriteBatch can do this
            EDGELINE_VFX, //BPE "BodyOutline" shader. Relies on 2 inputs: NormalPassRT1 and a texture that contains the outline color (16x16, though only the second pixel on the first line is used)
            AGE_TEST_DEPTH_TO_PFXD,

        }

        private readonly PostProccessShader Shader;

        //Samplers:
        public readonly SamplerState[] ImageSampler = new SamplerState[4];

        public PostShaderEffect(ShaderProgram shaderProgram, GameBase game) : base(game)
        {
            if (!Enum.TryParse(shaderProgram.Name, out PostProccessShader type))
                type = PostProccessShader.Undefined;

            Shader = type;
            GraphicsDevice = game.GraphicsDevice;
            Material = EmmMaterial.NewMaterial();
            Material.ShaderProgram = shaderProgram.Name;
            this.shaderProgram = shaderProgram;
            ShaderType = ShaderType.PostFilter;
            GameBase = game;

            InitSamplers();
            InitTechnique();
        }

        private void InitSamplers()
        {
            switch (Shader)
            {
                case PostProccessShader.AGE_TEST_DEPTH_TO_PFXD:
                    ImageSampler[0] = CreateSampler(TextureAddressMode.Clamp, TextureAddressMode.Wrap, TextureFilter.Point, 0);
                    break;
                case PostProccessShader.EDGELINE_VFX:
                    ImageSampler[0] = CreateSampler(TextureAddressMode.Clamp, TextureAddressMode.Wrap, TextureFilter.Point, 0);
                    ImageSampler[1] = CreateSampler(TextureAddressMode.Clamp, TextureAddressMode.Wrap, TextureFilter.Point, 0);
                    break;
                case PostProccessShader.DepthToDepth:
                    ImageSampler[0] = CreateSampler(TextureAddressMode.Clamp, TextureAddressMode.Wrap, TextureFilter.Point, 0);
                    break;
            }

            //Create remaining samplers
            for (int i = 0; i < 4; i++)
            {
                if(ImageSampler[i] == null)
                {
                    ImageSampler[i] = CreateSampler(TextureAddressMode.Clamp, TextureAddressMode.Wrap, TextureFilter.Linear, i);
                }
            }
        }

        private SamplerState CreateSampler(TextureAddressMode addUV, TextureAddressMode addW, TextureFilter textureFilter, int texSlot)
        {
            return new SamplerState()
            {
                AddressU = addUV,
                AddressV = addUV,
                AddressW = addW,
                BorderColor = Color.White,
                MaxAnisotropy = 1,
                ComparisonFunction = CompareFunction.Never,
                Filter = textureFilter,
                MipMapLevelOfDetailBias = 0,
                Name = GameBase.ShaderManager.GetSamplerName(texSlot),
                FilterMode = TextureFilterMode.Default,
                GraphicsDevice = GameBase.GraphicsDevice
            };
        }

        protected override void SetParameters()
        {
            //base.SetParameters();

            g_mWVP_VS = Parameters["g_mWVP_VS"];
            g_mVP_VS = Parameters["g_mVP_VS"];
            g_mWVP_Prev_VS = Parameters["g_mWVP_Prev_VS"];
            g_mWV_VS = Parameters["g_mWV_VS"];
            g_mW_VS = Parameters["g_mW_VS"];
            g_mWLP_SM_VS = Parameters["g_mWLP_SM_VS"];
        }

        protected override void OnApply()
        {
            Matrix world = Matrix.Identity;
            Matrix viewMatrix = Matrix.Identity;
            Matrix projMatrix = Matrix.CreateOrthographicOffCenter(
                0, GraphicsDevice.Viewport.Width,     // Left and right coordinates
                GraphicsDevice.Viewport.Height, 0,    // Top and bottom coordinates
                0, 1);              // Near and far plane distance;

            g_mWVP_VS.SetValue(world * viewMatrix * projMatrix);
            g_mVP_VS.SetValue(viewMatrix * projMatrix);
            g_mWVP_Prev_VS.SetValue(PrevWVP);
            g_mWV_VS.SetValue(world * viewMatrix);
            g_mW_VS.SetValue(world);
            g_mWLP_SM_VS.SetValue(projMatrix);

            PrevWVP = world * viewMatrix * projMatrix;

            switch (Shader)
            {
                case PostProccessShader.EDGELINE_VFX:
                    Parameters["g_vParam0_PS"].SetValue(new Vector4(0.00104f, 0.00185f, 0.00026f, 0.00046f));
                    Parameters["g_vParam1_PS"].SetValue(new Vector4(0, 1f, 10f, 0));
                    break;
                case PostProccessShader.AGE_TEST_EDGELINE_MRT:
                    Parameters["g_vParam0_PS"].SetValue(new Vector4(0.0f, 9, 3f, 0.6f));
                    Parameters["g_vParam1_PS"].SetValue(new Vector4(0.00039f, 0.00069f, 3f, 0.6f));
                    break;
                case PostProccessShader.AGE_TEST_DEPTH_TO_PFXD:
                    Parameters["g_vParam0_PS"].SetValue(new Vector4(0.04187f, 0.95813f, 80f, 0f));
                    Parameters["g_vScreen_VS"].SetValue(new Vector4(GameBase.RenderSystem.RenderWidth, GameBase.RenderSystem.RenderHeight, 0.10f, 10106.85645f));
                    break;
                case PostProccessShader.BIRD_BG_EDGELINE_RGB_HF:
                    Parameters["g_vEdge_PS"].SetValue(new Vector4(0.1f, 0.1f, 0.1f, 5f));
                    Parameters["g_vParam0_PS"].SetValue(new Vector4(0.034f, 0.885f, 0.85f, 0.138f));
                    Parameters["g_vParam1_PS"].SetValue(new Vector4(1.724f, 0.00f, 0.00f, 0.00f));
                    break;
            }
        }

        public override BlendState GetBlendState()
        {
            BlendState blendState = new BlendState();
            blendState.IndependentBlendEnable = true;
            blendState.ApplyAlphaBlend(0);
            blendState.ApplyAlphaBlend(1);
            blendState.ApplyNone(2);

            switch (Shader)
            {
                case PostProccessShader.EDGELINE_VFX:
                    blendState.ApplyCustom(0, BlendFunction.Add, Blend.One, Blend.One);
                    blendState.ApplyCustom(1, BlendFunction.Add, Blend.One, Blend.One);
                    break;
                case PostProccessShader.AGE_TEST_EDGELINE_MRT:
                    blendState[1].ColorWriteChannels = 0;
                    blendState[2].ColorWriteChannels = ColorWriteChannels.Green;
                    break;
                case PostProccessShader.BIRD_BG_EDGELINE_RGB_HF:
                    blendState.ApplyNone(0);
                    blendState.ApplyNone(1);
                    blendState[1].ColorWriteChannels = 0;
                    break;
                case PostProccessShader.AGE_TEST_DEPTH_TO_PFXD:
                    blendState.ApplyNone(0);
                    break;
                case PostProccessShader.DepthToDepth:
                    blendState.ApplyNone(0);
                    blendState.ApplyNone(1);
                    break;
                case PostProccessShader.AGE_MERGE_AddLowRez_AddMrt:
                    blendState.ApplyCustom(0, BlendFunction.Add, Blend.One, Blend.SourceAlpha, ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue);
                    blendState.ApplyCustom(1, BlendFunction.Add, Blend.One, Blend.SourceAlpha);
                    break;
            }

            return blendState;
        }

        public override DepthStencilState GetDepthState()
        {
            DepthStencilState depth = new DepthStencilState();
            depth.DepthBufferEnable = false;
            depth.DepthBufferFunction = CompareFunction.Never;
            depth.DepthBufferWriteEnable = false;

            switch (Shader)
            {
                case PostProccessShader.AGE_TEST_EDGELINE_MRT:
                    depth.StencilEnable = true;
                    depth.StencilMask = 80;
                    depth.StencilWriteMask = 80;
                    depth.ReferenceStencil = 80;
                    depth.CounterClockwiseStencilFunction = CompareFunction.Equal;
                    depth.StencilFunction = CompareFunction.Equal;
                    depth.DepthBufferFunction = CompareFunction.LessEqual;
                    break;
                case PostProccessShader.AGE_TEST_DEPTH_TO_PFXD:
                    depth.DepthBufferFunction = CompareFunction.Always;
                    break;
                case PostProccessShader.DepthToDepth:
                    depth.DepthBufferEnable = true;
                    depth.DepthBufferWriteEnable = true;
                    depth.DepthBufferFunction = CompareFunction.Always;
                    break;
            }

            return depth;
        }

        public override RasterizerState GetRasterizerState()
        {
            return new RasterizerState()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                DepthBias = 0,
                DepthClipEnable = true,
                ScissorTestEnable = false,
                MultiSampleAntiAlias = false,
                SlopeScaleDepthBias = 0
            };
        }

    }

}
