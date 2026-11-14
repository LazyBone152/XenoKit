using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Lighting;
using XenoKit.Engine.Stage;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.SDS;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;
using SimdVector4 = System.Numerics.Vector4;

namespace XenoKit.Engine.Shader
{
    /// <summary>
    /// Represents a DBXV2 shader instance (ShaderProgram and Materials).
    /// </summary>
    public class Xv2ShaderEffect : Xv2ShaderEffectBase
    {
        private static Vector4 HC_Param11 = new Vector4(1.10f, 0.20f, 0.80f, 0.10f);
        private static Vector4 HC_Param12 = new Vector4(-0.55689f, 0.79556f, -0.23867f, 1.00f);
        private static Vector4 HC_Param13 = new Vector4(8.00f, 0.40f, 0.00f, 0.00f);
        private static Vector4 HC_UserFlag1 = new Vector4(7936f, 0, 0.00f, 0.00f);
        private const float VanishShaderFlag = 115f;
        private static readonly Vector4 DefaultBackgroundGlareTone = new Vector4(0.5f, 0.5f, 0.5f, 3.75f);
        private static readonly Vector4 DefaultEffectGlareTone = new Vector4(0.9f, 0.9f, 0.9f, 0.95f);

        //Settings
        public bool IsSubtractiveBlending { get; protected set; }
        private bool SkinningEnabled { get; set; }
        public int ActorSlot { get; set; }

        //Updating
        public bool IsShaderProgramDirty { get; set; }
        public bool IsDirty { get; set; }
        private bool glareOutputAllowed = true;

        //Color Fade
        private float[] ECF_Multi = null;
        private float[] ECF_Rim = null;
        private float[] ECF_Add = null;

        private float[] TexScrl0, TexScrl1 = null;
        private float[] MatCol0, MatCol1, MatCol2, MatCol3 = null;

        #region Init
        public Xv2ShaderEffect(EmmMaterial material, ShaderType type) : this()
        {
            GraphicsDevice = Viewport.Instance.GraphicsDevice;
            Material = material;
            ShaderType = type;

            InitTechnique();

            Material.DecompiledParameters.ParametersChanged += DecompiledParameters_ParametersChanged;
            Material.PropertyChanged += Material_PropertyChanged;
            Xv2Stage.CurrentSpmChanged += Xv2Stage_CurrentSpmChanged;
            SettingsManager.SettingsSaved += SettingsManager_SettingsChanged;
        }

        protected Xv2ShaderEffect(EmmMaterial material, bool delayInit) : this()
        {
            GraphicsDevice = Viewport.Instance.GraphicsDevice;
            Material = material;

            if(!delayInit)
                InitTechnique();

            Material.DecompiledParameters.ParametersChanged += DecompiledParameters_ParametersChanged;
            Material.PropertyChanged += Material_PropertyChanged;
            Xv2Stage.CurrentSpmChanged += Xv2Stage_CurrentSpmChanged;
            SettingsManager.SettingsSaved += SettingsManager_SettingsChanged;
        }

        protected Xv2ShaderEffect() : base(Viewport.Instance.GraphicsDevice)
        {
        }

        //Render States2
        public override BlendState GetBlendState()
        {
            BlendState blendState = new BlendState();
            blendState.IndependentBlendEnable = true;
            IsSubtractiveBlending = false;

            if (ShaderType == ShaderType.CharaNormals || ShaderType == ShaderType.CharaShadow)
            {
                blendState.ApplyNone(0);
                blendState.ApplyNone(1);

                return blendState;
            }
            else if (ShaderType == ShaderType.Stage)
            {
                blendState.ApplyNone(0);
                blendState.ApplyNone(1);
                blendState[0].ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;

                //return blendState;
            }

            //Default path; used for effects (PBIND, TBIND and EMO) and regular character shaders
            switch (MatParam.AlphaBlend > 0 ? MatParam.AlphaBlendType : -1)
            {
                case 0: //Normal
                    //BlendState 0
                    blendState[0].ColorSourceBlend = Blend.SourceAlpha;
                    blendState[0].ColorDestinationBlend = Blend.InverseSourceAlpha;
                    blendState[0].ColorBlendFunction = BlendFunction.Add;

                    blendState[0].AlphaSourceBlend = Blend.SourceAlpha;
                    blendState[0].AlphaDestinationBlend = Blend.InverseSourceAlpha;
                    blendState[0].AlphaBlendFunction = BlendFunction.Add;

                    blendState[0].ColorWriteChannels = ColorWriteChannels.All;

                    //BlendState 1
                    blendState.CopyState(0, 1);
                    break;
                case 1: //Additive
                    //Blend 0
                    blendState.ColorSourceBlend = Blend.SourceAlpha;
                    blendState.ColorDestinationBlend = Blend.One;
                    blendState.ColorBlendFunction = BlendFunction.Add;

                    blendState.AlphaSourceBlend = Blend.SourceAlpha;
                    blendState.AlphaDestinationBlend = Blend.One;
                    blendState.AlphaBlendFunction = BlendFunction.Add;

                    blendState.ColorWriteChannels = ColorWriteChannels.All;

                    //Blend 1
                    blendState.CopyState(0, 1);

                    blendState[0].AlphaSourceBlend = Blend.Zero;
                    blendState[0].AlphaDestinationBlend = Blend.One;
                    blendState[1].AlphaSourceBlend = Blend.Zero;
                    blendState[1].AlphaDestinationBlend = Blend.One;

                    if (Material.DecompiledParameters.LowRez == 1 || Material.DecompiledParameters.LowRezSmoke == 1)
                    {
                        blendState[1].ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;
                    }
                    break;
                case 2: //Subtractive
                    //Blend 0
                    blendState.AlphaBlendFunction = BlendFunction.ReverseSubtract;
                    blendState.AlphaSourceBlend = Blend.SourceAlpha;
                    blendState.AlphaDestinationBlend = Blend.One;

                    blendState.ColorBlendFunction = BlendFunction.ReverseSubtract;
                    blendState.ColorSourceBlend = Blend.SourceAlpha;
                    blendState.ColorDestinationBlend = Blend.One;

                    blendState.ColorWriteChannels = ColorWriteChannels.All;

                    //Blend 1
                    blendState.CopyState(0, 1);

                    if (Material.DecompiledParameters.LowRez == 1 || Material.DecompiledParameters.LowRezSmoke == 1)
                    {
                        blendState[1].ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;

                        //Need to reconfirm this case.
                        //When both AlphaBlendType 1 AND LowRez are set, AlphaSourceBlend is set to zero on both states
                        blendState[0].AlphaSourceBlend = Blend.Zero;
                        blendState[1].AlphaSourceBlend = Blend.Zero;
                    }

                    IsSubtractiveBlending = true;
                    break;
                default:
                    blendState.ColorSourceBlend = Blend.One;
                    blendState.ColorDestinationBlend = Blend.Zero;
                    blendState.ColorBlendFunction = BlendFunction.Add;

                    blendState.AlphaSourceBlend = Blend.One;
                    blendState.AlphaDestinationBlend = Blend.Zero;
                    blendState.AlphaBlendFunction = BlendFunction.Add;

                    blendState.ColorWriteChannels = ColorWriteChannels.All;
                    blendState.CopyState(0, 1);
                    break;
            }

            return blendState;
        }

