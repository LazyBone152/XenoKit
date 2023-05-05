using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace XenoKit.Engine.Shader
{
    //Here is where all the Constant Buffers (CBs) used by DBXV2s shaders are declared.
    //The game makes use of common structs that all shaders use, even when most of the values aren't used by any given shader
    //NOT USED AT ALL BY XENOKIT! These are a remenant from an older attempt at implemting shaders and are only left here as a reference.

    //cb0
    [StructLayout(LayoutKind.Explicit, Size = 640)]
    public unsafe struct vs_matrix_cb
    {
        [FieldOffset(0)]
        public Matrix g_mWVP_VS;
        [FieldOffset(64)]
        public Matrix g_mVP_VS;
        [FieldOffset(128)]
        public Matrix g_mWVP_Prev_VS;
        [FieldOffset(192)]
        public Matrix g_mWIT_VS;
        [FieldOffset(256)]
        public Matrix g_mWLP_SM_VS;
        [FieldOffset(320)]
        public Matrix g_mWLPB_SM_VS;
        [FieldOffset(384)]
        public Matrix g_mWLP_PM_VS;
        [FieldOffset(448)]
        public Matrix g_mWLPB_PM_VS;
        [FieldOffset(512)]
        public Matrix g_mWV_VS; //This is a Matrix4x3, with a float4 following for padding. But... the padding has data in them.. so it IS a Matric4x4?
        [FieldOffset(576)]
        public Matrix g_mW_VS; //Same case as above

        /*
        [FieldOffset(560)]
        public fixed float g_mWV_VS_padding[4];
        [FieldOffset(624)]
        public fixed float g_mW_VS_padding[4];
        */
    }

    //cb1
    [StructLayout(LayoutKind.Explicit, Size = 208)]
    public unsafe struct vs_stage_cb
    {
        [FieldOffset(0)]
        public fixed float g_vAmbOcl_VS[4];
        [FieldOffset(16)]
        public fixed float g_vTexTile01_VS[4];
        [FieldOffset(32)]
        public fixed float g_vTexTile23_VS[4];
        [FieldOffset(48)]
        public fixed float g_vScreen_VS[4]; //3840.00, 2160.00, 0.10, 10106.85645 (x= screen width, y = screen height)
        [FieldOffset(64)]
        public fixed float g_vDepth_VS[4];
        [FieldOffset(80)]
        public fixed float g_ProjScale_VS[4];
        [FieldOffset(96)]
        public fixed float g_vMotionBlur_VS[4];
        [FieldOffset(112)]
        public fixed float g_vLayerCastShadow_VS[4];//5.00, 10.00, 20.00, 1000.00
        [FieldOffset(128)]
        public fixed float g_vLayerReceiveShadow_VS[4];//0.20, 0.10, 0.05, 0.001
        [FieldOffset(144)]
        public fixed float g_vColor_VS[4];
        [FieldOffset(160)]
        public fixed float g_vHeightFog_VS[4];
        [FieldOffset(176)]
        public fixed float g_vFog_VS[4];
        [FieldOffset(192)]
        public fixed float g_SystemTime_VS[4]; //X = Current running time of game (unpaused), in seconds. Not related to ElapsedTime. 
    }

    //cb3
    [StructLayout(LayoutKind.Explicit, Size = 416)]
    public unsafe struct vs_common_material_cb
    {
        [FieldOffset(0)]
        public fixed float g_vSubSurface_VS[4];
        [FieldOffset(16)]
        public fixed float g_TexScroll0_VS[4];
        [FieldOffset(32)]
        public fixed float g_TexScroll1_VS[4];
        [FieldOffset(48)]
        public fixed float g_TexScroll2_VS[4];
        [FieldOffset(64)]
        public fixed float g_TexScroll3_VS[4];
        [FieldOffset(80)]
        public fixed float g_MaterialCol0_VS[4];
        [FieldOffset(96)]
        public fixed float g_MaterialCol1_VS[4];
        [FieldOffset(112)]
        public fixed float g_MaterialCol2_VS[4];
        [FieldOffset(128)]
        public fixed float g_MaterialCol3_VS[4];
        [FieldOffset(144)]
        public fixed float g_MaterialCol4_VS[4];
        [FieldOffset(160)]
        public fixed float g_MaterialCol5_VS[4];
        [FieldOffset(176)]
        public fixed float g_MaterialCol6_VS[4];
        [FieldOffset(192)]
        public fixed float g_MaterialCol7_VS[4];
        [FieldOffset(208)]
        public fixed float g_MaterialScale0_VS[4];
        [FieldOffset(224)]
        public fixed float g_MaterialScale1_VS[4];
        [FieldOffset(240)]
        public fixed float g_MaterialOffset0_VS[4];
        [FieldOffset(256)]
        public fixed float g_MaterialOffset1_VS[4];
        [FieldOffset(272)]
        public fixed float g_AlphaFade_VS[4];
        [FieldOffset(288)]
        public fixed float g_Incidence_VS[4];
        [FieldOffset(304)]
        public fixed float g_Gradient_VS[4];
        [FieldOffset(320)]
        public fixed float g_ElapsedTime_VS[4];
        [FieldOffset(336)]
        public fixed float g_GlareCoeff_VS[4];
        [FieldOffset(352)]
        public fixed float g_Reflection_VS[4];
        [FieldOffset(368)]
        public fixed float g_Brush_VS[4];
        [FieldOffset(384)]
        public fixed float g_EdgeColor_VS[4];
        [FieldOffset(400)]
        public fixed float g_vLodViewport_VS[4];
    }

    //cb2
    [StructLayout(LayoutKind.Explicit, Size = 320)]
    public unsafe struct vs_common_light_cb
    {
        [FieldOffset(0)]
        public fixed float g_vAmbUni_VS[4];
        [FieldOffset(16)]
        public fixed float g_vRimColor_VS[4];
        [FieldOffset(32)]
        public fixed float g_vHemiA_VS[4];
        [FieldOffset(48)]
        public fixed float g_vHemiB_VS[4];
        [FieldOffset(64)]
        public fixed float g_vHemiC_VS[4];
        [FieldOffset(80)]
        public fixed float g_vLightVec0_VS[4];
        [FieldOffset(96)]
        public fixed float g_vLightDif0_VS[4];
        [FieldOffset(112)]
        public fixed float g_vLightSpc0_VS[4];
        [FieldOffset(128)]
        public fixed float g_vLightVec1_VS[4];
        [FieldOffset(144)]
        public fixed float g_vLightDif1_VS[4];
        [FieldOffset(160)]
        public fixed float g_vLightSpc1_VS[4];
        [FieldOffset(176)]
        public fixed float g_vLightVec2_VS[4];
        [FieldOffset(192)]
        public fixed float g_vLightDif2_VS[4];
        [FieldOffset(208)]
        public fixed float g_vLightSpc2_VS[4];
        [FieldOffset(224)]
        public fixed float g_vLightVec3_VS[4];
        [FieldOffset(240)]
        public fixed float g_vLightDif3_VS[4];
        [FieldOffset(256)]
        public fixed float g_vLightSpc3_VS[4];
        [FieldOffset(272)]
        public fixed float g_vEyePos_VS[4];
        [FieldOffset(288)]
        public fixed float g_vSpecular_VS[4];
        [FieldOffset(304)]
        public fixed float g_vRim_VS[4];

    }

    //Could also be vs_mtxplt_alias_sspl_cb in some shaders
    //cb4
    [StructLayout(LayoutKind.Explicit, Size = 1152)]
    public unsafe struct vs_mtxplt_cb
    {
        //Matrix4x3[24]. Contains an array of world position bones to be skinned. (Likely wont need this, as animation is handled on the application side)
        [FieldOffset(0)]
        public fixed float g_mMatrixPalette_VS[288];

    }

    //cb5
    [StructLayout(LayoutKind.Explicit, Size = 1152)]
    public unsafe struct vs_mtxplt_prev_cb
    {
        [FieldOffset(0)]
        public fixed float g_mMatrixPalettePrev_VS[288];

    }

    //cb8
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct cb_vs_bool
    {
        //Bools in C# are only 1 byte, while in HLSL they are 4. So Int32s are the best type to use.
        [FieldOffset(0)]
        public int g_bSkinning_VS;
        [FieldOffset(4)]
        public int g_bNearBlur_VS;
        [FieldOffset(8)]
        public int g_bFixProjection_VS;
        [FieldOffset(12)]
        public int g_bAmbOcl_VS;
        [FieldOffset(16)]
        public int g_bLodViewport_VS;
        [FieldOffset(20)]
        public int vs_bool_padding0;
        [FieldOffset(24)]
        public int vs_bool_padding1;
        [FieldOffset(28)]
        public int vs_bool_padding2;
        [FieldOffset(32)]
        public int g_bVersatile0_VS;
        [FieldOffset(36)]
        public int g_bVersatile1_VS;
        [FieldOffset(40)]
        public int g_bVersatile2_VS;
        [FieldOffset(44)]
        public int g_bVersatile3_VS;
        [FieldOffset(48)]
        public int g_bUserFlag0_VS;
        [FieldOffset(52)]
        public int g_bUserFlag1_VS;
        [FieldOffset(56)]
        public int g_bUserFlag2_VS;
        [FieldOffset(60)]
        public int g_bUserFlag3_VS;
    }

    //cb0
    [StructLayout(LayoutKind.Explicit, Size = 240)]
    public unsafe struct ps_stage_cb
    {
        [FieldOffset(0)]
        public fixed float g_vShadowMap_PS[4]; //8192.00, 0.00012, 0.0014, 0.75 (X = Resolution (2048 = low, 4096 = medium, 8192 = high))
        [FieldOffset(16)]
        public fixed float g_vEdge_PS[4];
        [FieldOffset(32)]
        public fixed float g_vGlare_PS[4];
        [FieldOffset(48)]
        public fixed float g_vLerp_PS[4];
        [FieldOffset(64)]
        public fixed float g_vOffset_PS[4];
        [FieldOffset(80)]
        public fixed float g_vScale_PS[4];
        [FieldOffset(96)]
        public fixed float g_vShadowColor_PS[4]; //0.40, 0.45, 0.40, 0.00
        [FieldOffset(112)]
        public fixed float g_vTone_PS[4]; //0.50, 0.50, 0.50, 3.75
        [FieldOffset(128)]
        public fixed float g_vFogMultiColor_PS[4];//g_vFogMultiColor_PS 0.10119, 0.19048, 0.28571, 1.00 float4
        [FieldOffset(144)]
        public fixed float g_vFogAddColor_PS[4];//g_vFogAddColor_PS 0.14881, 0.15476, 0.06548, 1.00 float4
        [FieldOffset(160)]
        public fixed float g_vFadeMulti_PS[4];
        [FieldOffset(176)]
        public fixed float g_vFadeRim_PS[4];
        [FieldOffset(192)]
        public fixed float g_vFadeAdd_PS[4];
        [FieldOffset(208)]
        public fixed float g_vAltFadeMulti_PS[4];//g_vAltFadeMulti_PS 1.00, 1.00, 1.00, 1.00 float4
        [FieldOffset(224)]
        public fixed float g_vAltFadeAdd_PS[4];
    }

    //cb1
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct ps_alphatest_cb
    {
        [FieldOffset(0)]
        public fixed float g_vAlphaTest_PS[4];//0.00392, 0.00, 0.00, 0.00
    }

    //cb8
    [StructLayout(LayoutKind.Explicit, Size = 52)]
    public struct cb_ps_bool
    {
        [FieldOffset(0)]
        public int ps_bool_padding0;
        [FieldOffset(4)]
        public int ps_bool_padding1;
        [FieldOffset(8)]
        public int g_bFog_PS;
        [FieldOffset(12)]
        public int g_bOutputGlareMRT_PS;
        [FieldOffset(16)]
        public int g_bOutputDepthMRT_PS;
        [FieldOffset(20)]
        public int ps_bool_padding2;
        [FieldOffset(24)]
        public int ps_bool_padding3;
        [FieldOffset(28)]
        public int g_bDepthTex_PS;
        [FieldOffset(32)]
        public int g_bShadowPCF1_PS;
        [FieldOffset(36)]
        public int g_bShadowPCF4_PS;
        [FieldOffset(40)]
        public int g_bShadowPCF8_PS;
        [FieldOffset(44)]
        public int g_bShadowPCF16_PS;
        [FieldOffset(48)]
        public int g_bShadowPCF24_PS;
    }

    //cb2
    [StructLayout(LayoutKind.Explicit, Size = 736)]
    public unsafe struct ps_common_cb
    {
        [FieldOffset(0)]
        public fixed float g_vAmbUni_PS[4];// 0.82569, 0.69725, 0.57339, 1.00
        [FieldOffset(16)]
        public fixed float g_vRimColor_PS[4]; // 0.77876, 0.90265, 1.00, 1.00
        [FieldOffset(32)]
        public fixed float g_vHemiA_PS[4]; //0.00, 0.00, 0.00, 1.00
        [FieldOffset(48)]
        public fixed float g_vHemiB_PS[4]; //0.00, 0.00, 0.00, 1.00
        [FieldOffset(64)]
        public fixed float g_vHemiC_PS[4]; //0.90, 0.90, 0.90, 1.00
        [FieldOffset(80)]
        public fixed float g_vLightVec0_PS[4]; //-0.86543, -0.38418, 0.32161, 0.00
        [FieldOffset(96)]
        public fixed float g_vLightDif0_PS[4]; //1.00, 1.00, 1.00, 1.00
        [FieldOffset(112)]
        public fixed float g_vLightSpc0_PS[4];//1.00, 1.00, 1.00, 1.00
        [FieldOffset(128)]
        public fixed float g_vLightVec1_PS[4]; //0.00, 1.00, 0.00, 0.00
        [FieldOffset(144)]
        public fixed float g_vLightDif1_PS[4];
        [FieldOffset(160)]
        public fixed float g_vLightSpc1_PS[4];
        [FieldOffset(176)]
        public fixed float g_vLightVec2_PS[4];
        [FieldOffset(192)]
        public fixed float g_vLightDif2_PS[4];
        [FieldOffset(208)]
        public fixed float g_vLightSpc2_PS[4];
        [FieldOffset(224)]
        public fixed float g_vLightVec3_PS[4];
        [FieldOffset(240)]
        public fixed float g_vLightDif3_PS[4];
        [FieldOffset(256)]
        public fixed float g_vLightSpc3_PS[4];
        [FieldOffset(272)]
        public fixed float g_vEyePos_PS[4];
        [FieldOffset(288)]
        public fixed float g_vSpecular_PS[4];
        [FieldOffset(304)]
        public fixed float g_vRim_PS[4];
        [FieldOffset(320)]
        public fixed float g_vSubSurface_PS[4];
        [FieldOffset(336)]
        public fixed float g_TexScroll0_PS[4];
        [FieldOffset(352)]
        public fixed float g_TexScroll1_PS[4];
        [FieldOffset(368)]
        public fixed float g_TexScroll2_PS[4];
        [FieldOffset(384)]
        public fixed float g_TexScroll3_PS[4];
        [FieldOffset(400)]
        public fixed float g_MaterialCol0_PS[4];
        [FieldOffset(416)]
        public fixed float g_MaterialCol1_PS[4];
        [FieldOffset(432)]
        public fixed float g_MaterialCol2_PS[4];
        [FieldOffset(448)]
        public fixed float g_MaterialCol3_PS[4];
        [FieldOffset(464)]
        public fixed float g_MaterialCol4_PS[4];
        [FieldOffset(480)]
        public fixed float g_MaterialCol5_PS[4];
        [FieldOffset(496)]
        public fixed float g_MaterialCol6_PS[4];
        [FieldOffset(512)]
        public fixed float g_MaterialCol7_PS[4];
        [FieldOffset(528)]
        public fixed float g_MaterialScale0_PS[4];
        [FieldOffset(544)]
        public fixed float g_MaterialScale1_PS[4];
        [FieldOffset(560)]
        public fixed float g_MaterialOffset0_PS[4];
        [FieldOffset(576)]
        public fixed float g_MaterialOffset1_PS[4];
        [FieldOffset(592)]
        public fixed float g_AlphaFade_PS[4];
        [FieldOffset(608)]
        public fixed float g_Incidence_PS[4];
        [FieldOffset(624)]
        public fixed float g_Gradient_PS[4];
        [FieldOffset(640)]
        public fixed float g_ElapsedTime_PS[4];//0.00, 0.00, 0.00, 0.00
        [FieldOffset(656)]
        public fixed float g_GlareCoeff_PS[4];
        [FieldOffset(672)]
        public fixed float g_Reflection_PS[4];
        [FieldOffset(688)]
        public fixed float g_Brush_PS[4];
        [FieldOffset(704)]
        public fixed float g_EdgeColor_PS[4];
        [FieldOffset(720)]
        public fixed float g_vLodViewport_PS[4]; //1.00, 1.00, 1.00, 1.00
    }

    //cb6
    [StructLayout(LayoutKind.Explicit, Size = 160)]
    public unsafe struct vs_versatile_cb
    {
        [FieldOffset(0)]
        public fixed float g_vParam0_VS[4]; // 30.00, 30.00, 30.00, 30.00
        [FieldOffset(16)]
        public fixed float g_vParam1_VS[4];//-16.4644, -21.62025, 0.025, 0.025
        [FieldOffset(32)]
        public fixed float g_vUserFlag0_VS[4];
        [FieldOffset(48)]
        public fixed float g_vUserFlag1_VS[4];
        [FieldOffset(64)]
        public fixed float g_vUserFlag2_VS[4]; //2.24208E-44, 2.24208E-44, 0.00, 0.00
        [FieldOffset(80)]
        public fixed float g_vUserFlag3_VS[4];
        [FieldOffset(96)]
        public fixed float reserved[16];
    }

    //cb4
    [StructLayout(LayoutKind.Explicit, Size = 576)]
    public unsafe struct ps_versatile_cb
    {
        [FieldOffset(0)]
        public fixed float g_vColor0_PS[4];
        [FieldOffset(16)]
        public fixed float g_vColor1_PS[4];
        [FieldOffset(32)]
        public fixed float g_vColor2_PS[4];
        [FieldOffset(48)]
        public fixed float g_vColor3_PS[4];
        [FieldOffset(64)]
        public fixed float g_vColor4_PS[4];
        [FieldOffset(80)]
        public fixed float g_vColor5_PS[4];
        [FieldOffset(96)]
        public fixed float g_vColor6_PS[4];
        [FieldOffset(112)]
        public fixed float g_vColor7_PS[4];
        [FieldOffset(128)]
        public fixed float g_vColor8_PS[4];
        [FieldOffset(144)]
        public fixed float g_vColor9_PS[4];
        [FieldOffset(160)]
        public fixed float g_vColor10_PS[4];
        [FieldOffset(176)]
        public fixed float g_vColor11_PS[4];
        [FieldOffset(192)]
        public fixed float g_vColor12_PS[4];
        [FieldOffset(208)]
        public fixed float g_vColor13_PS[4];
        [FieldOffset(224)]
        public fixed float g_vColor14_PS[4];
        [FieldOffset(240)]
        public fixed float g_vColor15_PS[4];
        [FieldOffset(256)]
        public fixed float g_vParam0_PS[4];
        [FieldOffset(272)]
        public fixed float g_vParam1_PS[4];
        [FieldOffset(288)]
        public fixed float g_vParam2_PS[4];
        [FieldOffset(304)]
        public fixed float g_vParam3_PS[4];
        [FieldOffset(320)]
        public fixed float g_vParam4_PS[4];
        [FieldOffset(336)]
        public fixed float g_vParam5_PS[4];
        [FieldOffset(352)]
        public fixed float g_vParam6_PS[4];
        [FieldOffset(368)]
        public fixed float g_vParam7_PS[4];
        [FieldOffset(384)]
        public fixed float g_vParam8_PS[4];
        [FieldOffset(400)]
        public fixed float g_vParam9_PS[4];
        [FieldOffset(416)]
        public fixed float g_vParam10_PS[4];
        [FieldOffset(432)]
        public fixed float g_vParam11_PS[4];
        [FieldOffset(448)]
        public fixed float g_vParam12_PS[4];
        [FieldOffset(464)]
        public fixed float g_vParam13_PS[4];
        [FieldOffset(480)]
        public fixed float g_vParam14_PS[4];
        [FieldOffset(496)]
        public fixed float g_vParam15_PS[4];
        [FieldOffset(512)]
        public fixed float g_vThreshold0_PS[4];
        [FieldOffset(528)]
        public fixed float g_vThreshold1_PS[4];
        [FieldOffset(544)]
        public fixed float g_vThreshold2_PS[4];
        [FieldOffset(560)]
        public fixed float g_vThreshold3_PS[4];
    }


    //cb5
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct ps_user_cb
    {
        [FieldOffset(0)]
        public fixed float g_vShadowParam_PS[4];
    }

    //Neither SharpDX or MonoGame provide a Matrix4x3, so we must create our own.
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public unsafe struct Matrix4x3
    {
        [FieldOffset(0)]
        public fixed float Row1[4];
        [FieldOffset(16)]
        public fixed float Row2[4];
        [FieldOffset(32)]
        public fixed float Row3[4];

        public Matrix4x3(Matrix mat4x4)
        {
            Row1[0] = mat4x4.M11;
            Row1[1] = mat4x4.M12;
            Row1[2] = mat4x4.M13;
            Row1[3] = mat4x4.M14;
            Row2[0] = mat4x4.M21;
            Row2[1] = mat4x4.M22;
            Row2[2] = mat4x4.M23;
            Row2[3] = mat4x4.M24;
            Row3[0] = mat4x4.M31;
            Row3[1] = mat4x4.M32;
            Row3[2] = mat4x4.M33;
            Row3[3] = mat4x4.M34;
        }

        public float[] GetValues()
        {
            return new float[12] { Row1[0], Row1[1] , Row1[2] , Row1[3],
                                   Row2[0], Row2[1] , Row2[2] , Row2[3],
                                   Row3[0], Row3[1] , Row3[2] , Row3[3] };
        }
    }
}
