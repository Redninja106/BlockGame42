
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

void get_hash_and_checksum(float3 position, float3 normal, out uint hash, out uint checksum)
{
    int normal_hash = (int)(normal.x + 51) * 101 + (int)(normal.y + 7) * 17 + (int)(normal.z + 13);

    int3 int_position = (int3)floor(position * 16 - normal * .5);

    hash = pcg_hash(int_position.x);
    checksum = wang_hash(int_position.x);

    hash = pcg_hash(hash + int_position.y);
    checksum = wang_hash(checksum + int_position.y);

    hash = pcg_hash(hash + int_position.z);
    checksum = wang_hash(checksum + int_position.z);

    hash = pcg_hash(hash + normal_hash);
    checksum = wang_hash(checksum + normal_hash);
}