        public override DepthStencilState GetDepthState()
        {
            DepthStencilState depth = new DepthStencilState();

            if (ShaderType == ShaderType.CharaNormals || ShaderType == ShaderType.CharaShadow)
            {
                depth.DepthBufferEnable = true;
                depth.DepthBufferWriteEnable = true;
                depth.DepthBufferFunction = CompareFunction.LessEqual;
                depth.StencilEnable = false;
                depth.StencilWriteMask = 255;
                depth.StencilMask = 255;
                depth.ReferenceStencil = 255;

                return depth;
            }
            else if (ShaderType == ShaderType.Chara)
            {
                //Default character depth buffer
                depth.DepthBufferEnable = true;
                depth.DepthBufferWriteEnable = true;
                depth.DepthBufferFunction = CompareFunction.LessEqual;
                depth.StencilEnable = true;
                depth.StencilWriteMask = 80;
                depth.StencilMask = 80;
                depth.ReferenceStencil = 80;
                depth.StencilFail = StencilOperation.Keep;
                depth.StencilDepthBufferFail = StencilOperation.Keep;
                depth.CounterClockwiseStencilFail = StencilOperation.Keep;
                depth.CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep;
                depth.CounterClockwiseStencilPass = StencilOperation.Replace;
                depth.StencilPass = StencilOperation.Replace;
                depth.StencilFunction = CompareFunction.Always;
                depth.CounterClockwiseStencilFunction = CompareFunction.Always;

                return depth;
            }
            else if (ShaderType == ShaderType.Stage)
            {
                //Default stage depth buffer
                depth.DepthBufferEnable = true;
                depth.DepthBufferWriteEnable = true;
                depth.DepthBufferFunction = CompareFunction.LessEqual;
                depth.StencilEnable = true;
                depth.StencilWriteMask = 80;
                depth.StencilMask = 80;
                depth.ReferenceStencil = 0;
                depth.StencilFunction = CompareFunction.Always;
                depth.CounterClockwiseStencilFunction = CompareFunction.Always;

                depth.StencilFail = StencilOperation.Keep;
                depth.CounterClockwiseStencilFail = StencilOperation.Keep;

                depth.CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep;
                depth.StencilDepthBufferFail = StencilOperation.Keep;

                depth.CounterClockwiseStencilPass = StencilOperation.Zero;
                depth.StencilPass = StencilOperation.Zero;

                //return depth;
            }

            //Default path: Valid for effects
            if (MatParam.AlphaSortMask == 1 || MatParam.ZTestMask == 1)
                depth.DepthBufferEnable = true;

            if (MatParam.AlphaBlend == 1)
            {
                depth.DepthBufferEnable = true;
                depth.DepthBufferWriteEnable = false;
                depth.DepthBufferFunction = CompareFunction.LessEqual;
                depth.StencilEnable = false;
                depth.StencilMask = 255;
                depth.StencilWriteMask = 255;

                depth.StencilFail = StencilOperation.Keep;
                depth.StencilPass = StencilOperation.Keep;
                depth.StencilDepthBufferFail = StencilOperation.Keep;
                depth.StencilFunction = CompareFunction.Always;
                depth.CounterClockwiseStencilFail = StencilOperation.Keep;
                depth.CounterClockwiseStencilPass = StencilOperation.Keep;
                depth.CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep;
                depth.CounterClockwiseStencilFunction = CompareFunction.Always;
            }

            //This is not right.
            //Materials marked as ZWriteMask seem to be rendered last
            if (MatParam.ZWriteMask == 1)
            {
                depth.DepthBufferWriteEnable = false;
            }

            return depth;
        }

        public override RasterizerState GetReflectionRasterizerState()
        {
            RasterizerState state = GetRasterizerState();
            state.CullMode = MatParam.BackFace > 0 || MatParam.TwoSidedRender > 0 ? CullMode.None : CullMode.CullCounterClockwiseFace;

            return state;
        }

        public override RasterizerState GetRasterizerState()
        {
            RasterizerState state = new RasterizerState();

            state.FillMode = Viewport.Instance.WireframeMode || MatParam.ForceWireframeMode ? FillMode.WireFrame : FillMode.Solid;
            state.CullMode = MatParam.BackFace > 0 || MatParam.TwoSidedRender > 0 ? CullMode.None : CullMode.CullClockwiseFace;

            switch (ShaderType)
            {
                case ShaderType.CharaShadow:
                    state.CullMode = CullMode.CullCounterClockwiseFace;
                    break;
            }

            return state;
        }

        #endregion

        #region Apply Parameters
        //Per draw call

        /// <summary>
        /// Update parameters just before render.
        /// </summary>
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

            //Apply states
            ApplyStates();

            //Apply billboarding rotation if material requires it
            if (MatParam.Billboard == 1)
            {
                Matrix4x4 billboardWorld = Matrix4x4.CreateFromAxisAngle(MathHelpers.Up, MathHelper.Pi) * MathHelpers.Invert(Viewport.Instance.Camera.ViewMatrix);
                billboardWorld.Translation = World.Translation;
                World = billboardWorld;
            }

            //Calculate matrices
            if (UseWV)
                WV = World * Viewport.Instance.Camera.ViewMatrix;

            if (UseWVP)
                WVP = World * Viewport.Instance.Camera.ViewProjectionMatrix;

            ApplyParameters();


            Parameters["g_bOutputGlareMRT_PS"]?.SetValue(glareOutputAllowed && MatParam.Glare == 1);
            glareOutputAllowed = true;

