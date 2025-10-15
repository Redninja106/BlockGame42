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
                continue;
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