cbuffer vs_post_process
{
    float4x4 g_mWVP_VS;
    float4 afUV_TexCoordOffsetV16[16];
}

cbuffer ps_post_process
{
    float4 afRGBA_Modulate[32];
    float4 afUV_TexCoordOffsetP32[96];
}

SamplerState asamp2D_Texture_s : register(s0);
Texture2D<float4> atex2D_Texture : register(t0);

struct VSInput
{
    float3 vertex : POSITION0;
    float2 uv : TEXCOORD0;
};

struct PSInput
{
    float4 position : SV_POSITION0;
    float2 UV0 : TEXCOORD0;
    float2 UV1 : TEXCOORD1;
    float2 UV2 : TEXCOORD2;
    float2 UV3 : TEXCOORD3;
    float2 UV4 : TEXCOORD4;
    float2 UV5 : TEXCOORD5;
    float2 UV6 : TEXCOORD6;
    float2 UV7 : TEXCOORD7;
};

PSInput VSMain(VSInput v)
{
    PSInput input;

    input.UV0.xy = afUV_TexCoordOffsetV16[0].xy + v.uv.xy;
    input.UV1.xy = afUV_TexCoordOffsetV16[1].xy + v.uv.xy;
    input.UV2.xy = afUV_TexCoordOffsetV16[2].xy + v.uv.xy;
    input.UV3.xy = afUV_TexCoordOffsetV16[3].xy + v.uv.xy;
    input.UV4.xy = afUV_TexCoordOffsetV16[4].xy + v.uv.xy;
    input.UV5.xy = afUV_TexCoordOffsetV16[5].xy + v.uv.xy;
    input.UV6.xy = afUV_TexCoordOffsetV16[6].xy + v.uv.xy;
    input.UV7.xy = afUV_TexCoordOffsetV16[7].xy + v.uv.xy;

    float4 vertex;
    vertex.xyz = v.vertex.xyz;
    vertex.w = 1;

    input.position.x = dot(vertex.xyzw, g_mWVP_VS._m00_m10_m20_m30);
    input.position.y = dot(vertex.xyzw, g_mWVP_VS._m01_m11_m21_m31);
    input.position.z = dot(vertex.xyzw, g_mWVP_VS._m02_m12_m22_m32);
    input.position.w = dot(vertex.xyzw, g_mWVP_VS._m03_m13_m23_m33);

    return input;
}


float4 PSMain(PSInput input) : SV_TARGET0
{
    float4 r0,r1;
    float4 finalColor;
    r0.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV1.xy, int2(0, 0)).xyzw;
    r0.xyzw = afRGBA_Modulate[1].xyzw * r0.xyzw;
    r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV0.xy, int2(0, 0)).xyzw;
    r0.xyzw = r1.xyzw * afRGBA_Modulate[0].xyzw + r0.xyzw;
    r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV2.xy, int2(0, 0)).xyzw;
    r0.xyzw = r1.xyzw * afRGBA_Modulate[2].xyzw + r0.xyzw;
    r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV3.xy, int2(0, 0)).xyzw;
    r0.xyzw = r1.xyzw * afRGBA_Modulate[3].xyzw + r0.xyzw;
    r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV4.xy, int2(0, 0)).xyzw;
    r0.xyzw = r1.xyzw * afRGBA_Modulate[4].xyzw + r0.xyzw;
    r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV5.xy, int2(0, 0)).xyzw;
    r0.xyzw = r1.xyzw * afRGBA_Modulate[5].xyzw + r0.xyzw;
    r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV6.xy, int2(0, 0)).xyzw;
    r0.xyzw = r1.xyzw * afRGBA_Modulate[6].xyzw + r0.xyzw;
    r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV7.xy, int2(0, 0)).xyzw;
    r0.xyzw = r1.xyzw * afRGBA_Modulate[7].xyzw + r0.xyzw;
    r1.xy = afUV_TexCoordOffsetP32[0].xy + input.UV0.xy;
    r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, r1.xy, int2(0, 0)).xyzw;
    finalColor.xyzw = r1.xyzw * afRGBA_Modulate[8].xyzw + r0.xyzw;

    return finalColor;
}