cbuffer vs_post_process
{
    float4x4 g_mWVP_VS;
    float4 afUV_TexCoordOffsetV16[16];
}

cbuffer ps_post_process
{
    float4 afRGBA_Modulate[32];
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
    float2 UV0 : TEXCOORD0; // UV coordinates for Texture0
    float2 UV1 : TEXCOORD1; // UV coordinates for Texture1
    float2 UV2 : TEXCOORD2;
    float2 UV3 : TEXCOORD3;
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

    output.UV0.xy = afUV_TexCoordOffsetV16[0].xy + v.uv.xy;
    output.UV1.xy = afUV_TexCoordOffsetV16[1].xy + v.uv.xy;
    output.UV2.xy = afUV_TexCoordOffsetV16[2].xy + v.uv.xy;
    output.UV3.xy = afUV_TexCoordOffsetV16[3].xy + v.uv.xy;

    return output;
}


float4 PSMain(PSInput input) : SV_TARGET0
{
  float4 r0,r1;
  float4 outputColor;

  r0.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV1.xy, int2(0, 0)).xyzw;
  r0.xyzw = afRGBA_Modulate[1].xyzw * r0.xyzw;
  r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV0.xy, int2(0, 0)).xyzw;
  r0.xyzw = r1.xyzw * afRGBA_Modulate[0].xyzw + r0.xyzw;
  r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV2.xy, int2(0, 0)).xyzw;
  r0.xyzw = r1.xyzw * afRGBA_Modulate[2].xyzw + r0.xyzw;
  r1.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, input.UV3.xy, int2(0, 0)).xyzw;
  outputColor.xyzw = r1.xyzw * afRGBA_Modulate[3].xyzw + r0.xyzw;

  return outputColor;
}