cbuffer vs_post_process
{
  float4x4 g_mWVP_VS;
}

cbuffer ps_post_process
{
    float4 afRGBA_Offset[16];
    float4 fParam_HDRFormatFactor_LOGRGB;
    float4 afUV_TexCoordOffsetV16[16];
    float4 afUV_TexCoordOffsetP32[96];
    float4 fParam_DitherOffsetScale = {0.00392156886,0.00392156886,-0.00392156886,0};
    float4 afRGBA_Modulate[32];
}

SamplerState asamp2D_Texture_s : register(s0);
Texture2D<float4> atex2D_Texture : register(t0);


//  declarations
#define cmp -

void VSMain( 
  float3 v0 : POSITION0,
  float2 v1 : TEXCOORD0,
  out float2 o0 : TEXCOORD0,
  out float2 o1 : TEXCOORD1,
  out float4 o2 : SV_POSITION0)
{
  float4 r0;
  uint4 bitmask, uiDest;
  float4 fDest;

  r0.xyz = v0.xyz;
  r0.w = 1;
  o2.x = dot(r0.xyzw, g_mWVP_VS._m00_m10_m20_m30);
  o2.y = dot(r0.xyzw, g_mWVP_VS._m01_m11_m21_m31);
  o2.z = dot(r0.xyzw, g_mWVP_VS._m02_m12_m22_m32);
  o2.w = dot(r0.xyzw, g_mWVP_VS._m03_m13_m23_m33);
  o0.xy = v1.xy;
  o1 = v1;
  return;
}

void PSMain(
  float2 v0 : TEXCOORD0,
  out float4 o0 : SV_TARGET0)
{
  float4 r0;
  uint4 bitmask, uiDest;
  float4 fDest;

  r0.xyzw = atex2D_Texture.Sample(asamp2D_Texture_s, v0.xy, int2(0, 0)).xyzw;
  r0.xyzw = afRGBA_Modulate[0].xyzw * r0.xyzw;
  r0.xyz = fParam_HDRFormatFactor_LOGRGB.xxx * r0.xyz;
  o0.w = r0.w;
  r0.xyz = float3(1.44269502,1.44269502,1.44269502) * r0.xyz;
  r0.xyz = exp2(r0.xyz);
  r0.xyz = float3(-1,-1,-1) + r0.xyz;
  o0.xyz = fParam_HDRFormatFactor_LOGRGB.www * r0.xyz;
  return;
}