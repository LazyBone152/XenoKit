using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.EMM;
using XenoKit.Engine.Shader.DXBC;
using XenoKit.Engine.Vfx;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.SDS;

namespace XenoKit.Engine.Shader
{
    /// <summary>
    /// Represents a DBXV2 shader instance (ShaderProgram and Materials).
    /// </summary>
    public class Xv2ShaderEffect : Effect
    {
        //Constant Buffer IDS:
        public const int VS_MATRIX_CB = 0;
        public const int VS_STAGE_CB = 1;
        public const int VS_COMMON_LIGHT_CB = 2;
        public const int VS_COMMON_MATERIAL_CB = 3;
        public const int VS_MTXPLT_CB = 4; //can also be: vs_mtxplt_alias_sspl_cb
        public const int VS_MTXPLT_PREV_CB = 5;
        public const int VS_VERSATILE_CB = 6;
        public const int CB_VS_BOOL = 8;
        public const int PS_STAGE_CB = 0;
        public const int PS_ALPHATEST_CB = 1;
        public const int PS_COMMON_CB = 2;
        public const int PS_VERSATILE_CB = 4;
        public const int PS_USER_CB = 5;
        public const int CB_PS_BOOL = 8;

        //Default materials to use when none is found:
        private static Xv2ShaderEffect _defaultCharaMaterial = null;
        public static Xv2ShaderEffect DefaultCharacterMaterial
        {
            get
            {
                if (_defaultCharaMaterial == null)
                {
                    _defaultCharaMaterial = CreateDefaultMaterial(ShaderType.Chara, SceneManager.MainGameBase);
                }
                return _defaultCharaMaterial;
            }
        }

        protected GameBase GameBase;

        public ShaderType ShaderType { get; protected set; }
        private Xv2Shader[] _shaders;

        public Matrix World = Matrix.Identity;
        public Matrix WVP = Matrix.Identity;
        public Matrix PrevWVP = Matrix.Identity;

        //Settings
        protected ShaderProgram shaderProgram = null;
        public EmmMaterial Material { get; protected set; }
        public DecompiledMaterial MatParam => Material?.DecompiledParameters;
        public bool IsSubtractiveBlending { get; protected set; }

        private bool SkinningEnabled { get; set; }

        private ShaderParameter[] SdsParameters;

        //Parameters
        protected EffectParameter g_mWVP_VS;
        protected EffectParameter g_mVP_VS;
        protected EffectParameter g_mWVP_Prev_VS;
        protected EffectParameter g_mWV_VS;
        protected EffectParameter g_mW_VS;
        protected EffectParameter g_mWLP_SM_VS;
        protected EffectParameter g_vScreen_VS;
        protected EffectParameter g_SystemTime_VS;
        protected EffectParameter g_vEyePos_VS;
        protected EffectParameter g_vLightVec0_VS;
        protected EffectParameter g_vUserFlag0_VS;
        protected EffectParameter g_vLightVec0_PS;
        protected EffectParameter g_vEyePos_PS;
        protected EffectParameter g_vFadeMulti_PS;
        protected EffectParameter g_vFadeRim_PS;
        protected EffectParameter g_vFadeAdd_PS;
        protected EffectParameter g_vColor0_PS;
        protected EffectParameter g_vParam1_PS;
        protected EffectParameter g_vParam2_PS;
        protected EffectParameter g_vParam3_PS;
        protected EffectParameter g_vParam4_PS;
        protected EffectParameter g_vParam5_PS;

        protected EffectParameter g_MaterialCol0_VS;
        protected EffectParameter g_MaterialCol1_VS;
        protected EffectParameter g_MaterialCol2_VS;
        protected EffectParameter g_MaterialCol3_VS;
        protected EffectParameter g_MaterialCol0_PS;
        protected EffectParameter g_MaterialCol1_PS;
        protected EffectParameter g_MaterialCol2_PS;
        protected EffectParameter g_MaterialCol3_PS;
        protected EffectParameter g_TexScroll0_PS;
        protected EffectParameter g_TexScroll1_PS;
        protected EffectParameter g_TexScroll0_VS;
        protected EffectParameter g_TexScroll1_VS;
        protected EffectParameter g_vTexTile01_VS;
        protected EffectParameter g_vTexTile23_VS;

        protected EffectParameter g_mMatrixPalette_VS;
        protected EffectParameter g_bSkinning_VS;


        //Updating
        public bool IsShaderProgramDirty { get; set; }
        public bool IsDirty { get; set; }

        //Color Fade
        private float[] ECF_Multi = null;
        private float[] ECF_Rim = null;
        private float[] ECF_Add = null;

        //VFX Light
        private float[] LIGHT_RGBA = null;
        private float[] LIGHT_Radius = null;
        private float[] LIGHT_SourcePosition = null;
        private float[] LIGHT_Strength = null;

        private float[] TexScrl0, TexScrl1 = null;
        private float[] MatCol0, MatCol1, MatCol2, MatCol3 = null;


        public Xv2ShaderEffect(EmmMaterial material, ShaderType type, GameBase gameBase) : base(gameBase.GraphicsDevice)
        {
            GraphicsDevice = gameBase.GraphicsDevice;
            Material = material;
            GameBase = gameBase;
            ShaderType = type;

            InitTechnique();

            material.DecompiledParameters.PropertyChanged += DecompiledParameters_PropertyChanged;
            material.PropertyChanged += Material_PropertyChanged;
        }

