cbuffer vs_post_process
{
    float4x4 g_mWVP_VS;
    float4 afUV_TexCoordOffsetV16[16];
}

cbuffer ps_post_process
{
    float4 afRGBA_Offset[16];
    float4 afRGBA_Modulate[32];
    float4 afUV_TexCoordOffsetP32[96];
    float4 fParam_DitherOffsetScale = {0.00392156886,0.00392156886,-0.00392156886,0};
}

struct VSInput
{
    float3 vertex : POSITION0;
    float2 UV0 : TEXCOORD0;
    float2 UV1 : TEXCOORD1;
    float2 UV2 : TEXCOORD2;
    float2 UV3 : TEXCOORD3;
    float2 UV4 : TEXCOORD4;
};

struct PSInput
{
    float4 position : SV_POSITION0;
    float2 UV0 : TEXCOORD0;
    float2 UV1 : TEXCOORD1;
    float2 UV2 : TEXCOORD2;
    float2 UV3 : TEXCOORD3;
    float2 UV4 : TEXCOORD4;
};

Texture2D Textures[4] : register(t0);
SamplerState Samplers[4] : register(s0);

PSInput VSMain(VSInput v)
{
    PSInput input;

    input.UV0.xy = v.UV0.xy;
    input.UV1.xy = v.UV1.xy;
    input.UV2.xy = v.UV2.xy;
    input.UV3.xy = v.UV3.xy;
    input.UV4.xy = v.UV4.xy;

    float4 vertex;
    vertex.xyz = v.vertex.xyz;
    vertex.w = 1;

    input.position.x = dot(vertex.xyzw, g_mWVP_VS._m00_m10_m20_m30);
    input.position.y = dot(vertex.xyzw, g_mWVP_VS._m01_m11_m21_m31);
    input.position.z = dot(vertex.xyzw, g_mWVP_VS._m02_m12_m22_m32);
    input.position.w = dot(vertex.xyzw, g_mWVP_VS._m03_m13_m23_m33);

    return input;
}

float4 PSMain(PSInput input) : SV_Target
{
    // Step 0: Sample texture 2
    float4 tex2Sample = Textures[2].Sample(Samplers[2], input.UV2);

    // Step 1-2: Apply modulation using offsets
    float3 temp1 = tex2Sample.rgb * afRGBA_Offset[0].x + afRGBA_Offset[0].y;
    float3 temp2 = tex2Sample.rgb * afRGBA_Offset[2].x + afRGBA_Offset[2].y;

    // Step 3-4: Compute dot product for grayscale and adjust
    float gray1 = dot(temp1, float3(0.3333, 0.3333, 0.3333));
    temp1 = temp1 - gray1;
    temp1 = saturate(afRGBA_Offset[1].x * temp1 + gray1);

    // Step 5-6: Apply further modulation
    temp1 = temp1 * afRGBA_Offset[0].z + afRGBA_Offset[0].w;

    // Repeat steps for the second temp
    float gray2 = dot(temp2, float3(0.3333, 0.3333, 0.3333));
    temp2 = temp2 - gray2;
    temp2 = afRGBA_Offset[3].x * temp2 + gray2;
    temp2 = temp2 * afRGBA_Offset[2].z + afRGBA_Offset[2].w;

    // Step 11-13: Sample texture 1, combine with temp1 and temp2
    float4 tex1Sample = Textures[1].Sample(Samplers[1], input.UV1);
    temp1 = tex1Sample.rgb * temp2 + temp1;
    temp1 = saturate(temp1 * afRGBA_Modulate[0].y);

    // Step 14-16: Sample texture 0 and apply modulation
    float4 tex0Sample = Textures[0].Sample(Samplers[0], input.UV0);
    temp2 = temp2 * tex0Sample.rgb;
    float alpha = dot(tex0Sample, afRGBA_Modulate[1]);
    float finalAlpha = saturate(alpha + afRGBA_Modulate[0].w);

    // Step 18-22: Clamp values and combine with temp1
    temp2 = saturate(temp2 * afRGBA_Modulate[0].x);
    float3 complement = 1.0 - temp2;
    temp2 = temp2 + complement * temp1;

    // Step 23-26: Apply dithering
    temp1 = temp2 * fParam_DitherOffsetScale.y;
    float4 tex3Sample = Textures[3].Sample(Samplers[3], input.UV3);
    temp1 = temp1 + tex3Sample.x * fParam_DitherOffsetScale.x;
    temp1 = temp1 + fParam_DitherOffsetScale.z;

    // Step 27: Final output
    float3 finalColor = temp2 + temp1;

    return float4(finalColor, finalAlpha);
}