struct Random 
{
    uint seed;

    [mutating]
    float NextFloat()
    {
        return asfloat((Next() & 0x007FFFFF) | 0x3F800000) - 1.0f;
    }

    [mutating]
    float3 NextUnitVector()
    {
        float3 result;
        [unroll]
        for (int i = 0; i < 32; i++)
        {
            result = float3(NextFloat(), NextFloat(), NextFloat());
            result = result * 2 - 1;
            if (dot(result, result) <= 1)
            {
                break;
            }
        }
        
        return normalize(result);
    }

    [mutating]
    uint Next()
    {
        uint state = seed * 747796405u + 2891336453u;
        uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
        return seed = (word >> 22u) ^ word;
    }
};

float sample_halton<int base>(uint index, float[] permutations)
{
    uint result = 0;
    uint exponent = 0;
    while (index > 0)
    {
        uint next = index / base;
        uint digit = index - next * base;
        result = result * base + permutations[digit];
        exponent++;
        index = next;
    }
    return result / pow(base, exponent);
}

constexpr float[] halton_permutations_base_2 = { 0, 1 };
constexpr float[] halton_permutations_base_3 = { 0, 2, 1 };
constexpr float[] halton_permutations_base_5 = { 0, 2, 4, 1, 3 };

float3 halton_unit_vector(uint index)
{ 
    float3 result;
    [unroll]
    for (int i = 0; i < 32; i++)
    {
        result = float3(
            sample_halton<2>(index, halton_permutations_base_2), 
            sample_halton<3>(index, halton_permutations_base_3), 
            sample_halton<5>(index, halton_permutations_base_5)
        );
        result = result * 2 - 1;
        if (dot(result, result) <= 1)
        {
            break;
        }
    }

    return normalize(result);
}