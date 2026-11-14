using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Numerics;
using XenoKit.Engine.Shader.DXBC;
using Xv2CoreLib.EMM;
using Xv2CoreLib.SDS;

namespace XenoKit.Engine.Shader
{
    public abstract class Xv2ShaderEffectBase : Effect
    {
        //Constant Buffer IDS:
        protected const int VS_MATRIX_CB = 0;
        protected const int VS_STAGE_CB = 1;
        protected const int VS_COMMON_LIGHT_CB = 2;
        protected const int VS_COMMON_MATERIAL_CB = 3;
        protected const int VS_MTXPLT_CB = 4; //can also be: vs_mtxplt_alias_sspl_cb
        protected const int VS_MTXPLT_PREV_CB = 5;
        protected const int VS_VERSATILE_CB = 6;
        protected const int CB_VS_BOOL = 8;
        protected const int PS_STAGE_CB = 0;
        protected const int PS_ALPHATEST_CB = 1;
        protected const int PS_COMMON_CB = 2;
        protected const int PS_VERSATILE_CB = 4;
        protected const int PS_USER_CB = 5;
        protected const int CB_PS_BOOL = 8;

        public ShaderProgram shaderProgram { get; protected set; }
        public EmmMaterial Material { get; protected set; }
        public DecompiledMaterial MatParam => Material?.DecompiledParameters;
        protected ShaderParameter[] SdsParameters;
        public ShaderType ShaderType { get; protected set; }
        internal Xv2Shader[] _shaders;

        protected BlendState _blendState;
        protected DepthStencilState _depthStencilState;
        protected RasterizerState _rasterizerState;
        protected RasterizerState _reflectionRasterizerState;

        //Parameters
        protected EffectParameter g_mWVP_VS;
        protected EffectParameter g_mVP_VS;
        protected EffectParameter g_mWVP_Prev_VS;
        protected EffectParameter g_mWV_VS;
        protected EffectParameter g_mWV_VS_padding;
        protected EffectParameter g_mW_VS;
        protected EffectParameter g_mWIT_VS;
        protected EffectParameter g_mWLP_SM_VS;
        protected EffectParameter g_mWLPB_SM_VS;
        protected EffectParameter g_mWLP_PM_VS;
        protected EffectParameter g_mWLPB_PM_VS;
        protected EffectParameter g_vScreen_VS;
        protected EffectParameter g_SystemTime_VS;
        protected EffectParameter g_vEyePos_VS;
        protected EffectParameter g_vLightVec0_VS;
        protected EffectParameter g_vLightDif0_VS;
        protected EffectParameter g_vLightSpc0_VS;
        protected EffectParameter g_vUserFlag0_VS;
        protected EffectParameter g_vLightVec0_PS;
        protected EffectParameter g_vLightDif0_PS;
        protected EffectParameter g_vLightSpc0_PS;
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

        protected EffectParameter g_vColor4_PS;
        protected EffectParameter g_vParam8_PS;
        protected EffectParameter g_vParam9_PS;
        protected EffectParameter g_vParam10_PS;
        protected EffectParameter g_vParam11_PS;
        protected EffectParameter g_vParam12_PS;
        protected EffectParameter g_vParam13_PS;
        protected EffectParameter g_vUserFlag1_VS;

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

        public Matrix4x4 World = Matrix4x4.Identity;
        public Matrix4x4 WV = Matrix4x4.Identity;
        public Matrix4x4 WVP = Matrix4x4.Identity;
        public Matrix4x4 PrevWVP = Matrix4x4.Identity;
        protected bool UseWV, UseWVP;
        protected bool UseLight_VS, UseLight_PS;

        private static readonly Microsoft.Xna.Framework.Vector4[] MagicReservedArray = new Microsoft.Xna.Framework.Vector4[4] { new Vector4(4, 8, 15, 16), Vector4.Zero, Vector4.Zero, Vector4.Zero };

        public Xv2ShaderEffectBase(GraphicsDevice device) : base(device) { }

        #region Init
        public void SetShaderType(ShaderType type)
        {
            if (ShaderType != type)
            {
                ShaderType = type;
                InitTechnique();
            }
        }

