using SharpDX.Direct3D11;
using LB_Common.Utils;
using XenoKit.Engine.Shader.DXBC;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.SDS;

namespace XenoKit.Engine.Shader
{
    public enum ShaderProgramSource
    {
        Xenoverse,
        External
    }

    public class ShaderProgram
    {
        public string Name { get; private set; }
        public SDSShaderProgram SdsEntry { get; private set; }
        public VertexShader VS;
        public PixelShader PS;
        public byte[] VS_Bytecode;
        public byte[] PS_Bytecode;
        public DxbcParser VsParser;
        public DxbcParser PsParser;
        private GraphicsDevice graphicsDevice;

        //Settings
        public bool HardwareSkinning { get; private set; }
        public ShaderProgramSource ShaderSource { get; private set; }

        //Buffers
        public readonly bool[] UsePixelShaderBuffer = new bool[9];
        public readonly bool[] UseVertexShaderBuffer = new bool[9];

        public bool ShaderValidationPassed { get; private set; }

        public ShaderProgram(SDSShaderProgram shaderProgram, byte[] vsByteCode, byte[] psByteCode, bool allowHardwareSkinning, GraphicsDevice graphicsDevice)
        {
            SdsEntry = shaderProgram;
            Name = SdsEntry.Name;
            HardwareSkinning = allowHardwareSkinning;
            ShaderSource = ShaderProgramSource.Xenoverse;
            this.graphicsDevice = graphicsDevice;
            VS_Bytecode = vsByteCode;
            PS_Bytecode = psByteCode;
            CreateShaders(true);
        }

        public ShaderProgram(string name, byte[] vsByteCode, byte[] psByteCode, ShaderProgramSource source, GraphicsDevice graphicsDevice)
        {
            Name = name;
            ShaderSource = source;
            VS_Bytecode = vsByteCode;
            PS_Bytecode = psByteCode;
            this.graphicsDevice = graphicsDevice;
            CreateShaders(false);

            SdsEntry = new SDSShaderProgram()
            {
                Name = "DUMMY",
                Parameters = new System.Collections.Generic.List<Xv2CoreLib.SDS.SDSParameter>()
            };
        }

        private void CreateShaders(bool createBufferBools)
        {
            VsParser = new DxbcParser(VS_Bytecode);
            PsParser = new DxbcParser(PS_Bytecode);
            ShaderValidationPassed = ValidateShader();

            if (!ShaderValidationPassed) return;

            try
            {
                VS = new VertexShader((Device)graphicsDevice.Handle, VS_Bytecode);
                PS = new PixelShader((Device)graphicsDevice.Handle, PS_Bytecode);

                if (createBufferBools)
                {
                    //Check for buffers
                    UsePixelShaderBuffer[0] = PsParser.HasCB("ps_stage_cb");
                    UsePixelShaderBuffer[1] = PsParser.HasCB("ps_alphatest_cb");
                    UsePixelShaderBuffer[2] = PsParser.HasCB("ps_common_cb");
                    UsePixelShaderBuffer[4] = PsParser.HasCB("ps_versatile_cb");
                    UsePixelShaderBuffer[5] = PsParser.HasCB("ps_user_cb");
                    UsePixelShaderBuffer[8] = PsParser.HasCB("cb_ps_bool");

                    UseVertexShaderBuffer[0] = VsParser.HasCB("vs_matrix_cb");
                    UseVertexShaderBuffer[1] = VsParser.HasCB("vs_stage_cb");
                    UseVertexShaderBuffer[2] = VsParser.HasCB("vs_common_light_cb");
                    UseVertexShaderBuffer[3] = VsParser.HasCB("vs_common_material_cb");
                    UseVertexShaderBuffer[4] = VsParser.HasCB("vs_mtxplt_cb") || VsParser.HasCB("vs_mtxplt_alias_sspl_cb");
                    UseVertexShaderBuffer[5] = VsParser.HasCB("vs_mtxplt_prev_cb");
                    UseVertexShaderBuffer[6] = VsParser.HasCB("vs_versatile_cb");
                    UseVertexShaderBuffer[8] = VsParser.HasCB("cb_vs_bool");
                }
            }
            catch
            {
                ShaderValidationPassed = false;
            }
        }

        private bool ValidateShader()
        {
            foreach(var input in VsParser.InputSignature)
            {
                if (input.Name == "INSTDATA")
                {
                    return false;
                }

            }

            return true;
        }
    }
}
