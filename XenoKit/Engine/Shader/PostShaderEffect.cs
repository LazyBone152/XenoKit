using ControlzEx.Standard;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.WIC;
using System;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Shader
{
    public class PostShaderEffect : Xv2ShaderEffect
    {
        public enum PostProccessShader
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

            //ExtShaders
            YBS_Copy,
            YBS_Pixel,
            YBS_Smooth,
            YBS_Glare,
            YBS_Dim,
            YBS_CopyRegion,
            YBS_Merge2,
            YBS_Merge5,
            YBS_Merge8,
            YBS_SceneMerge,
            LB_AxisCorrection
        }

        public PostProccessShader Shader { get; private set; }
        private YBSShaderParameters YBSParameters;

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
                    ImageSampler[1] = CreateSampler(TextureAddressMode.Clamp, TextureAddressMode.Wrap, TextureFilter.Point, 1);
                    break;
                case PostProccessShader.DepthToDepth:
                    ImageSampler[0] = CreateSampler(TextureAddressMode.Clamp, TextureAddressMode.Wrap, TextureFilter.Point, 0);
                    break;
                case PostProccessShader.YBS_Copy:
                case PostProccessShader.YBS_Dim:
                    ImageSampler[0] = CreateSampler(TextureAddressMode.Mirror, TextureAddressMode.Clamp, TextureFilter.MinLinearMagPointMipPoint, 0);
                    break;
                case PostProccessShader.YBS_CopyRegion:
                    ImageSampler[0] = CreateSampler(TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureFilter.MinLinearMagPointMipPoint, 0);
                    ImageSampler[0] = CreateSampler(TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureFilter.MinLinearMagPointMipPoint, 1);
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
            //Reload parameters
            if (IsDirty)
            {
                SetParameters();
                IsDirty = false;
            }

            //Reload entire shader
            if (IsShaderProgramDirty)
            {
                InitTechnique();
                IsShaderProgramDirty = false;
            }

            ApplyStates();

            Matrix world = Matrix.Identity;
            Matrix viewMatrix = Matrix.Identity;
            Matrix projMatrix = Matrix.CreateOrthographicOffCenter(
                0, GraphicsDevice.Viewport.Width,     // Left and right coordinates
                GraphicsDevice.Viewport.Height, 0,    // Top and bottom coordinates
                0, 1);              // Near and far plane distance;

            g_mWVP_VS?.SetValue(world * viewMatrix * projMatrix);

            if (shaderProgram.ShaderSource == ShaderProgramSource.Xenoverse)
            {
                g_mVP_VS?.SetValue(viewMatrix * projMatrix);
                g_mWVP_Prev_VS?.SetValue(PrevWVP);
                g_mWV_VS?.SetValue(world * viewMatrix);
                g_mW_VS?.SetValue(world);
                g_mWLP_SM_VS?.SetValue(projMatrix);

                PrevWVP = world * viewMatrix * projMatrix;
            }

            switch (Shader)
            {
                case PostProccessShader.EDGELINE_VFX:
                    Parameters["g_vParam0_PS"]?.SetValue(new Vector4(0.00104f, 0.00185f, 0.00026f, 0.00046f));
                    Parameters["g_vParam1_PS"]?.SetValue(new Vector4(0, 1f, 10f, 0));
                    break;
                case PostProccessShader.AGE_TEST_EDGELINE_MRT:
                    Parameters["g_vParam0_PS"]?.SetValue(new Vector4(0.0f, 9, 3f, 0.6f));
                    //Parameters["g_vParam1_PS"]?.SetValue(new Vector4(0.00039f, 0.00069f, 3f, 0.6f));
                    float factor = GameBase.RenderSystem.SuperSampleFactor > 1 ? 0.85f : 1f;
                    Parameters["g_vParam1_PS"]?.SetValue(new Vector4(0.00039f, 0.00055f, 3f, 0.6f) * factor);
                    break;
                case PostProccessShader.AGE_TEST_DEPTH_TO_PFXD:
                    Parameters["g_vParam0_PS"]?.SetValue(new Vector4(0.04187f, 0.95813f, 80f, 0f));
                    Parameters["g_vScreen_VS"]?.SetValue(new Vector4(GameBase.RenderSystem.RenderWidth, GameBase.RenderSystem.RenderHeight, 0.10f, 10106.85645f));
                    break;
                case PostProccessShader.BIRD_BG_EDGELINE_RGB_HF:
                    Parameters["g_vEdge_PS"]?.SetValue(new Vector4(0.1f, 0.1f, 0.1f, 5f));
                    Parameters["g_vParam0_PS"]?.SetValue(new Vector4(0.034f, 0.885f, 0.85f, 0.138f));
                    Parameters["g_vParam1_PS"]?.SetValue(new Vector4(1.724f, 0.00f, 0.00f, 0.00f));
                    break;
                case PostProccessShader.YBS_Copy:
                case PostProccessShader.YBS_Pixel:
                case PostProccessShader.YBS_Glare:
                case PostProccessShader.YBS_Smooth:
                    Parameters["afRGBA_Modulate"]?.SetValue(YBSParameters.afRGBA_Modulate);
                    Parameters["afUV_TexCoordOffsetV16"]?.SetValue(YBSParameters.afUV_TexCoordOffsetV16);
                    break;
                case PostProccessShader.YBS_Dim:
                    Parameters["afRGBA_Modulate"].SetValue(YBSParameters.afRGBA_Modulate);
                    Parameters["afUV_TexCoordOffsetV16"].SetValue(YBSParameters.afUV_TexCoordOffsetV16);
                    Parameters["fParam_HDRFormatFactor_LOGRGB"].SetValue(YBSParameters.fParam_HDRFormatFactor_LOGRGB);
                    break;
                case PostProccessShader.YBS_Merge2:
                case PostProccessShader.YBS_Merge5:
                case PostProccessShader.YBS_Merge8:
                    Parameters["afRGBA_Modulate"].SetValue(YBSParameters.afRGBA_Modulate);
                    break;
                case PostProccessShader.YBS_SceneMerge:
                    Parameters["fParam_DitherOffsetScale"].SetValue(YBSParameters.fParam_DitherOffsetScale);
                    Parameters["afRGBA_Modulate"].SetValue(YBSParameters.afRGBA_Modulate);
                    Parameters["afRGBA_Offset"].SetValue(YBSParameters.afRGBA_Offset);
                    break;
            }
        }

        public void SetParameters(YBSShaderParameters parameters)
        {
            YBSParameters = parameters;

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

            if (shaderProgram.ShaderSource == ShaderProgramSource.External)
            {
                blendState.IndependentBlendEnable = false;
                blendState.ApplyCustom(0, BlendFunction.Add, Blend.One, Blend.Zero);
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
            RasterizerState rs = new RasterizerState()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                DepthBias = 0,
                DepthClipEnable = true,
                ScissorTestEnable = false,
                MultiSampleAntiAlias = false,
                SlopeScaleDepthBias = 0
            };

            if (shaderProgram.ShaderSource == ShaderProgramSource.External)
            {
                rs.DepthClipEnable = false;
                rs.MultiSampleAntiAlias = true;
            }

            return rs;
        }

    }

    public struct YBSShaderParameters
    {
        public Vector2 fParam_ScreenSpaceScale;
        public Vector4 fParam_HDRFormatFactor_LOGRGB;
        public Vector4[] afUV_TexCoordOffsetV16; //16
        public Vector4[] afUV_TexCoordOffsetP32; // 96
        public Vector4[] afRGBA_Modulate; //32
        public Vector4[] afRGBA_Offset; //16
        public Vector4 fParam_DitherOffsetScale;
        public Vector4 DefaultColor;

        public static YBSShaderParameters Default()
        {
            return new YBSShaderParameters()
            {
                fParam_ScreenSpaceScale = new Vector2(1f, -1f),
                fParam_HDRFormatFactor_LOGRGB = new Vector4(5.09844f, 0.19614f, 162.76665f, 0.00614f),
                afUV_TexCoordOffsetV16 = new Vector4[16],
                afUV_TexCoordOffsetP32 = new Vector4[96],
                afRGBA_Modulate = new Vector4[32],
                afRGBA_Offset = new Vector4[16],
                fParam_DitherOffsetScale = new Vector4(0.00392156886f, 0.00392156886f, -0.00392156886f, 0f),
                DefaultColor = Vector4.Zero
            };
        }

        public void SetModulate(float value, int startIdx, int endIdx)
        {
            for(int i = startIdx; i <= endIdx; i++)
            {
                afRGBA_Modulate[i] = new Vector4(value);
            }
        }

        public void SetGlareModulate()
        {
            afRGBA_Modulate[0] = new Vector4(0.44327f, 0.50483f, 0.58626f, 1.73205f);
            afRGBA_Modulate[1] = new Vector4(0.36083f, 0.38658f, 0.40905f, 0.00f);
            afRGBA_Modulate[2] = new Vector4(0.36083f, 0.38658f, 0.40905f, 0.00f);
            afRGBA_Modulate[3] = new Vector4(0.19464f, 0.17359f, 0.13894f, 0.00f);
            afRGBA_Modulate[4] = new Vector4(0.19464f, 0.17359f, 0.13894f, 0.00f);
            afRGBA_Modulate[5] = new Vector4(0.06957f, 0.04571f, 0.02298f, 0.00f);
            afRGBA_Modulate[6] = new Vector4(0.06957f, 0.04571f, 0.02298f, 0.00f);
            afRGBA_Modulate[7] = new Vector4(0.01648f, 0.00706f, 0.00185f, 0.00f);
            afRGBA_Modulate[8] = new Vector4(0.01648f, 0.00706f, 0.00185f, 0.00f);
        }

        public void SetMergeModulate(int mergeCount, Vector4 moduleVector)
        {
            for(int i = 0; i < mergeCount; i++)
            {
                afRGBA_Modulate[i] = moduleVector;

                moduleVector /= 2;
            }
        }

        public void SetSmoothTexCoordV16(Vector4 baseCoords)
        {
            afUV_TexCoordOffsetV16[0] = baseCoords;
            afUV_TexCoordOffsetV16[1] = new Vector4(-baseCoords.X, baseCoords.Y, baseCoords.Z, baseCoords.W);
            afUV_TexCoordOffsetV16[2] = baseCoords;
            afUV_TexCoordOffsetV16[3] = afUV_TexCoordOffsetV16[1];
        }

        public void SetSceneMergeValues()
        {
            fParam_DitherOffsetScale = new Vector4(0.00392f, 0.00392f, -0.00392f, 0f);
            afRGBA_Modulate[0] = new Vector4(1.00f, 1.00f, 1.00f, 0.00f);
            afRGBA_Modulate[1] = new Vector4(0, 0, 0, 1);
            afRGBA_Offset[0] = new Vector4(2, -1, 0, 0);
            afRGBA_Offset[1] = new Vector4(0.5f, 0, 0, 0);
            afRGBA_Offset[2] = new Vector4(2, -1, 0, 1);
        }

        public void SetCopyValues()
        {
            afRGBA_Modulate[0] = Vector4.One;
            afUV_TexCoordOffsetV16[0] = Vector4.Zero;
        }
    }
}