        public void InitTechnique()
        {
            //Only grab the shader program if this shader effect is for a material, and not a ShaderProgram itself
            if (ShaderType != ShaderType.PostFilter)
                shaderProgram = Viewport.Instance.ShaderManager.GetShaderProgram(Material.ShaderProgram);

            UseWVP = UseWV = false;

            if (shaderProgram.ShaderSource == ShaderProgramSource.Xenoverse)
            {
                //Initialize the SDS parameter array. These are the list of parameters this shader uses and should be updated OnApply
                SdsParameters = new ShaderParameter[shaderProgram.SdsEntry.Parameters.Count];

                for (int i = 0; i < shaderProgram.SdsEntry.Parameters.Count; i++)
                {
                    if (!Enum.TryParse(shaderProgram.SdsEntry.Parameters[i].Name, out ShaderParameter param))
                    {
                        param = ShaderParameter.Unknown;
                        Editor.Log.Add($"Unknown ShaderParameter: {shaderProgram.SdsEntry.Parameters[i].Name}", Editor.LogType.Debug);
                    }

                    SdsParameters[i] = param;

                    switch (param)
                    {
                        case ShaderParameter.WV:
                            UseWV = true;
                            break;
                        case ShaderParameter.WVP:
                        case ShaderParameter.WVP_Prev:
                            UseWVP = true;
                            break;
                    }
                }
            }
            else
            {
                SdsParameters = new ShaderParameter[0];
            }

            //Start load parametrs

            int paramCount = shaderProgram.VsParser.CBuffers.Sum(x => x.Variables.Length) + shaderProgram.PsParser.CBuffers.Sum(x => x.Variables.Length);

            EffectParameter[] parameters = new EffectParameter[paramCount];

            //Initialize CBs
            ReadConstantBuffers(shaderProgram);

            //Parameters
            int idx = 0;
            ReadParameters(shaderProgram.PsParser, parameters, ref idx);
            ReadParameters(shaderProgram.VsParser, parameters, ref idx);

            Parameters = new EffectParameterCollection(parameters);

            //Load shaders
            _shaders = new Xv2Shader[2];
            _shaders[0] = new Xv2Shader(GraphicsDevice, this, shaderProgram, true);
            _shaders[1] = new Xv2Shader(GraphicsDevice, this, shaderProgram, false);

            //Set initial parameters from EMM
            SetParameters();
            InitParameterCache();

            //Pass the magic numbers to the shader - this allows shaders to check if they are running in XenoKit
            //Parameters["reserved"]?.SetValue(MagicReservedArray);

            //Set pass
            EffectTechnique[] techniques = new EffectTechnique[1];
            EffectPassCollection passes = ReadPasses();
            techniques[0] = new EffectTechnique(this, shaderProgram.Name, passes, null);

            Techniques = new EffectTechniqueCollection(techniques);
            CurrentTechnique = Techniques[0];

            _depthStencilState = GetDepthState();
            _blendState = GetBlendState();
            _rasterizerState = GetRasterizerState();

            if (_reflectionRasterizerState != null)
                _reflectionRasterizerState = GetReflectionRasterizerState();
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
                            //int elementSize2 = param.VariableSize / elements;
                            int elementSize = 4 * columnCount * rowCount;
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
            ConstantBuffers = new ConstantBuffer[shaderProgram.PsParser.CBuffers.Length + shaderProgram.VsParser.CBuffers.Length];

            int idx = 0;
            int pIdx = 0;

            ReadConstantBuffersForShader(shaderProgram.PsParser, ref idx, ref pIdx);
            ReadConstantBuffersForShader(shaderProgram.VsParser, ref idx, ref pIdx);
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

            passes[0] = new EffectPass(this, name, vertexShader, pixelShader, null, null, null, null);

            return new EffectPassCollection(passes);
        }

        public int IndexOfConstantBuffer(string name)
        {
            for (int i = 0; i < ConstantBuffers.Length; i++)
            {
                if (ConstantBuffers[i]._name == name) return i;
            }

            return -1;
        }

        private void InitParameterCache()
        {
            g_mWVP_VS = Parameters["g_mWVP_VS"];
            g_mVP_VS = Parameters["g_mVP_VS"];
            g_mWVP_Prev_VS = Parameters["g_mWVP_Prev_VS"];
            g_mWV_VS = Parameters["g_mWV_VS"];
            g_mWV_VS_padding = Parameters["g_mWV_VS_padding"];
            g_mW_VS = Parameters["g_mW_VS"];
            g_mWIT_VS = Parameters["g_mWIT_VS"];
            g_mWLPB_SM_VS = Parameters["g_mWLPB_SM_VS"];
            g_mWLP_PM_VS = Parameters["g_mWLP_PM_VS"];
            g_mWLPB_PM_VS = Parameters["g_mWLPB_PM_VS"];

            g_mWLP_SM_VS = Parameters["g_mWLP_SM_VS"];
            g_vScreen_VS = Parameters["g_vScreen_VS"];
            g_SystemTime_VS = Parameters["g_SystemTime_VS"];
            g_vEyePos_VS = Parameters["g_vEyePos_VS"];
            g_vLightVec0_VS = Parameters["g_vLightVec0_VS"];
            g_vLightDif0_VS = Parameters["g_vLightDif0_VS"];
            g_vLightSpc0_VS = Parameters["g_vLightSpc0_VS"];
            g_vLightVec0_PS = Parameters["g_vLightVec0_PS"];
            g_vLightDif0_PS = Parameters["g_vLightDif0_PS"];
            g_vLightSpc0_PS = Parameters["g_vLightSpc0_PS"];

            UseLight_PS = (g_vLightVec0_PS != null || g_vLightDif0_PS != null || g_vLightSpc0_PS != null);
            UseLight_VS = (g_vLightVec0_VS != null || g_vLightDif0_VS != null || g_vLightSpc0_VS != null);

            g_vUserFlag0_VS = Parameters["g_vUserFlag0_VS"];
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

            g_vColor4_PS = Parameters["g_vColor4_PS"];
            g_vParam8_PS = Parameters["g_vParam8_PS"];
            g_vParam9_PS = Parameters["g_vParam9_PS"];
            g_vParam10_PS = Parameters["g_vParam10_PS"];
            g_vParam11_PS = Parameters["g_vParam11_PS"];
            g_vParam12_PS = Parameters["g_vParam12_PS"];
            g_vParam13_PS = Parameters["g_vParam13_PS"];
            g_vUserFlag1_VS = Parameters["g_vUserFlag1_VS"];
        }

        protected abstract void SetParameters();
        #endregion

        #region States
        public abstract BlendState GetBlendState();

        public abstract DepthStencilState GetDepthState();

        public abstract RasterizerState GetReflectionRasterizerState();

        public abstract RasterizerState GetRasterizerState();
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed && disposing)
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

            base.Dispose(disposing);
        }
    }
}
