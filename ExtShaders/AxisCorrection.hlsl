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

    output.position = mul(float4(v.vertex, 1), g_mWVP_VS);
    output.UV0 = v.uv;

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET0
{
    float2 flippedUV = float2(1.0 - input.UV0.x, input.UV0.y);
    float4 color = atex2D_Texture.Sample(asamp2D_Texture_s, flippedUV);
    return color;
}