            if (ShaderType == ShaderType.Chara && ActorSlot != -1)
            {
                if (shaderProgram.UsePixelShaderBuffer[PS_COMMON_CB])
                {
                    g_MaterialCol3_PS?.SetValue(new Vector4(SceneManager.BattleDamageScratches, SceneManager.BattleDamageBlood, Material.DecompiledParameters.MatCol3.B, Material.DecompiledParameters.MatCol3.A));
                }

                Actor actor = SceneManager.Actors[ActorSlot];

                if (actor != null)
                {
                    //Set animated light parameters (light.ema)
                    VirtualLightSource virtualPointLight = Viewport.Instance.LightSystem.GetVirtualPointLight();

                    if(virtualPointLight is not null)
                    {
                        g_vColor0_PS?.SetValue(virtualPointLight.PointLightColor);
                        g_vParam3_PS?.SetValue(virtualPointLight.PointLightStrength);
                        g_vParam4_PS?.SetValue(virtualPointLight.PointLightAttenuation);
                        g_vParam5_PS?.SetValue(virtualPointLight.PointLightPosition - World.Translation);
                    }

                    ActorShaderPath shaderPath = actor.ShaderParameters.ShaderPath;
                    if (shaderPath != ActorShaderPath.Vanish)
                    {
                        g_mWV_VS_padding?.SetValue(Vector4.Zero);
                        g_vParam8_PS?.SetValue(Vector4.Zero);
                        g_vParam10_PS?.SetValue(Vector4.Zero);
                    }

                    switch (shaderPath)
                    {
                        case ActorShaderPath.Default:
                            g_vUserFlag1_VS?.SetValue(Vector4.Zero);
                            break;
                        case ActorShaderPath.Vanish:
                            g_mWV_VS_padding?.SetValue(new Vector4(VanishShaderFlag, 0, 0, 0));
                            g_vUserFlag1_VS?.SetValue(new Vector4(VanishShaderFlag, actor.ShaderParameters.g_vUserFlag1_VS.Y, 0, 0));
                            g_vColor4_PS?.SetValue(actor.ShaderParameters.g_vColor4_PS);
                            g_vParam8_PS?.SetValue(new Vector4(0f, 0f, 0.001f, 1f));
                            g_vParam9_PS?.SetValue(actor.ShaderParameters.g_vParam9_PS);
                            g_vParam10_PS?.SetValue(new Vector4(0f, 0f, 1f, 0f));
                            break;
                        case ActorShaderPath.HC:
                            g_vUserFlag1_VS?.SetValue(HC_UserFlag1);

                            if (g_vParam11_PS != null)
                            {
                                g_vParam11_PS.SetValue(HC_Param11);
                                g_vParam12_PS.SetValue(HC_Param12);
                                g_vParam13_PS.SetValue(HC_Param13);
                            }
                            break;
                    }
                }
            }

            if (ShaderType == ShaderType.CharaNormals)
            {
                Actor actor = ActorSlot >= 0 && ActorSlot < SceneManager.NumActors ? SceneManager.Actors[ActorSlot] : null;

                g_vParam1_PS?.SetValue(new Vector4(0f, 1f, 1f, 0.6f));

                if (actor?.ShaderParameters.BodyOutlineActive == true)
                {
                    g_vParam2_PS?.SetValue(actor.ShaderParameters.BodyOutlineParam2);
                    g_vParam3_PS?.SetValue(actor.ShaderParameters.BodyOutlineParam3);
                }
                else
                {
                    g_vParam2_PS?.SetValue(new Vector4(0f, 0f, 1f, 0f));
                    g_vParam3_PS?.SetValue(new Vector4(10f, 10000f, 0f, 0f));
                }

                return;
            }

            //Testing
            //SetParameters2();
            //Parameters["g_vTone_PS"]?.SetValue(-Vector4.One);
            //Parameters["g_bShadowPCF4_PS"]?.SetValue(true);
            //Parameters["g_bShadowPCF8_PS"]?.SetValue(true);
            //Parameters["g_bShadowPCF16_PS"]?.SetValue(true);
            //Parameters["g_bShadowPCF24_PS"]?.SetValue(true);
            //Parameters["g_vEyePos_VS"]?.SetValue(Vector4.Zero);
            //Parameters["g_vShadowParam_PS"]?.SetValue(new Vector4(16, 32, 0, 0));
            //Parameters["g_vShadowColor_PS"]?.SetValue(new Vector4(0.5f,0.5f,0.5f, 1));
            //Parameters["g_vShadowMap_PS"]?.SetValue(new Vector4(SettingsManager.settings.XenoKit_ShadowMapRes, 0.00012f, 0.001f, 0.85f));

            //Parameters["g_vLightDif0_PS"]?.SetValue(new Vector4(0.48649f, 0.48649f, 0.49324f, 1.00f)); //Needs to be set

            //Parameters["g_vLightSpc0_PS"]?.SetValue(new Vector4(0.3f, 0.3f, 0.3f, 1)); //NOT set. SogLightDir  is NOT this?
            //Parameters["g_vSpecular_PS"]?.SetValue(new Vector4(2,4,0,0)); //No effect?
            //Parameters["g_vAmbUni_PS"]?.SetValue(new Vector4(0.1175f, 0.17391f, 0.30072f, 1.00f)); //Needs to be set

            //Parameters["g_vSpecular_PS"]?.SetValue(new Vector4(2f, 20f, 0, 0));
            //Parameters["g_vLightSpc0_PS"]?.SetValue(new Vector4(1,1,1,1)); //  112 float4

            //Parameters["g_vTone_PS"]?.SetValue(new Vector4(0.05571f, 0.04429f, 0.06524f, 3.50f));
            //Parameters["g_bFog_PS"]?.SetValue(true);
            //Parameters["g_vFogAddColor_PS"]?.SetValue(new Vector4(0,0,0,1));
            //Parameters["g_vAmbUni_VS"]?.SetValue(new Vector4(0,0,0,1));

            //g_vLightDif0_PS 0.38161, 0.32367, 0.16482, 1.00 96 float4
            //g_vLightSpc0_PS 0.971, 0.745, 0.718, 1.00     112 float4
            //g_vSpecular_PS 0.50, 15.00, 0.00, 0.00 288 float4