        protected Xv2ShaderEffect(EmmMaterial material, bool delayInit, GameBase gameBase) : base(gameBase.GraphicsDevice)
        {
            GraphicsDevice = gameBase.GraphicsDevice;
            Material = material;
            GameBase = gameBase;

            if(!delayInit)
                InitTechnique();

            material.DecompiledParameters.PropertyChanged += DecompiledParameters_PropertyChanged;
            material.PropertyChanged += Material_PropertyChanged;
        }

        protected Xv2ShaderEffect(GameBase gameBase) : base(gameBase.GraphicsDevice)
        {
        }

        public void SetShaderType(ShaderType type)
        {
            if(ShaderType != type)
            {
                ShaderType = type;
                InitTechnique();
            }
        }

        public void InitTechnique()
        {
            //Only grab the shader program if this shader effect is for a material, and not a ShaderProgram itself
            if(ShaderType != ShaderType.PostFilter)
                shaderProgram = GameBase.ShaderManager.GetShaderProgram(Material.ShaderProgram);

            //Initialize the SDS parameter array. These are the list of parameters this shader uses and should be updated OnApply
            SdsParameters = new ShaderParameter[shaderProgram.SdsEntry.Parameters.Count];

            for(int i = 0; i < shaderProgram.SdsEntry.Parameters.Count; i++)
            {
                if (!Enum.TryParse(shaderProgram.SdsEntry.Parameters[i].Name, out ShaderParameter param))
                {
                    param = ShaderParameter.Unknown;
                    Editor.Log.Add($"Unknown ShaderParameter: {shaderProgram.SdsEntry.Parameters[i].Name}", Editor.LogType.Debug);
                }

                SdsParameters[i] = param;
            }

            //Start load parametrs
            EffectParameter[] parameters = new EffectParameter[shaderProgram.VsParser.CBuffers.Sum(x => x.Variables.Length) + shaderProgram.PsParser.CBuffers.Sum(x => x.Variables.Length)];

            //Initialize CBs
            ReadConstantBuffers(shaderProgram);

            //Parameters
            int idx = 0;
            ReadParameters(shaderProgram.VsParser, parameters, ref idx);
            ReadParameters(shaderProgram.PsParser, parameters, ref idx);

            Parameters = new EffectParameterCollection(parameters);

            //Load shaders
            _shaders = new Xv2Shader[2];
            _shaders[0] = new Xv2Shader(GraphicsDevice, this, shaderProgram, true);
            _shaders[1] = new Xv2Shader(GraphicsDevice, this, shaderProgram, false);

            //Set initial parameters from EMM
            SetParameters();

            //Set pass
            EffectTechnique[] techniques = new EffectTechnique[1];
            EffectPassCollection passes = ReadPasses();
            techniques[0] = new EffectTechnique(this, shaderProgram.Name, passes, null);

            Techniques = new EffectTechniqueCollection(techniques);
            CurrentTechnique = Techniques[0];
        }

