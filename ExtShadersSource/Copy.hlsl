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
};

PSInput VSMain(VSInput v)
{
    PSInput output;

    float4 vertex = float4(v.vertex, 1);

    output.position.x = dot(vertex, g_mWVP_VS._m00_m10_m20_m30);
    output.position.y = dot(vertex, g_mWVP_VS._m01_m11_m21_m31);
    output.position.z = dot(vertex, g_mWVP_VS._m02_m12_m22_m32);
    output.position.w = dot(vertex, g_mWVP_VS._m03_m13_m23_m33);

    output.UV0 = afUV_TexCoordOffsetV16[0].xy + v.uv;

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET0
{
  float4 color = afRGBA_Modulate[0] * atex2D_Texture.Sample(asamp2D_Texture_s, input.UV0);
  
  return color;
}