            //Parameters["g_vSpecular_PS"]?.SetValue(new Vector4(0.50f, 15.00f, 0.00f, 0.00f));
            //Parameters["g_vLightDif0_PS"]?.SetValue(new Vector4(0.38161f, 0.32367f, 0.16482f, 1.00f));
            //Parameters["g_vLightSpc0_PS"]?.SetValue(new Vector4(0.971f, 0.745f, 0.718f, 1.00f));

            //Required for T1_VFX_MTN_DIS_ALPHA, unsure where the value comes from (it changes)
            //Parameters["g_vColor13_PS"]?.SetValue(new Vector4(1.00f, 1.00f, 1.00f, 1f));

            //Parameters["g_vSpecular_VS"]?.SetValue(new Vector4(2.0159f, 2.06009f, 2.01842f, 2.01591f));
            //g_vEyePos_VS?.SetValue(new Vector4(Vector3.Transform(Viewport.Instance.Camera.CameraState.Position, Matrix.Invert(World)), 1f));
            //Update global parameters
            if (shaderProgram.UseVertexShaderBuffer[VS_STAGE_CB])
            {
                g_vScreen_VS?.SetVector4(Viewport.Instance.RenderSystem.RenderResolution);
                g_SystemTime_VS?.SetValue(Viewport.SystemTime);
            }

            //Remove references to animated parameters from this pass
            //MatCol0 = null;
            //MatCol1 = null;
            //MatCol2 = null;
            //MatCol3 = null;
            //TexScrl0 = null;
            //TexScrl1 = null;

            //Reset draw parameters
            ActorSlot = -1;

            //Set global samplers/textures
            foreach (Xv2Shader shader in _shaders)
                shader.SetGlobalSamplers(Viewport.Instance.ShaderManager);
        }
        
        private void ApplyParameters()
        {
            bool shadowMapRequired = false;
            bool applyLight = false;
            
            foreach (ShaderParameter parameter in SdsParameters)
            {
                switch (parameter)
                {
                    case ShaderParameter.W:
                        g_mW_VS?.SetValue(World);
                        break;
                    case ShaderParameter.WV:
                        g_mWV_VS?.SetValue(WV);
                        break;
                    case ShaderParameter.WVP:
                        g_mWVP_VS?.SetValue(WVP);
                        break;
                    case ShaderParameter.VP:
                        g_mVP_VS?.SetValue(Viewport.Instance.Camera.ViewProjectionMatrix);
                        break;
                    case ShaderParameter.WLP_SM: //World Light Projection (Shadow Map)
                    case ShaderParameter.WLPB_SM: //World Light Projection Bias (Shadow Map)
                    case ShaderParameter.WLP_PM: //Same as _SM variant
                    case ShaderParameter.WLPB_PM: //Different bias matrix to _SM version
                        shadowMapRequired = true;
                        break;
                    case ShaderParameter.WIT: //World Instance Transform
                        g_mWIT_VS?.SetValue(World);
                        break;
                    case ShaderParameter.WVP_Prev:
                        g_mWVP_Prev_VS?.SetValue(PrevWVP);
                        break;

                    case ShaderParameter.MatCol0_PS:
                        g_MaterialCol0_PS?.SetVector4(MatCol0 != null ? MatCol0 : Material.DecompiledParameters.MatCol0.Values);
                        break;
                    case ShaderParameter.MatCol1_PS:
                        g_MaterialCol1_PS?.SetVector4(MatCol1 != null ? MatCol1 : Material.DecompiledParameters.MatCol1.Values);
                        break;
                    case ShaderParameter.MatCol2_PS:
                        g_MaterialCol2_PS?.SetVector4(MatCol2 != null ? MatCol2 : Material.DecompiledParameters.MatCol2.Values);
                        break;
                    case ShaderParameter.MatCol3_PS:
                        g_MaterialCol3_PS?.SetVector4(MatCol3 != null ? MatCol3 : Material.DecompiledParameters.MatCol3.Values);
                        break;
                    case ShaderParameter.MatCol0_VS:
                        g_MaterialCol0_VS?.SetVector4(MatCol0 != null ? MatCol0 : Material.DecompiledParameters.MatCol0.Values);
                        break;
                    case ShaderParameter.MatCol1_VS:
                        g_MaterialCol1_VS?.SetVector4(MatCol1 != null ? MatCol1 : Material.DecompiledParameters.MatCol1.Values);
                        break;
                    case ShaderParameter.MatCol2_VS:
                        g_MaterialCol2_VS?.SetVector4(MatCol2 != null ? MatCol2 : Material.DecompiledParameters.MatCol2.Values);
                        break;
                    case ShaderParameter.MatCol3_VS:
                        g_MaterialCol3_VS?.SetVector4(MatCol3 != null ? MatCol3 : Material.DecompiledParameters.MatCol3.Values);
                        break;
                    case ShaderParameter.TexScrl0_VS:
                        g_TexScroll0_VS?.SetVector4(TexScrl0 != null ? TexScrl0 : Material.DecompiledParameters.TexScrl0.Values);
                        break;
                    case ShaderParameter.TexScrl1_VS:
                        g_TexScroll1_VS?.SetVector4(TexScrl1 != null ? TexScrl1 : Material.DecompiledParameters.TexScrl1.Values);
                        break;
                    case ShaderParameter.TexScrl0_PS:
                        g_TexScroll0_PS?.SetVector4(TexScrl0 != null ? TexScrl0 : Material.DecompiledParameters.TexScrl0.Values);
                        break;
                    case ShaderParameter.TexScrl1_PS:
                        g_TexScroll1_PS?.SetVector4(TexScrl1 != null ? TexScrl1 : Material.DecompiledParameters.TexScrl1.Values);
                        break;
                    case ShaderParameter.UserFlag0_VS:
                        g_vUserFlag0_VS?.SetValue(Viewport.Instance.Camera.CameraState.Position);
                        break;
                    case ShaderParameter.EyePos_VS: //Camera position in object space
                        g_vEyePos_VS?.SetValue(new Vector4(Vector3.Transform(Viewport.Instance.Camera.CameraState.Position, Matrix.Invert(World)), 1f));
                        break;
                    case ShaderParameter.EyePos_PS:
                        g_vEyePos_PS?.SetValue(new Vector4(Vector3.Transform(Viewport.Instance.Camera.CameraState.Position, Matrix.Invert(World)), 1f));
                        break;
                    case ShaderParameter.FadeColor:
                        if (ECF_Multi != null)
                        {
                            g_vFadeMulti_PS?.SetVector4(ECF_Multi);
                            g_vFadeRim_PS?.SetVector4(ECF_Rim);
                            g_vFadeAdd_PS?.SetVector4(ECF_Add);
                        }
                        else
                        {
                            //Set default values
                            g_vFadeMulti_PS?.SetValue(Vector4.One);
                            g_vFadeRim_PS?.SetValue(Vector4.Zero);
                            g_vFadeAdd_PS?.SetValue(Vector4.Zero);
                        }
                        break;
                    case ShaderParameter.LightVec0_PS:
                    case ShaderParameter.LightVec0_VS:
                    case ShaderParameter.MatDif0_PS:
                    case ShaderParameter.MatSpc0_PS:
                    case ShaderParameter.MatDif0_VS:
                    case ShaderParameter.MatSpc0_VS:
                        applyLight = true;
                        break;

                }
            }

            if (applyLight || ActorSlot != -1)
            {
                ApplyLight();
            }

            if (shadowMapRequired)
            {
                BackgroundLight bgLight = Viewport.Instance.LightSystem.BgLight;
                Matrix4x4 wlp = World * bgLight.LightViewProjectionMatrix;

                g_mWLP_SM_VS?.SetValue(wlp);
                g_mWLPB_SM_VS?.SetValue(wlp * bgLight.BiasMatrix_SM);
                g_mWLP_PM_VS?.SetValue(wlp);
                g_mWLPB_PM_VS?.SetValue(wlp * bgLight.BiasMatrix_PM);

                Parameters["g_vShadowParam_PS"]?.SetValue(new Vector4(bgLight.ShadowFadeStart, bgLight.ShadowFadeEnd, 0, 0));
            }
        }