        private void ReadParameters(DxbcParser shaderParser, EffectParameter[] parameters, ref int idx)
        {
            foreach (var cbuffer in shaderParser.CBuffers)
            {
                foreach (var param in cbuffer.Variables)
                {
                    var class_ = (EffectParameterClass)param.VariableClass;
                    var type = (EffectParameterType)param.VariableType;
                    var name = param.Name;
                    var semantic = string.Empty; //XV2 only has these on input/output
                    var rowCount = (int)param.NumMatrixRows;
                    var columnCount = (int)param.NumMatrixColumns;
                    var elements = param.NumArrayVariables;
                    var structMembers = param.NumStructMembers;

                    object data = null;
                    EffectParameter[] elementParameters = null;

                    if (elements > 0)
                    {
                        if (rowCount > 0 && columnCount > 0)
                        {
                            //Is []
                            byte[] defaultValue = (param.DefaultValue != null) ? param.DefaultValue : new byte[rowCount * columnCount * elements * 4];
                            elementParameters = new EffectParameter[elements];
                            int elementSize = param.VariableSize / elements;
                            object arrayValue = null;

                            for (int i = 0; i < elements; i++)
                            {
                                switch (type)
                                {
                                    case EffectParameterType.Bool:
                                    case EffectParameterType.Int32:
                                        {
                                            var buffer = new int[rowCount * columnCount];
                                            for (var j = 0; j < buffer.Length; j++)
                                                buffer[j] = BitConverter.ToInt32(defaultValue, j * 4 + (elementSize * i));
                                            arrayValue = buffer;
                                            break;
                                        }
                                    case EffectParameterType.Single:
                                        {
                                            var buffer = new float[rowCount * columnCount];
                                            for (var j = 0; j < buffer.Length; j++)
                                                buffer[j] = BitConverter.ToSingle(defaultValue, j * 4 + (elementSize * i));
                                            arrayValue = buffer;
                                            break;
                                        }

                                    case EffectParameterType.String:
                                        throw new NotSupportedException();

                                    default:
                                        break;
                                }

                                elementParameters[i] = new EffectParameter(class_, type, $"{name}_{i}", rowCount, columnCount, semantic, null, null, null, arrayValue);
                            }
                        }
                        else
                        {
                            //Is float, int or bool array, or a matrix
                            byte[] defaultValue = (param.DefaultValue != null) ? param.DefaultValue : new byte[4 * elements];
                            elementParameters = new EffectParameter[elements];
                            object arrayValue = null;

                            for (int i = 0; i < elements; i++)
                            {
                                switch (type)
                                {
                                    case EffectParameterType.Bool:
                                    case EffectParameterType.Int32:
                                        arrayValue = BitConverter.ToInt32(defaultValue, 4 * i);
                                        break;
                                    case EffectParameterType.Single:
                                        arrayValue = BitConverter.ToSingle(defaultValue, 4 * i);
                                        break;
                                    case EffectParameterType.String:
                                        throw new NotSupportedException();
                                }

                                elementParameters[i] = new EffectParameter(class_, type, $"{name}_{i}", 0, 0, semantic, null, null, null, arrayValue);
                            }
                        }
                    }
                    else if (elements == 0 && structMembers == 0)
                    {
                        byte[] defaultValue = (param.DefaultValue != null) ? param.DefaultValue : new byte[param.VariableSize];

                        switch (type)
                        {
                            case EffectParameterType.Bool:
                            case EffectParameterType.Int32:
                                {
                                    var buffer = new int[rowCount * columnCount];
                                    for (var j = 0; j < buffer.Length; j++)
                                        buffer[j] = BitConverter.ToInt32(defaultValue, j * 4);
                                    data = buffer;
                                    break;
                                }
                            case EffectParameterType.Single:
                                {
                                    var buffer = new float[rowCount * columnCount];
                                    for (var j = 0; j < buffer.Length; j++)
                                        buffer[j] = BitConverter.ToSingle(defaultValue, j * 4);
                                    data = buffer;
                                    break;
                                }

                            case EffectParameterType.String:
                                // TODO: We have not investigated what a string
                                // type should do in the parameter list.  Till then
                                // throw to let the user know.
                                throw new NotSupportedException();

                            default:
                                // NOTE: We skip over all other types as they 
                                // don't get added to the constant buffer.
                                break;
                        }
                    }

                    if (structMembers > 0)
                        throw new Exception($"Shader has structMembers ({shaderProgram.Name}, Vertex = {shaderProgram.VsParser == shaderParser}). Cannot parse this shader.");


                    EffectParameterCollection paramCollection = (elements > 0) ? new EffectParameterCollection(elementParameters) : null;

                    parameters[idx] = new EffectParameter(class_, type, name, rowCount, columnCount, semantic, null, paramCollection, null, data);
                    idx++;
                }
            }
        }

        private void ReadConstantBuffers(ShaderProgram shaderProgram)
        {
            // Read in all the constant buffers.
            var buffers = shaderProgram.VsParser.CBuffers.Length + shaderProgram.PsParser.CBuffers.Length;
            ConstantBuffers = new ConstantBuffer[buffers];

            int idx = 0;
            int pIdx = 0;

            ReadConstantBuffersForShader(shaderProgram.VsParser, ref idx, ref pIdx);
            ReadConstantBuffersForShader(shaderProgram.PsParser, ref idx, ref pIdx);
        }

        private void ReadConstantBuffersForShader(DxbcParser parser, ref int idx, ref int pIdx)
        {
            for (var i = 0; i < parser.CBuffers.Length; i++)
            {
                var name = parser.CBuffers[i].Name;

                // Read the parameter index values.
                var parameters = new int[parser.CBuffers[i].NumVariables];
                var offsets = new int[parameters.Length];
                int variableOffset = 0;

                for (var a = 0; a < parameters.Length; a++)
                {
                    parameters[a] = pIdx; //This is the index of the parameter in EffectParameterCollection. This should be equal to the idx of the CB plus 5 for the textures (which are added first).
                    offsets[a] = variableOffset; //Offset of each individual variable in this CB. This should be set to the sum of all previous variable sizes.

                    variableOffset += parser.CBuffers[i].Variables[a].VariableSize;
                    pIdx++;
                }

                var buffer = new ConstantBuffer(GraphicsDevice,
                                                parser.CBuffers[i].SizeInBytes,
                                                parameters,
                                                offsets,
                                                parser.CBuffers[i].Name,
                                                parser.CBuffers[i].Slot);
                ConstantBuffers[idx] = buffer;
                idx++;
            }
        }

        private EffectPassCollection ReadPasses()
        {
            var passes = new EffectPass[1];

            var name = $"{shaderProgram.Name}_Pass1";

            // Get the vertex shader.
            Xv2Shader vertexShader = _shaders[0];

            // Get the pixel shader.
            Xv2Shader pixelShader = _shaders[1];

            BlendState blend = GetBlendState();
            DepthStencilState depth = GetDepthState();
            RasterizerState raster = GetRasterizerState();

            passes[0] = new EffectPass(this, name, vertexShader, pixelShader, blend, depth, raster, null);

            return new EffectPassCollection(passes);
        }

        protected virtual void SetParameters()
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
            }

