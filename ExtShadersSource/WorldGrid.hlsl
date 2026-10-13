cbuffer vs_parameters
{
    float4x4 g_mV_VS;
    float4x4 g_mP_VS;
    float3 position;
}

cbuffer ps_parameters
{
    float4 gridColor;
    float near;
    float far;
    float supersampleFactor;
}

struct VSInput
{
    float3 vertex : POSITION0;
};

struct PSInput
{
    float4 position : SV_POSITION0;
    float3 nearPoint : TEXCOORD0;
    float3 farPoint : TEXCOORD1;
    float4x4 view : TEXCOORD2;
    float4x4 proj : TEXCOORD6;
};

float4x4 inverse(float4x4 m) 
{
    float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
    float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
    float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
    float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];

    float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
    float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
    float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
    float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

    float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
    float idet = 1.0f / det;

    float4x4 ret;

    ret[0][0] = t11 * idet;
    ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
    ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
    ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;

    ret[1][0] = t12 * idet;
    ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
    ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
    ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;

    ret[2][0] = t13 * idet;
    ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
    ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
    ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;

    ret[3][0] = t14 * idet;
    ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
    ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
    ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

    return ret;
}

float3 UnprojectPoint(float x, float y, float z, float4x4 view, float4x4 projection) 
{
    float4x4 viewInv = inverse(view);
    float4x4 projInv = inverse(projection);
    float4 unprojectedPoint = mul(float4(x, y, z, 1.0), mul(projInv, viewInv));
    return unprojectedPoint.xyz / unprojectedPoint.w;
}

float4 grid(float3 fragPos3D, float scale, out float2 drawAxis) 
{
    float2 coord = fragPos3D.xz * scale;
    float2 derivative = fwidth(coord);
    float2 grid = abs(frac(coord - 0.5) - 0.5) / derivative;
    float _line = min(grid.x, grid.y);
    float minimumz = min(derivative.y, 1);
    float minimumx = min(derivative.x, 1);
    float4 color = float4(gridColor.rgb, 1.0 - min(_line / supersampleFactor, 1.0));
    
    //color.rgb = lerp(float3(0,0,0), float3(1,0,0), gridColor.a);

    // z axis
    if(fragPos3D.x > -0.1 * minimumx && fragPos3D.x < 0.1 * minimumx)
    {
        drawAxis.y = 1;
    }
    // x axis
    if(fragPos3D.z > -0.1 * minimumz && fragPos3D.z < 0.1 * minimumz)
    {
        drawAxis.x = 1;
    }
    return color;
}

float computeLinearDepth(float3 pos, float near, float far, float4x4 view, float4x4 proj) 
{
    float4 clip_space_pos = mul(float4(pos.xyz, 1.0), mul(view, proj));
    float clip_space_depth = (clip_space_pos.z / clip_space_pos.w) * 2.0 - 1.0; // put back between -1 and 1
    float linearDepth = (2.0 * near * far) / (far + near - clip_space_depth * (far - near)); // get linear value between 0.01 and 100
    return linearDepth / far; // normalize
}

float computeDepth(float3 pos, float4x4 view, float4x4 proj) 
{
    float4 clip_space_pos = mul(float4(pos.xyz, 1.0), mul(view, proj));
    return (clip_space_pos.z / clip_space_pos.w);
}

PSInput VSMain(VSInput v)
{
    PSInput output;

    output.nearPoint = UnprojectPoint(v.vertex.x, v.vertex.y, 0.0, g_mV_VS, g_mP_VS).xyz;
    output.farPoint = UnprojectPoint(v.vertex.x, v.vertex.y, 1.0, g_mV_VS, g_mP_VS).xyz;
    output.position = float4(v.vertex, 1.0);
    output.view = g_mV_VS;
    output.proj = g_mP_VS;

    return output;
}

void PSMain(PSInput input, out float4 color : SV_TARGET, out float depth : SV_DEPTH) 
{
    float t = -input.nearPoint.y / (input.farPoint.y - input.nearPoint.y);
    float3 fragPos3D = input.nearPoint + t * (input.farPoint - input.nearPoint);

    depth = computeDepth(fragPos3D, input.view, input.proj);

    float linearDepth = computeLinearDepth(fragPos3D, near, far, input.view, input.proj);
    float fading = max(0, (0.5 - linearDepth));

    float2 drawAxis = float2(0,0);
    float2 drawAxisDummy = float2(0,0);

    color = (grid(fragPos3D, 10, drawAxis) + grid(fragPos3D, 1, drawAxisDummy)) * float(t > 0); // adding multiple resolution for the grid
    color.a *= fading;
    color *= 0.75;

    //Draw axis lines
    color.rgb = lerp(color.rgb, float3(1,0,0), drawAxis.x); //X
    color.rgb = lerp(color.rgb, float3(0,0,1), drawAxis.y); //Z
}