        private void ApplyLight()
        {
            DirLight mainLight = Viewport.Instance.LightSystem.GetActiveLight();

            //Calculate direction relative to world
            Matrix4x4 inverseRotation = Matrix4x4.Transpose(World);
            Vector3 localDirection = Vector3.TransformNormal(mainLight.CameraSpaceDirection, inverseRotation);
            Vector3 lightVec = Vector3.Normalize(localDirection);

            var matDif = Material.DecompiledParameters.MatDif;
            var matSpc = Material.DecompiledParameters.MatSpc;

            SimdVector4 diffuse = mainLight.GetDiffuseVec4() * new SimdVector4(matDif.R, matDif.G, matDif.B, 1f);
            SimdVector4 specular = mainLight.GetSpecularVec4() * new SimdVector4(matSpc.R, matSpc.G, matSpc.B, 1f);

            if (UseLight_VS)
            {
                g_vLightVec0_VS?.SetValue(lightVec);
                g_vLightSpc0_VS?.SetValue(specular);
                g_vLightDif0_VS?.SetValue(diffuse);
            }
            if (UseLight_PS)
            {
                g_vLightVec0_PS?.SetValue(lightVec);
                g_vLightSpc0_PS?.SetValue(specular);
                g_vLightDif0_PS?.SetValue(diffuse);
            }
        }

        public void SetGlareOutputAllowed(bool allowed)
        {
            glareOutputAllowed = allowed;
        }

        public void ApplyColorFade(Actor actor)
        {
            if (actor == null) return;
            VfxColorFadeEntry colorFade = actor.VfxManager.GetActiveColorFade(Material.Name, actor);

            if (colorFade != null)
            {
                ECF_Multi = colorFade.Multi;
                ECF_Rim = colorFade.RimColor;
                ECF_Add = colorFade.AddColor;
            }
            else
            {
                ECF_Multi = null;
                ECF_Rim = null;
                ECF_Add = null;
            }
        }

        public void ApplyMaterialAnimationValues(VfxEmaMaterialNode materialAnimationNode)
        {
            TexScrl0 = materialAnimationNode.TexScrl[0];
            TexScrl1 = materialAnimationNode.TexScrl[1];
            MatCol0 = materialAnimationNode.MatCol[0];
            MatCol1 = materialAnimationNode.MatCol[1];
            MatCol2 = materialAnimationNode.MatCol[2];
            MatCol3 = materialAnimationNode.MatCol[3];
        }

        public void ClearMaterialAnimationValues()
        {
            TexScrl0 = TexScrl1 = MatCol0 = MatCol1 = MatCol2 = MatCol3 = null;
        }

        public void ApplySkinningMatrices(Matrix4x4[] matrices)
        {
            if (!SkinningEnabled && shaderProgram.UseVertexShaderBuffer[CB_VS_BOOL])
            {
                SkinningEnabled = true;
                g_bSkinning_VS.SetValue(true);
            }

            if (shaderProgram.UseVertexShaderBuffer[VS_MTXPLT_CB])
            {
                g_mMatrixPalette_VS.SetValue(matrices);
            }
        }

        public void SetEyeMovement(float[] uvScroll)
        {
            if (shaderProgram.UsePixelShaderBuffer[PS_COMMON_CB])
            {
                TexScrl0 = uvScroll;
                //g_TexScroll0_PS.SetVector4(uvScroll);
            }

            if (shaderProgram.UseVertexShaderBuffer[VS_COMMON_MATERIAL_CB])
            {
                TexScrl0 = uvScroll;
                //g_TexScroll0_VS.SetVector4(uvScroll);
            }
        }

        public void ApplyTextureScrollOverrides(float[] texScrl0, float[] texScrl1)
        {
            if (texScrl0 != null && texScrl0.Length < 4)
                throw new ArgumentException("Texture scroll override 0 must have 4 values.", nameof(texScrl0));

            if (texScrl1 != null && texScrl1.Length < 4)
                throw new ArgumentException("Texture scroll override 1 must have 4 values.", nameof(texScrl1));

            TexScrl0 = texScrl0;
            TexScrl1 = texScrl1;
        }

        public void ApplyTextureTile(float[] texTile01, float[] texTile23)
        {
            if (shaderProgram.UseVertexShaderBuffer[VS_STAGE_CB])
            {
                g_vTexTile01_VS?.SetVector4(texTile01);
                g_vTexTile23_VS?.SetVector4(texTile23);
            }
        }

        protected void ApplyStates()
        {
            GraphicsDevice.BlendState = _blendState;
            GraphicsDevice.DepthStencilState = _depthStencilState;

            if (Viewport.Instance.RenderSystem.IsReflectionPass)
            {
                //Reflection state is only created on demand, since it is only used for reflections
                if (_reflectionRasterizerState == null)
                    _reflectionRasterizerState = GetReflectionRasterizerState();

                GraphicsDevice.RasterizerState = _reflectionRasterizerState;
            }
            else
            {
                GraphicsDevice.RasterizerState = _rasterizerState;
            }
        }