            //vs_common_light_cb
            if (shaderProgram.UseVertexShaderBuffer[VS_COMMON_LIGHT_CB])
            {
                Parameters["g_vAmbUni_VS"].SetVector4(MatParam.MatAmb.Values, MatParam.MatAmbScale);
                Parameters["g_vRim_VS"].SetValue(new Vector4(MatParam.RimCoeff, MatParam.RimPower, 0, 0));
                Parameters["g_vRimColor_VS"].SetValue(new Vector4(0.77876f, 0.90265f, 1, 1));
                Parameters["g_vSpecular_VS"].SetValue(new Vector4(MatParam.SpcCoeff, MatParam.SpcPower, 0, 0));
                Parameters["g_vEyePos_VS"].SetVector4(MatParam.gCamPos.Values);
                Parameters["g_vLightDif0_VS"].SetVector4(MatParam.MatDif.Values, MatParam.MatDifScale);
                Parameters["g_vLightSpc0_VS"].SetVector4(MatParam.MatSpc.Values);

                Parameters["g_vLightVec1_VS"].SetValue(new Vector3(0f, 1f, 0f));
                Parameters["g_vLightVec2_VS"].SetValue(new Vector3(0f, 1f, 0f));
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
                Parameters["g_Incidence_VS"].SetValue(new Vector4(MatParam.IncidencePower, MatParam.IncidenceAlphaBias, 0, 0));
                Parameters["g_Gradient_VS"].SetValue(new Vector4(MatParam.GradientInit, MatParam.GradientSpeed, 0, 0));
                Parameters["g_GlareCoeff_VS"].SetVector4(MatParam.GlareCol.Values);
                Parameters["g_Reflection_VS"].SetValue(new Vector4(MatParam.ReflectCoeff, MatParam.ReflectFresnelBias, MatParam.ReflectFresnelCoeff, 0));
                Parameters["g_EdgeColor_VS"].SetValue(Vector4.One);
            }

            //vs_versatile_cb
            if (shaderProgram.UseVertexShaderBuffer[VS_VERSATILE_CB])
            {
                //g_vUserFlag0_VS 71.44044, 1.72659, 88.18906, 1.00 float4

                //Parameters["g_vParam0_VS"].SetValue(new Vector4(30.00f, 1.00f, 8.56854f, 0.00f));
                //Parameters["g_vParam1_VS"].SetValue(new Vector4(53.35901f, 64.98734f, 0.025f, 0.025f));
                //Parameters["g_vUserFlag0_VS"].SetValue(new Vector4(71.44044f, 1.72659f, 88.18906f, 1.00f));

                //Parameters["g_vParam0_VS"].SetValue(new Vector4(1, 1, 1, 1));
                //Parameters["g_vUserFlag0_VS"].SetValue(new Vector4(0, 1.4f, -1, 0));

                //g_vUserFlag0_VS: affects lighting
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
                Parameters["g_vShadowMap_PS"].SetValue(new Vector4(2048f, 00049f, 0.001f, 0.85f));
                //Parameters["g_vShadowMap_PS"].SetValue(new Vector4(4096f, 0.00012f, 0.001f, 0.85f));
                Parameters["g_vShadowColor_PS"].SetValue(new Vector4(0.5f, 0.5f, 0.5f, 0));
                Parameters["g_vEdge_PS"].SetValue(MatParam.NoEdge > 0 ? Vector4.Zero : Vector4.One);
                Parameters["g_vGlare_PS"].SetValue(Vector4.Zero);
                Parameters["g_vTone_PS"].SetValue(Vector4.One);
                Parameters["g_vScale_PS"].SetValue(new Vector4(1.00f, 0.00f, 0.50f, 0.50f));

                //Values from BFten
                Parameters["g_vFogMultiColor_PS"].SetValue(new Vector4(0.75229f, 0.79817f, 0.875f, 1.00f));
                Parameters["g_vFogAddColor_PS"].SetValue(new Vector4(0.11009f, 0.15596f, 0.38095f, 1.00f));
            }

            //ps_alphatest_cb
            if (shaderProgram.UsePixelShaderBuffer[PS_ALPHATEST_CB])
            {
                Parameters["g_vAlphaTest_PS"].SetValue(MatParam.AlphaTest > 0 ? new Vector4(MatParam.AlphaTest / 255f, 0, 0, 0) : Vector4.Zero);
            }

