using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.EMM;
using XenoKit.Engine.Shader.DXBC;
using XenoKit.Engine.Vfx;
using XenoKit.Engine.Vfx.Asset;

namespace XenoKit.Engine.Shader
{
    /// <summary>
    /// Represents a DBXV2 shader instance (ShaderProgram and Materials).
    /// </summary>
    public class Xv2ShaderEffect : Effect
    {
        //Default materials to use when none is found:
        private static Xv2ShaderEffect _defaultCharaMaterial = null;
        public static Xv2ShaderEffect DefaultCharacterMaterial
        {
            get
            {
                if (_defaultCharaMaterial == null)
                {
                    _defaultCharaMaterial = CreateDefaultMaterial(SceneManager.MainGameBase);
                }
                return _defaultCharaMaterial;
            }
        }

        private GameBase GameBase;

        private Xv2Shader[] _shaders;

        public Matrix World = Matrix.Identity;
        public Matrix PrevWVP = Matrix.Identity;

        //Settings
        private ShaderProgram shaderProgram = null;
        public EmmMaterial Material { get; private set; }
        public DecompiledMaterial MatParam => Material?.DecompiledParameters;

        //Parameters
        private EffectParameter g_mWVP_VS;
        private EffectParameter g_mVP_VS;
        private EffectParameter g_mWVP_Prev_VS;
        private EffectParameter g_mWV_VS;
        private EffectParameter g_mW_VS;
        private EffectParameter g_mWLP_SM_VS;
        private EffectParameter g_vScreen_VS;
        private EffectParameter g_SystemTime_VS;
        private EffectParameter g_vEyePos_VS;
        private EffectParameter g_vLightVec0_VS;
        private EffectParameter g_vUserFlag0_VS;
        private EffectParameter g_vLightVec0_PS;
        private EffectParameter g_vEyePos_PS;
        private EffectParameter g_vFadeMulti_PS;
        private EffectParameter g_vFadeRim_PS;
        private EffectParameter g_vFadeAdd_PS;
        private EffectParameter g_vColor0_PS;
        private EffectParameter g_vParam3_PS;
        private EffectParameter g_vParam4_PS;
        private EffectParameter g_vParam5_PS;

        private EffectParameter g_MaterialCol0_VS;
        private EffectParameter g_MaterialCol1_VS;
        private EffectParameter g_MaterialCol2_VS;
        private EffectParameter g_MaterialCol3_VS;
        private EffectParameter g_MaterialCol0_PS;
        private EffectParameter g_MaterialCol1_PS;
        private EffectParameter g_MaterialCol2_PS;
        private EffectParameter g_MaterialCol3_PS;
        private EffectParameter g_TexScroll0_PS;
        private EffectParameter g_TexScroll1_PS;
        private EffectParameter g_TexScroll0_VS;
        private EffectParameter g_TexScroll1_VS;

        private EffectParameter g_mMatrixPalette_VS;
        private EffectParameter g_bSkinning_VS;


        //Updating
        public bool IsShaderProgramDirty { get; set; }
        public bool IsDirty { get; set; }

        //Lighting
        private static Vector4 LightVector = new Vector4(-0.50204f, -0.0f, -0.3682f, 0.00f);

        //Color Fade
        private float[] ECF_Multi = null;
        private float[] ECF_Rim = null;
        private float[] ECF_Add = null;

        //VFX Light
        private float[] LIGHT_RGBA = null;
        private float[] LIGHT_Radius = null;
        private float[] LIGHT_SourcePosition = null;
        private float[] LIGHT_Strength = null;


        public Xv2ShaderEffect(EmmMaterial material, GameBase gameBase) : base(gameBase.GraphicsDevice)
        {
            GraphicsDevice = gameBase.GraphicsDevice;
            Material = material;
            GameBase = gameBase;

            InitTechnique();

            material.DecompiledParameters.PropertyChanged += DecompiledParameters_PropertyChanged;
            material.PropertyChanged += Material_PropertyChanged;
        }

