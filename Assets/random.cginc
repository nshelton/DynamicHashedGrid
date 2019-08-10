////////////Some Random stuff////////////////////////// 
uint rng_state;
float nrand(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}
uint rand_xorshift()
{
	// Xorshift algorithm from George Marsaglia's paper
    rng_state ^= (rng_state << 13);
    rng_state ^= (rng_state >> 17);
    rng_state ^= (rng_state << 5);
    return rng_state;
} 
float3 RandomVector(int seed)
{
    rng_state = seed;
    float f0 = float(rand_xorshift()) * (1.0 / 4294967296.0) - 0.5;
    float f1 = float(rand_xorshift()) * (1.0 / 4294967296.0) - 0.5;
    float f2 = float(rand_xorshift()) * (1.0 / 4294967296.0) - 0.5;
    float3 normalF3 = normalize(float3(f0, f1, f2)) * 0.8f;
    normalF3 *= float(rand_xorshift()) * (1.0 / 4294967296.0);

    return float3(normalF3.x, normalF3.y, normalF3.z + 3.0);
}