            //ps_common_cb
            if (shaderProgram.UsePixelShaderBuffer[PS_COMMON_CB])
            {
                Parameters["g_vAmbUni_PS"].SetVector4(MatParam.MatAmb.Values, MatParam.MatAmbScale);
                Parameters["g_vLightDif0_PS"].SetVector4(MatParam.gLightDif.Values, MatParam.MatDifScale);

                Parameters["g_vHemiA_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vHemiB_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vHemiC_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vLodViewport_PS"].SetValue(Vector4.One);

                Parameters["g_vRimColor_PS"].SetValue(new Vector4(0.77876f, 0.90265f, 1, 1));
                //Parameters["g_vLightVec0_PS"].SetValue(MatParam.g_vLightVec);
                Parameters["g_vLightVec1_PS"].SetValue(new Vector3(0f, 1f, 0f));
                Parameters["g_vLightVec2_PS"].SetValue(new Vector3(0f, 1f, 0f));
                Parameters["g_vLightSpc0_PS"].SetVector4(MatParam.gLightSpc.Values);
                Parameters["g_vLightDif0_PS"].SetVector4(MatParam.gLightDif.Values);
                Parameters["g_vEyePos_PS"].SetVector4(MatParam.gCamPos.Values);
                Parameters["g_vSpecular_PS"].SetValue(new Vector4(MatParam.SpcCoeff, MatParam.SpcPower, 0, 0));
                Parameters["g_vRim_PS"].SetValue(new Vector4(MatParam.RimCoeff, MatParam.RimPower, 0, 0));
                Parameters["g_TexScroll0_PS"].SetVector4(MatParam.TexScrl0.Values);
                Parameters["g_TexScroll1_PS"].SetVector4(MatParam.TexScrl1.Values);
                Parameters["g_MaterialCol0_PS"].SetVector4(MatParam.MatCol0.Values);
                Parameters["g_MaterialCol1_PS"].SetVector4(MatParam.MatCol1.Values);
                Parameters["g_MaterialCol2_PS"].SetVector4(MatParam.MatCol2.Values);
                Parameters["g_MaterialCol3_PS"].SetVector4(MatParam.MatCol3.Values);
                Parameters["g_MaterialOffset0_PS"].SetVector4(MatParam.MatOffset0.Values);
                Parameters["g_MaterialOffset1_PS"].SetVector4(MatParam.MatOffset1.Values);
                Parameters["g_MaterialScale0_PS"].SetVector4(MatParam.MatScale0.Values);
                Parameters["g_MaterialScale1_PS"].SetVector4(MatParam.MatScale1.Values);
                Parameters["g_AlphaFade_PS"].SetValue(new Vector4(MatParam.FadeInit, MatParam.FadeSpeed, 0, 0));
                Parameters["g_Incidence_PS"].SetValue(new Vector4(MatParam.IncidencePower, MatParam.IncidenceAlphaBias, 0, 0));
                Parameters["g_Gradient_PS"].SetValue(new Vector4(MatParam.GradientInit, MatParam.GradientSpeed, 0, 0));
                Parameters["g_GlareCoeff_PS"].SetVector4(MatParam.GlareCol.Values);
                Parameters["g_Reflection_PS"].SetValue(new Vector4(MatParam.ReflectCoeff, MatParam.ReflectFresnelBias, MatParam.ReflectFresnelCoeff, 0));
                Parameters["g_EdgeColor_PS"].SetValue(Vector4.One);


            }

            //ps_versatile_cb
            if (shaderProgram.UsePixelShaderBuffer[PS_VERSATILE_CB])
            {
                Parameters["g_vParam0_PS"].SetValue(new Vector4(1.0f, 0.0f, 1.0f, 1.0f));
                Parameters["g_vParam1_PS"].SetValue(new Vector4(0, 0, 0, 1f));
                Parameters["g_vParam2_PS"].SetValue(new Vector4(0, 0, 0, 1f));
                Parameters["g_vParam3_PS"].SetValue(new Vector4(1f));
                Parameters["g_vParam4_PS"].SetValue(new Vector4(0.25f, 0.75f, 0f, 0f)); //x: for dyt color, y : for color from samlpe14 (second dyt) , so test normal RAD for unbreak shaders. and est with dyt.emb.data001.dds on samlper14. (XenoViewer comment. Is wrong about Sampler14 - that is not a dyt, but common lighting), EDIT once again: this cant be true, since this param is for LIGHT animations?

                //Parameters["g_vParam4_PS"].SetValue(new Vector4(0.25f, 0.0f, 0f, 0f)); 

                Parameters["g_vParam7_PS"].SetValue(new Vector4(0.0f, 23.2558f, 0.04587f, 0.0f)); //Toon Detail Parameter 
                Parameters["g_vParam11_PS"].SetValue(new Vector4(1f, 1f, 0.22f, 0.25f));
                Parameters["g_vParam12_PS"].SetValue(new Vector4(0, 0, 0, 1));
                Parameters["g_vParam13_PS"].SetValue(new Vector4(1f));
                Parameters["g_vParam14_PS"].SetValue(new Vector4(1f));
            }

            //cb_ps_bool
            if (shaderProgram.UsePixelShaderBuffer[CB_PS_BOOL])
            {
                Parameters["g_bFog_PS"].SetValue(false); //Disable for now. Default values are set from BFten, but that may not look good on all stages. Likely the stage spm contains these values which XenoKit has no way of loading as of now.
                Parameters["g_bOutputGlareMRT_PS"].SetValue(MatParam.Glare == 1);
                Parameters["ps_bool_padding0"].SetValue(true);
            }

            //Set parameter references that will be updated every frame (since doing it via string look up would be bad)
            g_mWVP_VS = Parameters["g_mWVP_VS"];
            g_mVP_VS = Parameters["g_mVP_VS"];
            g_mWVP_Prev_VS = Parameters["g_mWVP_Prev_VS"];
            g_mWV_VS = Parameters["g_mWV_VS"];
            g_mW_VS = Parameters["g_mW_VS"];
            g_mWLP_SM_VS = Parameters["g_mWLP_SM_VS"];
            g_vScreen_VS = Parameters["g_vScreen_VS"];
            g_SystemTime_VS = Parameters["g_SystemTime_VS"];
            g_vEyePos_VS = Parameters["g_vEyePos_VS"];
            g_vLightVec0_VS = Parameters["g_vLightVec0_VS"];
            g_vUserFlag0_VS = Parameters["g_vUserFlag0_VS"];
            g_vLightVec0_PS = Parameters["g_vLightVec0_PS"];
            g_vEyePos_PS = Parameters["g_vEyePos_PS"];