        public void InitTechnique()
        {
            shaderProgram = ShaderManager.Instance.GetShaderProgram(Material.ShaderProgram, GraphicsDevice);

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

                                elementParameters[i] = new EffectParameter(class_, type, $"{name}_{i}", 0, 0, semantic, null, null, null, arrayValue);
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

        private void SetParameters()
        {
            //NOTE: A lot of these default values are taken from XenoViewer. Others are taken from frames captured with RenderDoc.

            //vs_stage_cb
            if (shaderProgram.UseVertexShaderBuffer[1])
            {
                Parameters["g_vHeightFog_VS"].SetValue(new Vector4(-1.0f, 1 / 100, -2, 1 / 1000));
                Parameters["g_vFog_VS"].SetValue(new Vector4(-2, 1 / 1000, -2, 1 / 1000));

                //Parameters["g_vTexTile01_VS"].SetValue(new Vector4(MatParam.TexScrl0.U, MatParam.TexScrl0.V, MatParam.TexScrl1.U, MatParam.TexScrl1.V));
                //Parameters["g_vTexTile23_VS"].SetValue(new Vector4(1f, 1f, 0f, 0f));

                Parameters["g_vTexTile01_VS"].SetValue(new Vector4(1, 1, 1, 1));
                Parameters["g_vTexTile23_VS"].SetValue(new Vector4(1, 1, 1, 1));
            }

            //vs_common_light_cb
            if (shaderProgram.UseVertexShaderBuffer[2])
            {
                Parameters["g_vAmbUni_VS"].SetVector4(MatParam.MatAmb.Values, MatParam.MatAmbScale);
                Parameters["g_vRim_VS"].SetValue(new Vector4(MatParam.RimCoeff, MatParam.RimPower, 0, 0));
                Parameters["g_vRimColor_VS"].SetValue(new Vector4(0.77876f, 0.90265f, 1, 1));
                Parameters["g_vSpecular_VS"].SetValue(new Vector4(MatParam.SpcCoeff, MatParam.SpcPower, 0, 0));
                Parameters["g_vEyePos_VS"].SetVector4(MatParam.gCamPos.Values);
                //Parameters["g_vLightVec0_VS"].SetValue(MatParam.g_vLightVec);
                Parameters["g_vLightDif0_VS"].SetVector4(MatParam.gLightDif.Values);
                Parameters["g_vLightSpc0_VS"].SetVector4(MatParam.gLightSpc.Values);

                Parameters["g_vLightVec1_VS"].SetValue(new Vector3(0f, 1f, 0f));
                Parameters["g_vLightVec2_VS"].SetValue(new Vector3(0f, 1f, 0f));
            }

            //vs_common_material_cb
            if (shaderProgram.UseVertexShaderBuffer[3])
            {
                Parameters["g_TexScroll0_VS"].SetVector4(MatParam.TexScrl0.Values);
                Parameters["g_TexScroll1_VS"].SetVector4(MatParam.TexScrl1.Values);
                Parameters["g_MaterialCol0_VS"].SetVector4(MatParam.MatCol0.Values);
                Parameters["g_MaterialCol1_VS"].SetVector4(MatParam.MatCol1.Values);
                Parameters["g_MaterialCol2_VS"].SetVector4(MatParam.MatCol2.Values);
                Parameters["g_MaterialCol3_VS"].SetVector4(MatParam.MatCol3.Values);
                Parameters["g_MaterialScale0_VS"].SetVector4(MatParam.MatScale0.Values);
                Parameters["g_MaterialScale1_VS"].SetVector4(MatParam.MatScale1.Values);
                Parameters["g_AlphaFade_VS"].SetValue(new Vector4(MatParam.FadeInit, MatParam.FadeSpeed, 0, 0));
                Parameters["g_Incidence_VS"].SetValue(new Vector4(MatParam.IncidencePower, MatParam.IncidenceAlphaBias, 0, 0));
                Parameters["g_Gradient_VS"].SetValue(new Vector4(MatParam.GradientInit, MatParam.GradientSpeed, 0, 0));
                Parameters["g_GlareCoeff_VS"].SetVector4(MatParam.GlareCol.Values);
                Parameters["g_Reflection_VS"].SetValue(new Vector4(MatParam.ReflectCoeff, MatParam.ReflectFresnelBias, MatParam.ReflectFresnelCoeff, 0));
                Parameters["g_EdgeColor_VS"].SetValue(Vector4.One);
            }

            //vs_versatile_cb
            if (shaderProgram.UseVertexShaderBuffer[6])
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
            if (shaderProgram.UseVertexShaderBuffer[8])
            {
                //Force disable hardware skinning. XenoKit does this on the CPU currently.
                Parameters["g_bSkinning_VS"].SetValue(false);

                Parameters["g_bVersatile0_VS"].SetValue(MatParam.VsFlag0);
                Parameters["g_bVersatile1_VS"].SetValue(MatParam.VsFlag1);
                Parameters["g_bVersatile2_VS"].SetValue(MatParam.VsFlag2);
                Parameters["g_bVersatile3_VS"].SetValue(MatParam.VsFlag3);

                Parameters["g_bUserFlag1_VS"].SetValue(true);
                Parameters["g_bUserFlag2_VS"].SetValue(true);
                Parameters["g_bUserFlag3_VS"].SetValue(true);
            }

            //ps_stage_cb
            if (shaderProgram.UsePixelShaderBuffer[0])
            {
                //Parameters["g_vShadowMap_PS"].SetValue(new Vector4(2048f, 00049f, 0.001f, 0.85f));
                Parameters["g_vShadowMap_PS"].SetValue(new Vector4(4096f, 0.00012f, 0.001f, 0.85f));
                Parameters["g_vShadowColor_PS"].SetValue(new Vector4(0.5f, 0.5f, 0.5f, 0));
                Parameters["g_vEdge_PS"].SetValue(MatParam.NoEdge > 0 ? Vector4.Zero : Vector4.One);
                Parameters["g_vGlare_PS"].SetValue(Vector4.Zero);
                Parameters["g_vTone_PS"].SetValue(Vector4.One);

                //Parameters["g_vFogMultiColor_PS"].SetValue(new Vector4(1, 1, 1, 0.2f));
                //Parameters["g_vFogAddColor_PS"].SetValue(Vector4.Zero);

                Parameters["g_vFogMultiColor_PS"].SetValue(new Vector4(0.75229f, 0.79817f, 0.875f, 1.00f));
                Parameters["g_vFogAddColor_PS"].SetValue(new Vector4(0.11009f, 0.15596f, 0.38095f, 1.00f));
                Parameters["g_vScale_PS"].SetValue(new Vector4(1.00f, 0.00f, 0.50f, 0.50f));

            }

            //ps_alphatest_cb
            if (shaderProgram.UsePixelShaderBuffer[1])
            {
                Parameters["g_vAlphaTest_PS"].SetValue(MatParam.AlphaTest > 0 ? new Vector4(MatParam.AlphaTest / 255f, 0, 0, 0) : Vector4.Zero);
            }

            //ps_common_cb
            if (shaderProgram.UsePixelShaderBuffer[2])
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
            if (shaderProgram.UsePixelShaderBuffer[4])
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
            if (shaderProgram.UsePixelShaderBuffer[8])
            {
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

            g_bSkinning_VS = Parameters["g_bSkinning_VS"];
            g_mMatrixPalette_VS = Parameters["g_mMatrixPalette_VS"];
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


            //If shader has vs_matrix_cb, update these parameters every frame.
            if (shaderProgram.UseVertexShaderBuffer[0])
            {
                Matrix viewMatrix = GameBase.ActiveCameraBase.ViewMatrix;
                Matrix projMatrix = GameBase.ActiveCameraBase.ProjectionMatrix;

                g_mWVP_VS.SetValue(World * viewMatrix * projMatrix);
                g_mVP_VS.SetValue(viewMatrix * projMatrix);
                g_mWVP_Prev_VS.SetValue(PrevWVP);
                g_mWV_VS.SetValue(World * viewMatrix);
                g_mW_VS.SetValue(World);
                g_mWLP_SM_VS.SetValue(projMatrix);

                PrevWVP = World * viewMatrix * projMatrix;

                //Parameters["g_mW_VS"].SetValue(Matrix.CreateTranslation(new Vector3(10,0,0)));
            }

            //vs_stage_cb.
            if (shaderProgram.UseVertexShaderBuffer[1])
            {
                g_vScreen_VS.SetValue(SceneManager.ScreenSize);
                g_SystemTime_VS.SetValue(SceneManager.SystemTime);

                //VFX Testing
                /*
                Parameters["g_vAmbOcl_VS"].SetValue(new Vector4(0, 0, 0, 1f));
                Parameters["g_vTexTile01_VS"].SetValue(new Vector4(1,1,1,1));
                Parameters["g_vTexTile23_VS"].SetValue(new Vector4(1,1,1,1));
                Parameters["g_vFog_VS"].SetValue(new Vector4(30.00f, 300.00f, 1.11111f, -0.0037f));
                Parameters["g_vHeightFog_VS"].SetValue(new Vector4(0.11466f, 0.02857f, 1.00033f, -0.00333f));
                Parameters["g_vLayerCastShadow_VS"].SetValue(new Vector4(5.00f, 10.00f, 20.00f, 1000.00f));
                Parameters["g_vLayerReceiveShadow_VS"].SetValue(new Vector4(0.20f, 0.10f, 0.05f, 0.001f));
                Parameters["g_vColor_VS"].SetValue(new Vector4(0.5f, 0.5f, 0.5f, 0f));
                */

                //Parameters["g_vHeightFog_VS"].SetValue(new Vector4(40.00f, 300.00f, 1.15385f, -0.00385f));

            }

            //vs_common_material_cb
            if (shaderProgram.UseVertexShaderBuffer[3])
            {
                //VFX Testing
                /*
                Parameters["g_MaterialScale0_VS"].SetValue(new Vector4(0, 0, 0, 0f));
                Parameters["g_MaterialScale1_VS"].SetValue(new Vector4(0, 0, 0, 0f));
                Parameters["g_MaterialOffset0_VS"].SetValue(new Vector4(10, 4, 0, 0f));
                Parameters["g_MaterialOffset1_VS"].SetValue(new Vector4(0, 0, 0, 0f));
                Parameters["g_AlphaFade_VS"].SetValue(new Vector4(0.390f, 0f, 0f, 0f));
                Parameters["g_Incidence_VS"].SetValue(new Vector4(1, 1, 8, 0));
                Parameters["g_Gradient_VS"].SetValue(new Vector4(1, 0, 0, 0));
                Parameters["g_Reflection_VS"].SetValue(new Vector4(0.3f, 0.5f, 1.00f, 0.00f));
                Parameters["g_vLodViewport_VS"].SetValue(new Vector4(1,1,1,1));

                */
            }

            //vs_common_light_cb.
            if (shaderProgram.UseVertexShaderBuffer[2])
            {
                //Front Side: (Left in XenoKit)
                //Parameters["g_vEyePos_VS"].SetValue(new Vector4(-0.31891f, 0.41145f, -3.78455f, 1.00f));
                //Parameters["g_vLightVec0_VS"].SetValue(new Vector4(0.64923f, -0.42757f, -0.62904f, 0.00f));

                //Back Side: (Right in XenoKit)
                //Parameters["g_vEyePos_VS"].SetValue(new Vector4(-0.2219f, 0.94195f, 3.86588f, 1.00f));
                //Parameters["g_vLightVec0_VS"].SetValue(new Vector4(-0.73446f, -0.3447f, 0.58459f, 0.00f));

                //Right Side: (Front in XenoKit)
                //g_vEyePos_VS.SetValue(new Vector4(-3.727f, 0.14767f, -0.20346f, 1.00f));
                //g_vLightVec0_VS.SetValue(new Vector4(-0.50204f, -0.46655f, -0.7282f, 0.00f));


                //Left Side: (Back in XenoKit)
                //Parameters["g_vEyePos_VS"].SetValue(new Vector4(3.7336f, 0.9493f, -1.02876f, 1.00f));
                //Parameters["g_vLightVec0_VS"].SetValue(new Vector4(0.78974f, -0.34353f, 0.50823f, 0.00f));

                //Light direction / origin point relative to character. Light range is infinite, so large numbers have the same effect as small numbers (unless the sign changes, which then inverts that axis).

                g_vLightVec0_VS.SetValue(GameBase.LightSource.Direction);

            }

            //vs_versatile_cb.
            if (shaderProgram.UseVertexShaderBuffer[6])
            {
                //Camera position / Shade caster. This is where the "shadow" over a character originates.
                //g_vUserFlag0_VS.SetValue(new Vector4(0,-1,-1, 0));
                //g_vUserFlag0_VS.SetValue(new Vector4(SceneManager.CameraInstance.CameraState.Position.X, SceneManager.CameraInstance.CameraState.Position.Y, SceneManager.CameraInstance.CameraState.Position.Z, 0));

                //g_vUserFlag0_VS.SetValue(new Vector4(GameBase.ActiveCameraBase.CameraState.Position.X, GameBase.ActiveCameraBase.CameraState.Position.Y, GameBase.ActiveCameraBase.CameraState.Position.Z, 0));
                g_vUserFlag0_VS.SetValue(GameBase.LightSource.Position);
            }

            //cb_vs_bool
            if (shaderProgram.UseVertexShaderBuffer[8])
            {
                //VFX Testing
                /*
                Parameters["g_bVersatile0_VS"].SetValue(true);
                Parameters["g_bUserFlag2_VS"].SetValue(true);
                Parameters["g_bUserFlag3_VS"].SetValue(true);
                */
            }

            //ps_stage_cb
            if (shaderProgram.UsePixelShaderBuffer[0])
            {
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

                //Parameters["g_vShadowMap_PS"].SetValue(new Vector4(8192, 0.00012f, 0.00006f, 1));
                //Parameters["g_vShadowColor_PS"].SetValue(new Vector4(0.5f, 0.5f, 0.5f, 0));
            }

            //ps_user_cb
            if (shaderProgram.UsePixelShaderBuffer[5])
            {
                //Parameters["g_vShadowParam_PS"].SetValue(new Vector4(0, 0, 0, 1)); //Y must be equal or greater than X, else no shadow
            }

            //ps_common_cb.
            if (shaderProgram.UsePixelShaderBuffer[2])
            {
                //Parameters["g_vEyePos_PS"].SetValue(new Vector4(-0.50204f, -0.46655f, -0.7282f, 0.00f));

                //Front Side:
                //Parameters["g_vLightVec0_PS"].SetValue(new Vector4(0.64923f, -0.42757f, -0.62904f, 0.00f));

                //Back Side:
                //Parameters["g_vLightVec0_PS"].SetValue(new Vector4(-0.73446f, -0.3447f, 0.58459f, 0.00f));

                //Right Side:
                //g_vLightVec0_PS.SetValue(new Vector4(-0.69975f, -0.25339f, 0.66794f, 0.00f));

                //Left Side:
                //Parameters["g_vLightVec0_PS"].SetValue(new Vector4(0.78974f, -0.34353f, 0.50823f, 0.00f));

                //Parameters["g_vLightVec0_PS"].SetValue(lightVector);
                //g_vLightVec0_PS.SetValue(Vector4.Transform(LightVector, GameBase.ActiveCameraBase.TestViewMatrix * GameBase.ActiveCameraBase.ProjectionMatrix));
                g_vLightVec0_PS.SetValue(GameBase.LightSource.Direction);


            }
            //ps_versatile_cb
            if (shaderProgram.UsePixelShaderBuffer[4])
            {
                //Light animation parameters
                /*
                Parameters["g_vColor0_PS"].SetValue(new Vector4(0f, 2f, 0f, 10f)); //Light RGBA
                Parameters["g_vParam4_PS"].SetValue(new Vector4(0.0f, 3.5f, 0f, 0f)); //Inner Radius, Outer Radius

                Parameters["g_vParam3_PS"].SetValue(new Vector4(1, 1f, 0f, 0f)); //Ambient light strength (always 1?), Anim Light strength (A / 2, clamped to 0 - 10 range)
                Parameters["g_vParam5_PS"].SetValue(new Vector4(-2.5f, 1.0f, 0, 1.00f)); //Light source position (relative to World)
                */

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

            //cb_ps_bool
            if (shaderProgram.UsePixelShaderBuffer[8])
            {
                /*
                Parameters["g_bFog_PS"].SetValue(false); //Enables fog (on stages)
                Parameters["g_bOutputGlareMRT_PS"].SetValue(true);
                Parameters["ps_bool_padding0"].SetValue(true);
                Parameters["ps_bool_padding2"].SetValue(true);
                Parameters["ps_bool_padding3"].SetValue(true);
                Parameters["g_bDepthTex_PS"].SetValue(true);
                Parameters["g_bShadowPCF1_PS"].SetValue(true);
                Parameters["g_bShadowPCF4_PS"].SetValue(true);
                Parameters["g_bShadowPCF8_PS"].SetValue(true);
                Parameters["g_bShadowPCF16_PS"].SetValue(true);
                Parameters["g_bShadowPCF24_PS"].SetValue(true);
                */
                //Parameters["g_bFog_PS"].SetValue(false); //Enables fog (on stages)

            }

            //Set global samplers/textures
            foreach (var shader in _shaders)
                shader.SetGlobalSamplers();
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
        public BlendState GetBlendState()
        {
            BlendState blendState = new BlendState();

            switch (MatParam.AlphaBlendType)
            {
                case 0: //Normal
                    //blendState = BlendState.NonPremultiplied;
                    blendState.IndependentBlendEnable = true;
                    blendState.AlphaBlendFunction = BlendFunction.Add;
                    blendState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                    blendState.AlphaSourceBlend = Blend.SourceAlpha;
                    blendState.ColorBlendFunction = BlendFunction.Add;
                    blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;
                    blendState.ColorSourceBlend = Blend.SourceAlpha;
                    blendState.ColorWriteChannels = ColorWriteChannels.All;
                    blendState.MultiSampleMask = 1;
                    break;
                case 1: //Additive
                    blendState = BlendState.Additive;
                    break;
                case 2: //Sub
                    blendState.AlphaBlendFunction = BlendFunction.ReverseSubtract;
                    blendState.ColorSourceBlend = Blend.InverseDestinationColor;
                    blendState.ColorDestinationBlend = Blend.One;
                    blendState.AlphaSourceBlend = Blend.Zero;
                    blendState.AlphaDestinationBlend = Blend.One;
                    break;
            }

            return blendState;
        }

        public DepthStencilState GetDepthState()
        {
            DepthStencilState depth = new DepthStencilState();

            if (MatParam.AlphaBlend == 1 || MatParam.AlphaSortMask == 1 || MatParam.ZTestMask == 1)
                depth.DepthBufferEnable = true;

            if (MatParam.AlphaBlend == 1)
                depth.DepthBufferWriteEnable = false;


            return depth;
        }

        public RasterizerState GetRasterizerState()
        {
            return new RasterizerState
            {
                CullMode = MatParam.BackFace > 0 || MatParam.TwoSidedRender > 0 ? CullMode.None : CullMode.CullCounterClockwiseFace,
                FillMode = SceneManager.WireframeModeCharacters ? FillMode.WireFrame : FillMode.Solid,
            };
        }

        public static Xv2ShaderEffect CreateDefaultMaterial(GameBase gameBase)
        {
            EmmMaterial mat = new EmmMaterial();
            mat.ShaderProgram = "TOON_UNIF_STAIN3_DFD";
            mat.Name = "default";
            mat.DecompileParameters();

            return new Xv2ShaderEffect(mat, gameBase);
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
            if (shaderProgram.UseVertexShaderBuffer[3])
            {
                g_TexScroll0_VS.SetVector4(materialAnimationNode.TexScrl[0]);
                g_TexScroll1_VS.SetVector4(materialAnimationNode.TexScrl[1]);
                g_MaterialCol0_VS.SetVector4(materialAnimationNode.MatCol[0]);
                g_MaterialCol1_VS.SetVector4(materialAnimationNode.MatCol[1]);
                g_MaterialCol2_VS.SetVector4(materialAnimationNode.MatCol[2]);
                g_MaterialCol3_VS.SetVector4(materialAnimationNode.MatCol[3]);
            }

            if (shaderProgram.UsePixelShaderBuffer[2])
            {
                g_TexScroll0_PS.SetVector4(materialAnimationNode.TexScrl[0]);
                g_TexScroll1_PS.SetVector4(materialAnimationNode.TexScrl[1]);
                g_MaterialCol0_PS.SetVector4(materialAnimationNode.MatCol[0]);
                g_MaterialCol1_PS.SetVector4(materialAnimationNode.MatCol[1]);
                g_MaterialCol2_PS.SetVector4(materialAnimationNode.MatCol[2]);
                g_MaterialCol3_PS.SetVector4(materialAnimationNode.MatCol[3]);
            }
        }

        //Animation
        public void SetSkinningMatrices(Matrix[] matrices)
        {
            if (shaderProgram.UseVertexShaderBuffer[8])
            {
                g_bSkinning_VS.SetValue(true);
            }

            if (shaderProgram.UseVertexShaderBuffer[4])
            {
                g_mMatrixPalette_VS.SetValue(matrices);
            }
        }

        public void SetEyeMovement(float[] uvScroll)
        {
            if (shaderProgram.UsePixelShaderBuffer[2])
            {
                g_TexScroll0_PS.SetVector4(uvScroll);
            }

            if (shaderProgram.UseVertexShaderBuffer[3])
            {
                g_TexScroll0_VS.SetVector4(uvScroll);
            }
        }
    }
}
