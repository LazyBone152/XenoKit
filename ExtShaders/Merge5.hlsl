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

struct VSInput
{
    float3 vertex : POSITION;
    float2 UV : TEXCOORD0;
};

struct PSInput
{
    float2 UV : TEXCOORD0;
    float4 position : SV_Position;
};

Texture2D Textures[5] : register(t0);
SamplerState Samplers[5] : register(s0);

PSInput VSMain(VSInput v)
{
    PSInput output;

    output.UV = v.UV;

    float4 vertex;
    vertex.xyz = v.vertex.xyz;
    vertex.w = 1;

    output.position.x = dot(vertex.xyzw, g_mWVP_VS._m00_m10_m20_m30);
    output.position.y = dot(vertex.xyzw, g_mWVP_VS._m01_m11_m21_m31);
    output.position.z = dot(vertex.xyzw, g_mWVP_VS._m02_m12_m22_m32);
    output.position.w = dot(vertex.xyzw, g_mWVP_VS._m03_m13_m23_m33);

    return output;
}

float4 PSMain(PSInput input) : SV_Target
{
    float4 result = float4(0.0, 0.0, 0.0, 0.0);

    for (int i = 0; i < 5; ++i)
    {
        float4 texSample = Textures[i].Sample(Samplers[i], input.UV);
        result = mad(texSample, afRGBA_Modulate[i], result);
    }

    return result;
}