            g_vFadeMulti_PS = Parameters["g_vFadeMulti_PS"];
            g_vFadeRim_PS = Parameters["g_vFadeRim_PS"];
            g_vFadeAdd_PS = Parameters["g_vFadeAdd_PS"];

            g_vColor0_PS = Parameters["g_vColor0_PS"];
            g_vParam1_PS = Parameters["g_vParam1_PS"];
            g_vParam2_PS = Parameters["g_vParam2_PS"];
            g_vParam3_PS = Parameters["g_vParam3_PS"];
            g_vParam4_PS = Parameters["g_vParam4_PS"];
            g_vParam5_PS = Parameters["g_vParam5_PS"];

            g_MaterialCol0_PS = Parameters["g_MaterialCol0_PS"];
            g_MaterialCol1_PS = Parameters["g_MaterialCol1_PS"];
            g_MaterialCol2_PS = Parameters["g_MaterialCol2_PS"];
            g_MaterialCol3_PS = Parameters["g_MaterialCol3_PS"];
            g_MaterialCol0_VS = Parameters["g_MaterialCol0_VS"];
            g_MaterialCol1_VS = Parameters["g_MaterialCol1_VS"];
            g_MaterialCol2_VS = Parameters["g_MaterialCol2_VS"];
            g_MaterialCol3_VS = Parameters["g_MaterialCol3_VS"];
            g_TexScroll0_PS = Parameters["g_TexScroll0_PS"];
            g_TexScroll1_PS = Parameters["g_TexScroll1_PS"];
            g_TexScroll0_VS = Parameters["g_TexScroll0_VS"];
            g_TexScroll1_VS = Parameters["g_TexScroll1_VS"];
            g_vTexTile01_VS = Parameters["g_vTexTile01_VS"];
            g_vTexTile23_VS = Parameters["g_vTexTile23_VS"];

