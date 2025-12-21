
// https://www.reedbeta.com/blog/hash-functions-for-gpu-rendering/
uint32_t pcg_hash(uint32_t x)
{
    x = x * 747796405u + 2891336453u;
    x = ((x >> ((x >> 28u) + 4u)) ^ x) * 277803737u;
    return (x >> 22u) ^ x;
}

// https://stackoverflow.com/questions/664014/what-integer-hash-function-are-good-that-accepts-an-integer-hash-key
uint32_t wang_hash(uint32_t x) 
{
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = (x >> 16) ^ x;
    return x;
}

float hash_detail(float3 ray_origin, float3 ray_hit)
{
    constexpr int base_lod = 4; // 2^4 (16x16 base lod)
    constexpr float lod_scale = 16.0;

    float dist = distance(
        floor(ray_origin * lod_scale) * (1.0 / lod_scale), 
        floor(ray_hit * lod_scale) * (1.0 / lod_scale)
    );

    return exp2(base_lod - (int)log2(dist * (1.0 / lod_scale) + 1));
}

void get_hash_and_checksum(float3 position, float3 normal, float detail, out uint hash, out uint checksum)
{
    int normal_hash = (int)(normal.x + 51) * 101 + (int)(normal.y + 7) * 17 + (int)(normal.z + 13);

    int3 int_position = (int3)floor(position * detail + normal * .001);

    hash = pcg_hash(int_position.x);
    checksum = wang_hash(int_position.x);

    hash = pcg_hash(hash + int_position.y);
    checksum = wang_hash(checksum + int_position.y);

    hash = pcg_hash(hash + int_position.z);
    checksum = wang_hash(checksum + int_position.z);

    hash = pcg_hash(hash + normal_hash);
    checksum = wang_hash(checksum + normal_hash);
}