using SharpDX.Direct3D11;
using LB_Common.Utils;
using XenoKit.Engine.Shader.DXBC;
using Microsoft.Xna.Framework.Graphics;

namespace XenoKit.Engine.Shader
{
    public class ShaderProgram
    {
        public string Name => SdsEntry.Name;
        public Xv2CoreLib.SDS.SDSShaderProgram SdsEntry { get; private set; }
        public VertexShader VS;
        public PixelShader PS;
        public byte[] VS_Bytecode;
        public byte[] PS_Bytecode;
        public DxbcParser VsParser;
        public DxbcParser PsParser;

        //Settings
        public bool HardwareSkinning { get; private set; }

        //Buffers
        public readonly bool[] UsePixelShaderBuffer = new bool[9];
        public readonly bool[] UseVertexShaderBuffer = new bool[9];

        public ShaderProgram(Xv2CoreLib.SDS.SDSShaderProgram shaderProgram, byte[] vsByteCode, byte[] psByteCode, bool allowHardwareSkinning, GraphicsDevice graphicsDevice)
        {
            SdsEntry = shaderProgram;
            HardwareSkinning = allowHardwareSkinning;

            VS_Bytecode = vsByteCode;
            PS_Bytecode = psByteCode;

            VsParser = new DxbcParser(VS_Bytecode);
            PsParser = new DxbcParser(PS_Bytecode);

            VS = new VertexShader((Device)graphicsDevice.Handle, vsByteCode);
            PS = new PixelShader((Device)graphicsDevice.Handle, psByteCode);

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
}