            g_bSkinning_VS = Parameters["g_bSkinning_VS"];
            g_mMatrixPalette_VS = Parameters["g_mMatrixPalette_VS"];
        }

        private void ApplyParameter(ShaderParameter parameter)
        {
            switch (parameter)
            {
                case ShaderParameter.W:
                    g_mW_VS.SetValue(World);
                    break;
                case ShaderParameter.WV:
                    g_mWV_VS.SetValue(World * GameBase.ActiveCameraBase.ViewMatrix);
                    break;
                case ShaderParameter.WVP:
                    WVP = World * GameBase.ActiveCameraBase.ViewMatrix * GameBase.ActiveCameraBase.ProjectionMatrix;
                    g_mWVP_VS.SetValue(WVP);
                    break;
                case ShaderParameter.VP:
                    g_mVP_VS.SetValue(GameBase.ActiveCameraBase.ViewMatrix * GameBase.ActiveCameraBase.ProjectionMatrix);
                    break;
                case ShaderParameter.WLPB_SM:
                    break;
                case ShaderParameter.WLPB_PM:
                    break;
                case ShaderParameter.WLP_PM:
                    break;
                case ShaderParameter.WLP_SM:
                    break;
                case ShaderParameter.WIT:
                    break;
                case ShaderParameter.WVP_Prev:
                    g_mWVP_Prev_VS.SetValue(PrevWVP);
                    break;

                case ShaderParameter.MatCol0_PS:
                    g_MaterialCol0_PS.SetVector4(MatCol0 != null ? MatCol0 : Material.DecompiledParameters.MatCol0.Values);
                    break;
                case ShaderParameter.MatCol1_PS:
                    g_MaterialCol1_PS.SetVector4(MatCol1 != null ? MatCol1 : Material.DecompiledParameters.MatCol1.Values);
                    break;
                case ShaderParameter.MatCol2_PS:
                    g_MaterialCol2_PS.SetVector4(MatCol2 != null ? MatCol2 : Material.DecompiledParameters.MatCol2.Values);
                    break;
                case ShaderParameter.MatCol3_PS:
                    g_MaterialCol3_PS.SetVector4(MatCol3 != null ? MatCol3 : Material.DecompiledParameters.MatCol3.Values);
                    break;
                case ShaderParameter.MatCol0_VS:
                    g_MaterialCol0_VS.SetVector4(MatCol0 != null ? MatCol0 : Material.DecompiledParameters.MatCol0.Values);
                    break;
                case ShaderParameter.MatCol1_VS:
                    g_MaterialCol1_VS.SetVector4(MatCol1 != null ? MatCol1 : Material.DecompiledParameters.MatCol1.Values);
                    break;
                case ShaderParameter.MatCol2_VS:
                    g_MaterialCol2_VS.SetVector4(MatCol2 != null ? MatCol2 : Material.DecompiledParameters.MatCol2.Values);
                    break;
                case ShaderParameter.MatCol3_VS:
                    g_MaterialCol3_VS.SetVector4(MatCol3 != null ? MatCol3 : Material.DecompiledParameters.MatCol3.Values);
                    break;

                case ShaderParameter.TexScrl0_VS:
                    g_TexScroll0_VS.SetVector4(TexScrl0 != null ? TexScrl0 : Material.DecompiledParameters.TexScrl0.Values);
                    break;
                case ShaderParameter.TexScrl1_VS:
                    g_TexScroll1_VS.SetVector4(TexScrl1 != null ? TexScrl1 : Material.DecompiledParameters.TexScrl1.Values);
                    break;
                case ShaderParameter.TexScrl0_PS:
                    g_TexScroll0_PS.SetVector4(TexScrl0 != null ? TexScrl0 : Material.DecompiledParameters.TexScrl0.Values);
                    break;
                case ShaderParameter.TexScrl1_PS:
                    g_TexScroll1_PS.SetVector4(TexScrl1 != null ? TexScrl1 : Material.DecompiledParameters.TexScrl1.Values);
                    break;

                case ShaderParameter.LightVec0_VS:
                    g_vLightVec0_VS.SetValue(GameBase.LightSource.Direction);
                    break;
                case ShaderParameter.LightVec0_PS:
                    g_vLightVec0_PS.SetValue(GameBase.LightSource.Direction);
                    break;
                case ShaderParameter.UserFlag0_VS:
                    g_vUserFlag0_VS.SetValue(GameBase.LightSource.Position);
                    break;

                case ShaderParameter.FadeColor:
                    if (ECF_Multi != null)
                    {
                        g_vFadeMulti_PS.SetVector4(ECF_Multi);
                        g_vFadeRim_PS.SetVector4(ECF_Rim);
                        g_vFadeAdd_PS.SetVector4(ECF_Add);
                    }
                    else
                    {
                        //Set default values
                        g_vFadeMulti_PS.SetValue(Vector4.One);
                        g_vFadeRim_PS.SetValue(Vector4.Zero);
                        g_vFadeAdd_PS.SetValue(Vector4.Zero);
                    }
                    break;

            }
        }

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

            foreach(ShaderParameter parameter in SdsParameters)
            {
                ApplyParameter(parameter);
            }

            if(ShaderType == ShaderType.Chara)
            {
                if (LIGHT_RGBA != null)
                {
                    g_vColor0_PS.SetVector4(LIGHT_RGBA);
                    g_vParam3_PS.SetVector4(LIGHT_Strength);
                    g_vParam4_PS.SetVector4(LIGHT_Radius);
                    g_vParam5_PS.SetVector4(LIGHT_SourcePosition);
                }
                else
                {
                    g_vColor0_PS.SetValue(Vector4.Zero);
                    g_vParam3_PS.SetValue(Vector4.One);
                    g_vParam4_PS.SetValue(Vector4.Zero);
                    g_vParam5_PS.SetValue(Vector4.Zero);
                }
            }

            if (ShaderType == ShaderType.CharaNormals)
            {
                //Controls the final vertex color of NormalPassRT0 and 1, which is key for the black body outline around characters as well as BPE BodyOutlines 
                g_vParam1_PS.SetValue(new Vector4(0, 50, 1, 0.6f)); //Related to RT0
                g_vParam2_PS.SetValue(new Vector4(0.01563f, 0, 1, 0)); //X = NormalPassRT1 B? Z is related to RT0
                g_vParam3_PS.SetValue(new Vector4(0.001f, 1000, 0.92638f, 0.90798f)); //Z = NormalPassRT1 Alpha (This is 0 if no BodyOutline BPE, else some value greater than 0. Values seen: 0.92549 = chara 1, 0.4: = chara 2)

                //Testing:
                Parameters["g_vParam0_PS"].SetValue(Vector4.Zero);
                g_vParam1_PS.SetValue(new Vector4(0, 1, 1f, 0.6f)); //W = Outline strength (chara black edgeline), Y = somehow affects size of outline, default value is 50 but that looks off in XenoKit so a value of 1 is used instead
                g_vParam2_PS.SetValue(new Vector4(0.00391f * 6, 0.00f, 1f, 0.00f)); //X = Pixel color in main color pallete texture (0.00391 * index) (BPE)
                g_vParam3_PS.SetValue(new Vector4(0.001f, 10000.00f, 0.40f, 0.00f)); //Z = BPE Outline strength
                return;
            }

            
            //if(ShaderType == ShaderType.Default)
            {
                if (shaderProgram.UsePixelShaderBuffer[CB_PS_BOOL])
                {
                    Parameters["g_bFog_PS"].SetValue(true);
                    Parameters["g_bDepthTex_PS"].SetValue(true);
                }
            }
            
            //Update global parameters
            if (shaderProgram.UseVertexShaderBuffer[VS_STAGE_CB])
            {
                g_vScreen_VS.SetVector4(GameBase.RenderSystem.RenderResolution);
                g_SystemTime_VS.SetValue(SceneManager.SystemTime);
            }

            //Remove references to animated parameters from this pass
            MatCol0 = null;
            MatCol1 = null;
            MatCol2 = null;
            MatCol3 = null;
            TexScrl0 = null;
            TexScrl1 = null;

            //Set global samplers/textures
            foreach (Xv2Shader shader in _shaders)
                shader.SetGlobalSamplers(GameBase.ShaderManager);
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (_shaders != null)
                    {
                        foreach (var shader in _shaders)
                            shader.Dispose();
                    }

                    if (ConstantBuffers != null)
                    {
                        foreach (var buffer in ConstantBuffers)
                            buffer.Dispose();
                        ConstantBuffers = null;
                    }
                }
            }

            base.Dispose(disposing);
        }

        public int IndexOfConstantBuffer(string name)
        {
            for (int i = 0; i < ConstantBuffers.Length; i++)
            {
                if (ConstantBuffers[i]._name == name) return i;
            }

            return -1;
        }


        //Render Settings
        public virtual BlendState GetBlendState()
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
            else if(ShaderType == ShaderType.Stage)
            {
                blendState.ApplyNone(0);
                blendState.ApplyNone(1);
                blendState[0].ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;

                return blendState;
            }

            //Default path; used for effects (PBIND, TBIND and EMO) and regular character shaders
            switch (MatParam.AlphaBlendType)
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

                    if(Material.DecompiledParameters.LowRez == 1 || Material.DecompiledParameters.LowRezSmoke == 1)
                    {
                        blendState[1].ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;

                        //Re-enable these once LowRez is implemented in the renderer
                        blendState[0].AlphaSourceBlend = Blend.Zero;
                        blendState[1].AlphaSourceBlend = Blend.Zero;
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

        public virtual DepthStencilState GetDepthState()
        {
            DepthStencilState depth = new DepthStencilState();

            if(ShaderType == ShaderType.CharaNormals || ShaderType == ShaderType.CharaShadow)
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
            else if(ShaderType == ShaderType.Chara)
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

                return depth;
            }

            //Default path: Valid for effects
            if (MatParam.AlphaSortMask == 1 || MatParam.ZTestMask == 1)
                depth.DepthBufferEnable = true;

            if (MatParam.AlphaBlend == 1 && MatParam.AlphaBlendType != 3)
            {
                depth.DepthBufferEnable = true;
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

            if(MatParam.ZWriteMask == 1)
            {
                depth.DepthBufferWriteEnable = false;
            }

            return depth;
        }

        public virtual RasterizerState GetRasterizerState()
        {
            RasterizerState state = new RasterizerState();

            state.FillMode = SceneManager.WireframeModeCharacters ? FillMode.WireFrame : FillMode.Solid;
            state.CullMode = MatParam.BackFace > 0 || MatParam.TwoSidedRender > 0 ? CullMode.None : CullMode.CullCounterClockwiseFace;

            switch (ShaderType)
            {
                case ShaderType.CharaShadow:
                    state.CullMode = CullMode.CullCounterClockwiseFace;
                    break;
            }

            return state;
        }

        public static Xv2ShaderEffect CreateDefaultMaterial(ShaderType type, GameBase gameBase)
        {
            EmmMaterial mat = new EmmMaterial();
            mat.ShaderProgram = "TOON_UNIF_STAIN3_DFD";
            mat.Name = "default";
            mat.DecompileParameters();

            return new Xv2ShaderEffect(mat, type, gameBase);
        }

        //Auto-updating
        private void DecompiledParameters_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DecompiledMaterial.ParametersChanged))
            {
                if (Material.DecompiledParameters.ParametersChanged == 1)
                {
                    IsDirty = true;
                }
                else if (Material.DecompiledParameters.ParametersChanged == 2)
                {
                    IsShaderProgramDirty = true;
                }

                Material.DecompiledParameters.ResetParametersChanged();
            }
        }

        private void Material_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Material.ShaderProgram))
            {
                IsShaderProgramDirty = true;
            }
        }

        public void Release()
        {
            Material.DecompiledParameters.PropertyChanged -= DecompiledParameters_PropertyChanged;
            Material.PropertyChanged -= Material_PropertyChanged;
        }

        //VFX
        public void SetColorFade(Actor actor)
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

        public void SetVfxLight()
        {
            VfxLight light = GameBase.VfxManager.GetActiveLight(World);

            if (light != null)
            {
                LIGHT_RGBA = light.RGBA;
                LIGHT_Radius = light.Light;
                LIGHT_SourcePosition = light.LightSourcePosition;
                LIGHT_Strength = light.LightStrength;
            }
            else
            {
                LIGHT_RGBA = null;
                LIGHT_Radius = null;
                LIGHT_SourcePosition = null;
                LIGHT_Strength = null;
            }
        }

        public void SetMaterialAnimationValues(VfxEmaMaterialNode materialAnimationNode)
        {
            TexScrl0 = materialAnimationNode.TexScrl[0];
            TexScrl1 = materialAnimationNode.TexScrl[1];
            MatCol0 = materialAnimationNode.MatCol[0];
            MatCol1 = materialAnimationNode.MatCol[1];
            MatCol2 = materialAnimationNode.MatCol[2];
            MatCol3 = materialAnimationNode.MatCol[3];
        }

        //Animation
        public void SetSkinningMatrices(Matrix[] matrices)
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

        public void DisableSkinning()
        {
            if (!SkinningEnabled) return;

            if (shaderProgram.UseVertexShaderBuffer[CB_VS_BOOL])
            {
                SkinningEnabled = false;
                g_bSkinning_VS.SetValue(false);
            }
        }

        public void SetEyeMovement(float[] uvScroll)
        {
            if (shaderProgram.UsePixelShaderBuffer[PS_COMMON_CB])
            {
                g_TexScroll0_PS.SetVector4(uvScroll);
            }

            if (shaderProgram.UseVertexShaderBuffer[VS_COMMON_MATERIAL_CB])
            {
                g_TexScroll0_VS.SetVector4(uvScroll);
            }
        }
    
        public void SetTextureTile(float[] texTile01, float[] texTile23)
        {
            if (shaderProgram.UseVertexShaderBuffer[VS_STAGE_CB])
            {
                g_vTexTile01_VS.SetVector4(texTile01);
                g_vTexTile23_VS.SetVector4(texTile23);
            }
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
}