        #endregion

        #region Set Parameters
        //Per material parameters that are not set every draw

        protected override void SetParameters()
        {
            //Set initial parameters

            //vs_stage_cb
            if (shaderProgram.UseVertexShaderBuffer[VS_STAGE_CB])
            {
                Parameters["g_vAmbOcl_VS"].SetValue(new Vector4(0, 0, 0, 1));

                //Values from BFten
                Parameters["g_vHeightFog_VS"].SetValue(new Vector4(0.13714f, 0.02857f, 1.00033f, -0.00333f));
                Parameters["g_vFog_VS"].SetValue(new Vector4(30.00f, 300.00f, 1.11111f, -0.0037f));

                Parameters["g_vTexTile01_VS"].SetValue(new Vector4(1, 1, 1, 1));
                Parameters["g_vTexTile23_VS"].SetValue(new Vector4(1, 1, 1, 1));
                Parameters["g_vMotionBlur_VS"].SetValue(new Vector4(0.10f, 1.77778f, Viewport.Instance.CurrentStage.CurrentSpm.ExtendedData != null ? Viewport.Instance.CurrentStage.CurrentSpm.ExtendedData.F_68[0] : 0.435f, 1.00f));
            }

            //vs_common_light_cb
            if (shaderProgram.UseVertexShaderBuffer[VS_COMMON_LIGHT_CB])
            {
                Parameters["g_vHemiA_VS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vHemiB_VS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vHemiC_VS"].SetValue(new Vector4(0.9f, 0.9f, 0.9f, 1));
                //Parameters["g_vAmbUni_VS"].SetVector4(MatParam.MatAmb.Values, MatParam.MatAmbScale);
                
                Parameters["g_vRim_VS"].SetValue(new Vector4(MatParam.RimCoeff, MatParam.RimPower, 0, 0));
                Parameters["g_vSpecular_VS"].SetValue(new Vector4(MatParam.SpcCoeff, MatParam.SpcPower, 0, 0));
                Parameters["g_vEyePos_VS"].SetVector4(MatParam.gCamPos.Values);
            }

            //vs_common_material_cb
            if (shaderProgram.UseVertexShaderBuffer[VS_COMMON_MATERIAL_CB])
            {
                Parameters["g_TexScroll0_VS"].SetVector4(MatParam.TexScrl0.Values);
                Parameters["g_TexScroll1_VS"].SetVector4(MatParam.TexScrl1.Values);
                Parameters["g_MaterialCol0_VS"].SetVector4(MatParam.MatCol0.Values);
                Parameters["g_MaterialCol1_VS"].SetVector4(MatParam.MatCol1.Values);
                Parameters["g_MaterialCol2_VS"].SetVector4(MatParam.MatCol2.Values);
                Parameters["g_MaterialCol3_VS"].SetVector4(MatParam.MatCol3.Values);
                Parameters["g_MaterialScale0_VS"].SetVector4(MatParam.MatScale0.Values);
                Parameters["g_MaterialScale1_VS"].SetVector4(MatParam.MatScale1.Values);
                Parameters["g_MaterialOffset0_VS"].SetVector4(MatParam.MatOffset0.Values);
                Parameters["g_MaterialOffset1_VS"].SetVector4(MatParam.MatOffset1.Values);
                Parameters["g_AlphaFade_VS"].SetValue(new Vector4(MatParam.FadeInit, MatParam.FadeSpeed, 0, 0));
                float incidenceCurve = shaderProgram.Name.StartsWith("T1_VFX_TRC_I", StringComparison.Ordinal) ? MatParam.IncidencePower : 0f;
                Parameters["g_Incidence_VS"].SetValue(new Vector4(MatParam.IncidencePower, MatParam.IncidenceAlphaBias, incidenceCurve, 0));
                Parameters["g_Gradient_VS"].SetValue(new Vector4(MatParam.GradientInit, MatParam.GradientSpeed, 0, 0));
                Parameters["g_GlareCoeff_VS"].SetVector4(MatParam.GlareCol.Values);
                Parameters["g_Reflection_VS"].SetValue(new Vector4(MatParam.ReflectCoeff, MatParam.ReflectFresnelBias, MatParam.ReflectFresnelCoeff, 0));
                Parameters["g_EdgeColor_VS"].SetValue(Vector4.One);
            }

            //cb_vs_bool
            if (shaderProgram.UseVertexShaderBuffer[CB_VS_BOOL])
            {
                Parameters["g_bSkinning_VS"].SetValue(SkinningEnabled);
                Parameters["g_bAmbOcl_VS"].SetValue(SdsParameters.Contains(ShaderParameter.AmbOclColor));

                Parameters["g_bVersatile0_VS"].SetValue(MatParam.VsFlag0);
                Parameters["g_bVersatile1_VS"].SetValue(MatParam.VsFlag1);
                Parameters["g_bVersatile2_VS"].SetValue(MatParam.VsFlag2);
                Parameters["g_bVersatile3_VS"].SetValue(MatParam.VsFlag3);

                Parameters["g_bUserFlag1_VS"].SetValue(true);
                Parameters["g_bUserFlag2_VS"].SetValue(true);
                Parameters["g_bUserFlag3_VS"].SetValue(true);
            }

            //ps_stage_cb
            if (shaderProgram.UsePixelShaderBuffer[PS_STAGE_CB])
            {
                Parameters["g_vShadowMap_PS"].SetValue(new Vector4(SettingsManager.settings.XenoKit_ShadowMapRes, 00049f, 0.001f, 0.85f));
                Parameters["g_vShadowColor_PS"].SetValue(new Vector4(0.5f, 0.5f, 0.5f, 0));
                Parameters["g_vEdge_PS"].SetValue(MatParam.NoEdge > 0 ? Vector4.Zero : Vector4.One);
                Parameters["g_vGlare_PS"].SetValue(Vector4.Zero);
                Parameters["g_vTone_PS"].SetValue(ShaderType == ShaderType.Stage ? DefaultBackgroundGlareTone : DefaultEffectGlareTone);
                Parameters["g_vScale_PS"].SetValue(new Vector4(1.00f, 0.00f, 0.50f, 0.50f));

                //No fog
                Parameters["g_vFogMultiColor_PS"].SetValue(Vector4.One);
                Parameters["g_vFogAddColor_PS"].SetValue(Vector4.Zero);

                Parameters["g_vAltFadeMulti_PS"].SetValue(new Vector4(1, 1, 1, 1f));
            }

            //ps_alphatest_cb
            if (shaderProgram.UsePixelShaderBuffer[PS_ALPHATEST_CB])
            {
                Parameters["g_vAlphaTest_PS"]?.SetValue(MatParam.AlphaTest > 0 ? new Vector4(MatParam.AlphaTest / 255f, 0, 0, 0) : Vector4.Zero);
            }

            //ps_common_cb
            if (shaderProgram.UsePixelShaderBuffer[PS_COMMON_CB])
            {
                Parameters["g_vHemiA_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vHemiB_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vHemiC_PS"].SetValue(new Vector4(0.9f, 0.9f, 0.9f, 1));
                Parameters["g_vLodViewport_PS"].SetValue(Vector4.One);

                //Parameters["g_vAmbUni_PS"].SetVector4(MatParam.MatAmb.Values, MatParam.MatAmbScale);
                Parameters["g_vLightVec1_PS"].SetValue(new Vector3(0f, 1f, 0f));
                Parameters["g_vLightVec2_PS"].SetValue(new Vector3(0f, 1f, 0f));
                Parameters["g_vEyePos_PS"].SetVector4(MatParam.gCamPos.Values);
                Parameters["g_vSpecular_PS"].SetValue(new Vector4(MatParam.SpcCoeff, MatParam.SpcPower, 0, 0));
                Parameters["g_vRim_PS"].SetValue(new Vector4(MatParam.RimCoeff, MatParam.RimPower, 0, 0));
                Parameters["g_TexScroll0_PS"].SetVector4(MatParam.TexScrl0.Values);
                Parameters["g_TexScroll1_PS"].SetVector4(MatParam.TexScrl1.Values);
                Parameters["g_MaterialCol0_PS"].SetVector4(MatParam.MatCol0.Values);
                Parameters["g_MaterialCol1_PS"].SetVector4(MatParam.MatCol1.Values);
                Parameters["g_MaterialCol2_PS"].SetVector4(MatParam.MatCol2.Values);
                Parameters["g_MaterialCol3_PS"].SetVector4(MatParam.MatCol3.Values); //(Characters) R = scratches, G = blood
                Parameters["g_MaterialOffset0_PS"].SetVector4(MatParam.MatOffset0.Values);
                Parameters["g_MaterialOffset1_PS"].SetVector4(MatParam.MatOffset1.Values);
                Parameters["g_MaterialScale0_PS"].SetVector4(MatParam.MatScale0.Values);
                Parameters["g_MaterialScale1_PS"].SetVector4(MatParam.MatScale1.Values);
                Parameters["g_AlphaFade_PS"].SetValue(new Vector4(MatParam.FadeInit, MatParam.FadeSpeed, 0, 0));
                float incidenceCurve = shaderProgram.Name.StartsWith("T1_VFX_TRC_I", StringComparison.Ordinal) ? MatParam.IncidencePower : 0f;
                Parameters["g_Incidence_PS"].SetValue(new Vector4(MatParam.IncidencePower, MatParam.IncidenceAlphaBias, incidenceCurve, 0));
                Parameters["g_Gradient_PS"].SetValue(new Vector4(MatParam.GradientInit, MatParam.GradientSpeed, 0, 0));
                Parameters["g_GlareCoeff_PS"].SetVector4(MatParam.GlareCol.Values);
                Parameters["g_Reflection_PS"].SetValue(new Vector4(MatParam.ReflectCoeff, MatParam.ReflectFresnelBias, MatParam.ReflectFresnelCoeff, 0));
                Parameters["g_EdgeColor_PS"].SetValue(Vector4.One);
            }

            //ps_versatile_cb
            if (shaderProgram.UsePixelShaderBuffer[PS_VERSATILE_CB])
            {
                Parameters["g_vColor0_PS"].SetValue(Vector4.One);
                Parameters["g_vColor1_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vColor2_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vColor8_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vColor9_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vColor10_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vColor11_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vColor13_PS"].SetValue(Vector4.One); //Required for T1_VFX_MTN_DIS_ALPHA
                Parameters["g_vColor15_PS"].SetValue(new Vector4(0, 0, 0, 1));

                Parameters["g_vParam1_PS"].SetValue(new Vector4(1, 1f, 0, 1f));
                Parameters["g_vParam2_PS"].SetValue(new Vector4(0, 0, 0, 1f));
                Parameters["g_vParam3_PS"].SetValue(new Vector4(1f));
                Parameters["g_vParam4_PS"].SetValue(new Vector4(0.25f, 0.75f, 0f, 0f)); //x: for dyt color, y : for color from samlpe14 (second dyt) , so test normal RAD for unbreak shaders. and est with dyt.emb.data001.dds on samlper14. (XenoViewer comment. Is wrong about Sampler14 - that is not a dyt, but common lighting), EDIT once again: this cant be true, since this param is for LIGHT animations?

                //Parameters["g_vParam4_PS"].SetValue(new Vector4(0.25f, 0.0f, 0f, 0f)); 

                Parameters["g_vParam7_PS"].SetValue(new Vector4(0.0f, 23.2558f, 0.04587f, 0.0f)); //Toon Detail Parameter 
                Parameters["g_vParam11_PS"].SetValue(new Vector4(1f, 1f, 0.22f, 0.25f));
                Parameters["g_vParam12_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vParam13_PS"].SetValue(new Vector4(1f));
                Parameters["g_vParam14_PS"].SetValue(new Vector4(1f));

                if (shaderProgram.PsParser.UsesAlphaDepth)
                {
                    Parameters["g_vParam0_PS"]?.SetValue(new Vector4(0.01667f, 0.98333f, 60.00f, 0.00f));
                    Parameters["g_vParam1_PS"]?.SetValue(new Vector4(0.50f, 0.50f, 0.10f, 10106.85645f));

                    //Parameters["g_vParam0_PS"].SetValue(new Vector4(Viewport.Instance.Camera.FarClip / (Viewport.Instance.Camera.FarClip - Viewport.Instance.Camera.NearClip), 0, Viewport.Instance.Camera.FarClip, 0f));
                }
            }

            if (shaderProgram.UsePixelShaderBuffer[PS_USER_CB])
            {
                Parameters["g_vShadowParam_PS"].SetValue(new Vector4(-10f, -85f, 0, 0));
            }

            //cb_ps_bool
            if (shaderProgram.UsePixelShaderBuffer[CB_PS_BOOL])
            {
                Parameters["g_bFog_PS"].SetValue(false);
                Parameters["g_bOutputGlareMRT_PS"].SetValue(MatParam.Glare == 1);
                Parameters["ps_bool_padding0"].SetValue(true);
                Parameters["g_bDepthTex_PS"]?.SetValue(true);
            }

            //Set defeault shadow params
            Parameters["g_vShadowParam_PS"]?.SetValue(new Vector4(16, 32, 0, 0));
            Parameters["g_vShadowColor_PS"]?.SetValue(new Vector4(0.5f, 0.5f, 0.5f, 1));

            SetShadowMap();
            SetSpmValues();

        }

