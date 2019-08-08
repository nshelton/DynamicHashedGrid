
float3 pal(in float t, in float3 a, in float3 b, in float3 c, in float3 d)
{
    return a + b * cos(6.28318 * (c * t + d));
}

float3 magma_quintic(float x)
{
    x = clamp(x, 0.0, 1.0);
    float4 x1 = float4(1.0, x, x * x, x * x * x); // 1 x x2 x3
    float4 x2 = x1 * x1.w * x; // x4 x5 x6 x7
    return float3(
				dot(x1.xyzw, float4(-0.023226960, +1.087154378, -0.109964741, +6.333665763)) + dot(x2.xy, float2(-11.640596589, +5.337625354)),
				dot(x1.xyzw, float4(+0.010680993, +0.176613780, +1.638227448, -6.743522237)) + dot(x2.xy, float2(+11.426396979, -5.523236379)),
				dot(x1.xyzw, float4(-0.008260782, +2.244286052, +3.005587601, -24.279769818)) + dot(x2.xy, float2(+32.484310068, -12.688259703)));
}

float3 plasma_quintic(float x)
{
    x = clamp(x, 0.0, 1.0);
    float4 x1 = float4(1.0, x, x * x, x * x * x); // 1 x x2 x3
    float4 x2 = x1 * x1.w * x; // x4 x5 x6 x7
    return float3(
				dot(x1.xyzw, float4(+0.063861086, +1.992659096, -1.023901152, -0.490832805)) + dot(x2.xy, float2(+1.308442123, -0.914547012)),
				dot(x1.xyzw, float4(+0.049718590, -0.791144343, +2.892305078, +0.811726816)) + dot(x2.xy, float2(-4.686502417, +2.717794514)),
				dot(x1.xyzw, float4(+0.513275779, +1.580255060, -5.164414457, +4.559573646)) + dot(x2.xy, float2(-1.916810682, +0.570638854)));
}

float3 palette(float t, float paletteIndex)
{
    float3 color = pal(t, float3(0.5, 0.5, 0.5), float3(0.5, 0.5, 0.5), float3(1.0, 1.0, 1.0), float3(0.0, 0.33, 0.67));
    if (paletteIndex > 1.0)
        color = pal(t, float3(0.5, 0.5, 0.5), float3(0.5, 0.5, 0.5), float3(1.0, 1.0, 1.0), float3(0.0, 0.10, 0.20));
    if (paletteIndex > 2.0)
        color = pal(t, float3(0.5, 0.5, 0.5), float3(0.5, 0.5, 0.5), float3(1.0, 1.0, 1.0), float3(0.3, 0.20, 0.20));
    if (paletteIndex > 3.0)
        color = pal(t, float3(0.5, 0.5, 0.5), float3(0.5, 0.5, 0.5), float3(1.0, 1.0, 0.5), float3(0.8, 0.90, 0.30));
    if (paletteIndex > 4.0)
        color = pal(t, float3(0.5, 0.5, 0.5), float3(0.5, 0.5, 0.5), float3(1.0, 0.7, 0.4), float3(0.0, 0.15, 0.20));
    if (paletteIndex > 5.0)
        color = pal(t, float3(0.5, 0.5, 0.5), float3(0.5, 0.5, 0.5), float3(2.0, 1.0, 0.0), float3(0.5, 0.20, 0.25));
    if (paletteIndex > 5.0)
        color = pal(t, float3(0.5, 0.5, 0.5), float3(0.5, 0.5, 0.5), float3(2.0, 1.0, 0.0), float3(0.5, 0.20, 0.25));
    return color;
}