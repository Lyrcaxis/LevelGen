uint2 texSize; // The size of the input texture.
uint2 rndSeed; // The seed for the random number generator.

bool isInBounds(uint2 id) { return id.x < texSize.x && id.y < texSize.y; }
float3 rand(uint2 id) { // pcg3d hash from http://www.jcgt.org/published/0009/03/02/ (slightly altered)
    uint3 v = uint3(id.xyx + uint3(rndSeed.x, rndSeed.y, rndSeed.x * rndSeed.y)) * 1664525u + 1013904223u;
    v.x += v.y*v.z; v.y += v.z*v.x; v.z += v.x*v.y;
    v ^= v >> 16u;
    v.x += v.y*v.z; v.y += v.z*v.x; v.z += v.x*v.y;
    return v / 4964138627.0; // Average return value is 0.5
}
float randBit(uint2 id, float threshold) { return rand(id).x >= threshold; }
float randBit(uint2 id) { return randBit(id, 0.5); }
float4 randBit4(uint2 id, float threshold) { float3 rnd = rand(id); return float4(rnd.x >= threshold, rnd.y >= threshold, rnd.z >= threshold, (rnd.x + rnd.y - 2 * rnd.z) >= threshold); }