        private void SetSpmValues()
        {
            Xv2Stage currentStage = Viewport.Instance.CurrentStage;

            if (shaderProgram.UsePixelShaderBuffer[PS_STAGE_CB] || ShaderType == ShaderType.Stage)
            {
                Vector4 tone = ShaderType == ShaderType.Stage
                    ? currentStage?.BackgroundGlareTone ?? DefaultBackgroundGlareTone
                    : currentStage?.EffectGlareTone ?? DefaultEffectGlareTone;

                Parameters["g_vGlare_PS"]?.SetValue(Vector4.Zero);
                Parameters["g_vTone_PS"]?.SetValue(tone);
                Parameters["g_vEdge_PS"]?.SetValue(MatParam.NoEdge > 0 ? Vector4.Zero : (currentStage?.EdgeColor ?? Vector4.One));

                if (currentStage != null && currentStage.FogEnabled)
                {
                    Parameters["g_vFogMultiColor_PS"]?.SetValue(currentStage.FogMultiColor);
                    Parameters["g_vFogAddColor_PS"]?.SetValue(currentStage.FogAddColor);
                    Parameters["g_vFog_VS"]?.SetValue(currentStage.Fog);
                    Parameters["g_bFog_PS"]?.SetValue(currentStage.FogEnabled);
                }
                else
                {
                    Parameters["g_bFog_PS"]?.SetValue(false);
                }
            }

            if(currentStage != null)
            {
                Vector4 ambient = currentStage.AmbientColor * new Vector4(MatParam.MatAmb.R, MatParam.MatAmb.G, MatParam.MatAmb.B, MatParam.MatAmb.A);

                Parameters["g_vAmbUni_VS"]?.SetValue(ambient);
                Parameters["g_vAmbUni_PS"]?.SetValue(ambient);
                Parameters["g_vRimColor_VS"]?.SetValue(currentStage.RimColor);
                Parameters["g_vRimColor_PS"]?.SetValue(currentStage.RimColor);
                Parameters["g_vColor14_PS"]?.SetValue(currentStage.Color14);
            }
        }

