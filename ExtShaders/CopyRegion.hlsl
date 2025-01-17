cbuffer vs_post_process
{
  float4x4 g_mWVP_VS;
}

Texture2D Texture0 : register(t0); // First texture
Texture2D Texture1 : register(t1); // Second texture
SamplerState Sampler0 : register(s0); // Sampler for Texture0
SamplerState Sampler1 : register(s1); // Sampler for Texture1

struct VSInput
{
    float3 vertex : POSITION0;
    float2 uv : TEXCOORD0;
    float2 uv2 : TEXCOORD1;
};

struct PSInput
{
    float4 position : SV_POSITION0;
    float2 UV0 : TEXCOORD0; // UV coordinates for Texture0
    float2 UV1 : TEXCOORD1; // UV coordinates for Texture1
};

PSInput VSMain(VSInput v)
{
    PSInput output;

    float4 vertex;
    vertex.xyz = v.vertex.xyz;
    vertex.w = 1;

    output.position.x = dot(vertex.xyzw, g_mWVP_VS._m00_m10_m20_m30);
    output.position.y = dot(vertex.xyzw, g_mWVP_VS._m01_m11_m21_m31);
    output.position.z = dot(vertex.xyzw, g_mWVP_VS._m02_m12_m22_m32);
    output.position.w = dot(vertex.xyzw, g_mWVP_VS._m03_m13_m23_m33);
    output.UV0 = v.uv;
    output.UV1 = v.uv2;

    return output;
}

float4 PSMain(PSInput input) : SV_Target
{
    // Sample both textures
    float4 color1 = Texture0.Sample(Sampler0, input.UV0);
    float4 color2 = Texture1.Sample(Sampler1, input.UV0);

    // Multiply the RGB components and use the alpha from color1
    float3 resultColor = color1.rgb * color2.rgb;
    float alpha = color1.a;

    return float4(resultColor, alpha);
}