        private void SetShadowMap()
        {
            BackgroundLight bgLight = Viewport.Instance.LightSystem.BgLight;
            Parameters["g_vShadowMap_PS"]?.SetValue(new Vector4(SettingsManager.settings.XenoKit_ShadowMapRes, 1f / SettingsManager.settings.XenoKit_ShadowMapRes, bgLight.ShadowParams.DepthBias, bgLight.ShadowParams.ShadowParam48));
        }

        #endregion

        #region Auto Updating
        private void DecompiledParameters_ParametersChanged(object sender, ParameterChangeType changeType)
        {
            if(changeType == ParameterChangeType.UpdateShaderProgram)
            {
                IsShaderProgramDirty = true;
            }
            else if(changeType == ParameterChangeType.UpdateParameters)
            {
                IsDirty = true;
            }
        }

        private void Material_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Material.ShaderProgram))
            {
                IsShaderProgramDirty = true;
            }
        }

        private void Xv2Stage_CurrentSpmChanged(object sender, EventArgs e)
        {
            SetShadowMap();
            SetSpmValues();
        }

        private void SettingsManager_SettingsChanged(object sender, EventArgs e)
        {
            SetShadowMap();
            SetSpmValues();
        }

        public void SetShaderProgram(ShaderProgram shader)
        {
            shaderProgram = shader;
            IsShaderProgramDirty = true;
        }

        #endregion

        public static Xv2ShaderEffect[] LoadMaterials(EMM_File emmFile, ShaderType shaderType)
        {
            if (emmFile == null)
                return new Xv2ShaderEffect[0];

            Xv2ShaderEffect[] materials = new Xv2ShaderEffect[emmFile.Materials.Count];

            for (int i = 0; i < emmFile.Materials.Count; i++)
            {
                materials[i] = Viewport.Instance.CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(emmFile.Materials[i], shaderType);
            }

            return materials;
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (Material != null)
                {
                    Material.DecompiledParameters.ParametersChanged -= DecompiledParameters_ParametersChanged;
                    Material.PropertyChanged -= Material_PropertyChanged;
                    Xv2Stage.CurrentSpmChanged -= Xv2Stage_CurrentSpmChanged;
                    SettingsManager.SettingsSaved -= SettingsManager_SettingsChanged;
                }
            }

            base.Dispose(disposing);
        }

    }

    public enum ShaderType
    {
        Default, //Effects (PBIND, TBIND, EMO)
        Chara,
        CharaNormals, //NORMAL_FADE_WATERDEPTH_W_M
        CharaShadow, //ShadowModel_W
        Stage,
        StageShadow, //ShadowModel
        PostFilter,
    }

    public enum ActorShaderPath
    {
        Default,
        Vanish,
        HC
    }

    public struct ActorShaderExtraParameters
    {
        public ActorShaderPath ShaderPath;
        public SimdVector4 g_vColor4_PS; //Tint color (RGB used, A ignored).
        public SimdVector4 g_vParam9_PS; //X = horizontal line size, Y = vertical line size, Z = gap between horizontal lines, W = gap between vertical lines
        public SimdVector4 g_vUserFlag1_VS; //X = Activate Path (115 = Vanish, 7936 = HC), Y = controls intensity of g_vColor4_PS (0 - 10 range, where 0 just tints the character, and 10 is fully opaque with no texture details) 
        public bool BodyOutlineActive;
        public SimdVector4 BodyOutlineColor;
        public SimdVector4 BodyOutlineParam2;
        public SimdVector4 BodyOutlineParam3;
    }
}
