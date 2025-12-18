#include "sdl.slang"
#include "random.slang"
#include "hash.slang"
#include "util.slang"

#define COUNT_RAY_STEPS 1

struct MaterialData
{
    int transmissions[6];
    int emissions[6];
};

// RW Resources
SDL_CS_RW_RESOURCE(0, RWStructuredBuffer<uint> tileChecksums);
SDL_CS_RW_RESOURCE(1, RWStructuredBuffer<uint4> tileIrradiances);
SDL_CS_RW_RESOURCE(2, RWStructuredBuffer<uint4> tileReflections);

// Samplers/Sampled Textures
SDL_CS_SAMPLER(0, SamplerState sampler);
SDL_CS_SAMPLER(1, SamplerState sampler1);
SDL_CS_SAMPLER(2, SamplerState sampler2);
SDL_CS_RESOURCE(0, Texture2DArray<float4> albedoTextureArray);
SDL_CS_RESOURCE(1, Texture2DArray<float4> normalTextureArray);
SDL_CS_RESOURCE(2, Texture2DArray<float4> specularTextureArray);

SDL_CS_RESOURCE(3, Texture2D<float4> positionTarget);
SDL_CS_RESOURCE(4, Texture2D<float4> albedoTarget);
SDL_CS_RESOURCE(5, Texture2D<float4> normalTarget);
SDL_CS_RESOURCE(6, Texture2D<float4> specularTarget);
// SDL_CS_RESOURCE(3, Texture2D<float4> prevPositionTarget);

SDL_CS_RESOURCE(7, Texture3D<uint2> blockMasks);
SDL_CS_RESOURCE(8, Texture3D<uint> materialIds);

// SDL_CS_RESOURCE(9, StructuredBuffer<uint4> prevTileReflections);

// SDL_CS_RESOURCE(8, StructuredBuffer<MaterialData> materials)

struct Uniforms
{
    float4 sundir;
    float4 cameraPosition;
    int3 blockMasksOffset;
    uint tileOffset;
    uint tileCount;
    uint phaseCount;
    uint ticks;
};

SDL_CS_UNIFORM_BUFFER(0, Uniforms uniforms);

static Random random;
static uint haltonIndex;

#if COUNT_RAY_STEPS
static uint ray_step_count = 0;
#endif

[shader("compute")]
[numthreads(16, 16, 1)]
void main(uint3 dispatchThreadID: SV_DispatchThreadID)
{
    float4 pos_and_norm = positionTarget[dispatchThreadID.xy];
    float4 encoded_normal = normalTarget[dispatchThreadID.xy];
    float4 specular = specularTarget[dispatchThreadID.xy];
    // float4 prev_pos = prevPositionTarget[dispatchThreadID.xy];
    // float4 albedo = specularTarget[dispatchThreadID.xy];

    float3 position = pos_and_norm.xyz;
    float3 original_normal = unpackSnorm4x8ToFloat(asuint(pos_and_norm.w)).xyz;

    float3 normal = original_normal;// decode_normal(encoded_normal.xy);

    random.seed = uniforms.ticks * 0x3f05aa5f +
                  (dispatchThreadID.x + 0x8523) * 0xb7e35a7d + 
                  (dispatchThreadID.y + 0x7423) * 0x3f05aa5f;
    random.Next();
    
    // haltonIndex = dispatchThreadID.x + 
    //               dispatchThreadID.y + 
    //               uniforms.ticks;

    const int samples = 4;

    float3 totalColor = float3(0);
    for (int i = 0; i < samples; i++)
    {
        Ray sample_ray;
        sample_ray.origin = position + original_normal * .0001;
        sample_ray.direction = normalize(normal + random.NextUnitVector());
        sample_ray.inverseDirection = 1 / sample_ray.direction;
        sample_ray.length = 64;

        totalColor += ray_color(sample_ray, normal);
    }
    totalColor *= (1.0 / samples);

    uint detail = hash_detail(position, uniforms.cameraPosition.xyz);
    uint idx = find_or_insert(position, original_normal, detail);
    InterlockedAdd(tileIrradiances[idx].r, int(255 * totalColor.r));
    InterlockedAdd(tileIrradiances[idx].g, int(255 * totalColor.g));
    InterlockedAdd(tileIrradiances[idx].b, int(255 * totalColor.b));
    InterlockedAdd(tileIrradiances[idx].a, 1);

    float reflection = specular.g <= 229 ? specular.g : 0;
    if (reflection > 0.1)
    {
        const int reflect_ray_count = 1;

        float3 reflect_color = 0;
        for (int i = 0; i < reflect_ray_count; i++) 
        {
            Ray reflection_ray;
            reflection_ray.origin = position + normal * .0001;
            float roughness = pow(1.0 - specular.r, 2.0);
            float3 viewDir = normalize(position - uniforms.cameraPosition.xyz);
            reflection_ray.direction = reflect(viewDir, normal) + random.NextUnitVector() * roughness;
            reflection_ray.inverseDirection = 1 / reflection_ray.direction;
            reflection_ray.length = 64;

            reflect_color = reflection_color(reflection_ray);
        }

        // float3 motion = position - prev_pos.xyz;
        // float prev_idx = find(0, position - motion * .5, original_normal, detail);

        InterlockedAdd(tileReflections[idx - uniforms.tileOffset].r, int(255 * reflect_color.r));
        InterlockedAdd(tileReflections[idx - uniforms.tileOffset].g, int(255 * reflect_color.g));
        InterlockedAdd(tileReflections[idx - uniforms.tileOffset].b, int(255 * reflect_color.b));
        InterlockedAdd(tileReflections[idx - uniforms.tileOffset].a, reflect_ray_count);
    }
}

static const float3 TURBO[16] = {
    float3(0.18995, 0.07176, 0.23217),
    float3(0.25107, 0.25237, 0.63374),
    float3(0.27628, 0.42118, 0.89123),
    float3(0.25862, 0.57230, 0.97711),
    float3(0.21603, 0.70670, 0.90068),
    float3(0.17895, 0.82095, 0.74684),
    float3(0.27059, 0.90909, 0.51912),
    float3(0.46920, 0.96773, 0.32103),
    float3(0.67579, 0.98821, 0.21008),
    float3(0.85144, 0.94188, 0.18409),
    float3(0.96359, 0.84317, 0.23270),
    float3(0.99675, 0.70259, 0.34699),
    float3(0.95547, 0.53393, 0.47156),
    float3(0.85907, 0.35018, 0.58910),
    float3(0.72161, 0.19110, 0.63536),
    float3(0.47960, 0.01583, 0.57066)
}; 

float3 SampleTurbo(float t)
{
    t = saturate(t);
    float x = t * 15.0;
    int i = (int)x;
    float f = frac(x);
    return lerp(TURBO[i], TURBO[min(i + 1, 15)], f);
}

uint find_or_insert(float3 position, float3 normal, float detail)
{
    const int max_search = 32;

    uint hash, checksum;
    get_hash_and_checksum(position, normal, detail, hash, checksum);

    uint searchIndex = hash % uniforms.tileCount;
    for (int i = 0; i < max_search; i++)
    {
        uint original;
        InterlockedCompareExchange(tileChecksums[uniforms.tileOffset + searchIndex], 0, checksum, original);
        if (original == 0 || original == checksum)
        {
            return uniforms.tileOffset + searchIndex;
        }

        searchIndex = pcg_hash(searchIndex) % uniforms.tileCount;
    }

    return 0xFFFFFFFF;
}

float3 ray_color(Ray ray, float3 normal)
{
    const int MaxBounces = 3;
    
    float hitT;
    float3 hitNormal;
    int3 hitVoxel;

    float3 sky_light = 0;
    float3 albedo[MaxBounces];
    float4 specular[MaxBounces];
    for (int i = 0; i < MaxBounces; i++)
    {
        albedo[i] = 0;
        specular[i] = 0;
    }


    for (int i = 0; i < MaxBounces; i++)
    {
        if (dda(ray, hitT, hitNormal, hitVoxel))
        {
            uint texId = materialIds[hitVoxel + uniforms.blockMasksOffset];
            // MaterialData material = materials[materialIds[hitVoxel + uniforms.blockMasksOffset]];
            // return hitNormal * .5 + .5;
            float3 hitPos = ray.origin + ray.direction * hitT;

            float2 uv = calc_face_uv(frac(hitPos), hitNormal);
            float3 uvw = float3(uv, texId);

            albedo[i] = albedoTextureArray.SampleLevel(sampler, uvw, 0).xyz;
            specular[i] = specularTextureArray.SampleLevel(sampler, uvw, 0);
            
            float3x3 tbn = calc_face_tbn_matrix(hitNormal);
            float4 sampledNormal = normalTextureArray.SampleLevel(sampler, uvw, 0);
            float3 n = decode_normal(sampledNormal.xy);
            float3 worldNormal = normalize(mul(tbn, n));
            // transmission[i] = materialTextures.SampleLevel(sampler, float3(uv, material.transmissions[0]), 0).xyz;

            ray.origin = hitPos + hitNormal * 0.001;
            ray.direction = normalize(worldNormal + random.NextUnitVector());
            ray.inverseDirection = 1 / ray.direction;
        }
        else
        {
            const float skystrength = .1;

            // sky emission
            sky_light += skystrength; // + 0.5 * float3(.392f, .584f, .929f);
            break;
        }
    }

    // sun emission
    const float sunfuzz = 0.005;
    const float sunstrength = .9;

    Ray sunRay;
    sunRay.origin = ray.origin;
    sunRay.direction = normalize(uniforms.sundir.xyz);
    sunRay.inverseDirection = 1.0 / sunRay.direction;
    sunRay.length = ray.length;

    if (!dda(sunRay, hitT, hitNormal, hitVoxel))
    {
        float sunbrightness = dot(sunRay.direction, normal) * dot(sunRay.direction, normalize(uniforms.sundir.xyz));
        sky_light += sunstrength * max(0, sunbrightness);
    }

    constexpr float emissionStrength = 2;

    float3 color = float3(0, 0, 0);
    for (int j = MaxBounces - 1; j >= 0; j--)
    {
        float emission = specular[j].w;
        if (emission == 1) 
        {
            emission = 0;
        }

        color = albedo[j] * (color + emission * emissionStrength);
    }
    return color + sky_light;
}

float3 reflection_color(Ray ray)
{
    float hitT;
    float3 hitNormal;
    int3 hitVoxel;

    float3 hitPos;
    if (!dda(ray, hitT, hitNormal, hitVoxel))
    {
        const float skystrength = .15;
        return skystrength;
    }

    hitPos = ray.at(hitT);

        // uint idx = find(uniforms.tileOffset / uniforms.tileCount, hitPos, hitNormal);
        // if (idx != 0xFFFFFFFF)
        // {
        //     uint4 packed_irradiance = tileIrradiances[idx];
        //     if (packed_irradiance.w != 0)
        //     {
        //         return (float3(packed_irradiance.xyz) / 255.0) / packed_irradiance.w;
        //     }
        // }

    // load material data at hit position
    uint material = materialIds[hitVoxel + uniforms.blockMasksOffset];
    float2 uv = calc_face_uv(frac(hitPos), hitNormal);
    float4 albedo = albedoTextureArray.SampleLevel(sampler, float3(uv, material), 0);
    float4 normal = normalTextureArray.SampleLevel(sampler, float3(uv, material), 0);
    float4 specular = specularTextureArray.SampleLevel(sampler, float3(uv, material), 0);

    float emission = specular.a;
    if (emission == 1) emission = 0;

    // float3 transmission = materialTextures.SampleLevel(sampler, float3(uv, material.transmissions[0]), 0).xyz;
    // float4 emission_and_reflectiveness = materialTextures.SampleLevel(sampler, float3(uv, material.emissions[0]), 0);
    // float3 emission = emission_and_reflectiveness.xyz;
    // float reflectiveness = emission_and_reflectiveness.w;

    // emissions[order] = emission;
    // transmissions[order] = transmission;
    // reflecivenesses[order] = reflectiveness;

    // hit is not reflective (stop reflecting!)
    // if (reflectiveness == 0)
    // {
    //     break;
    // }

    // update ray for next reflection
    // ray.direction = reflect(normalize(hitPos - ray.origin), hitNormal);
    // ray.inverseDirection = 1 / ray.direction;
    // ray.origin = hitPos + hitNormal * .0001;
    // ray.length = 64;
    
    ray.origin = hitPos + hitNormal * 0.0001;
    ray.direction = normalize(hitNormal + random.NextUnitVector());
    ray.inverseDirection = 1 / ray.direction;
    float3 color = ray_color(ray, hitNormal);

    uint new_idx = find_or_insert(hitPos, hitNormal, 16);
    InterlockedAdd(tileIrradiances[new_idx].r, int(255 * color.r));
    InterlockedAdd(tileIrradiances[new_idx].g, int(255 * color.g));
    InterlockedAdd(tileIrradiances[new_idx].b, int(255 * color.b));
    InterlockedAdd(tileIrradiances[new_idx].a, 1);

    float4 total_irradiance = 0;
    for (int i = 0; i < uniforms.phaseCount; i++)
    {
        uint idx = find(i, hitPos, hitNormal, 16);

        if (idx == 0xFFFFFFFF)
        {
            // emit green - not found / more than 32 hash conflicts
            continue;
        }

        uint4 packed_irradiance = tileIrradiances[idx];
        float3 irradiance = float3(packed_irradiance.xyz) / 255.0;
        uint accumulatedCount = packed_irradiance.w;
        if (accumulatedCount == 0)
        {
            // emit red - no illumination samples
            continue;
        }

        total_irradiance += float4(irradiance, float(accumulatedCount));
    }

    float3 reflection_color = (total_irradiance.xyz / total_irradiance.w);

    return (reflection_color + emission) * albedo.xyz;
}

uint find(uint phase, float3 position, float3 normal, float detail)
{
    uint hash, checksum;
    get_hash_and_checksum(position, normal, detail, hash, checksum);

    uint searchIndex = hash % uniforms.tileCount;
    for (int i = 0; i < 32; i++)
    {
        uint searchIndexChecksum = tileChecksums[phase * uniforms.tileCount + searchIndex];

        if (searchIndexChecksum == checksum)
        {
            return phase * uniforms.tileCount + searchIndex;
        }

        if (searchIndexChecksum == 0)
        {
            return 0xFFFFFFFF;
        }

        searchIndex = pcg_hash(searchIndex) % uniforms.tileCount;
    }

    return 0xFFFFFFFF;
}

float2 calc_face_uv(float3 hitPos, float3 hitNormal)
{
    // x!=0 (east/west)     -> u=z v=y
    // y!=0 (up/down)       -> u=x v=z
    // z!=0 (north/south)   -> u=x v=y
    
    int3 n = sign(hitNormal);
    float u = select(n.x != 0, -hitPos.z * n.x, hitPos.x * select(n.y != 0, n.y, n.z));
    float v = select(n.y != 0, hitPos.z * n.y, -hitPos.y);
    return frac(float2(u, v));
}

struct Box
{
    float3 min;
    float3 max;
};

struct Ray
{
    float3 origin;
    float3 direction;
    float3 inverseDirection;
    float length;

    float3 at(float t)
    {
        return origin + direction * t;
    }
};

uint64_t get_ray_mask(Ray ray, float t_entrance, float3 t_delta, int3 step)
{
    float3 pos = ray.at(t_entrance);
    int3 voxel = int3(frac(pos)*4);

    float3 t = .25 * t_delta * select(ray.direction > 0, 1 - frac(pos*4), frac(pos*4));

    uint64_t result = 0;
    bool3 voxelIncr = false;
    for (int i = 0; i < 10; i++)
    {
        if (any(voxel < 0 || voxel >= 4))
        {
            break;
        }

        result |= 1 << (voxel.y * 16 + voxel.z * 4 + voxel.x);
        
        voxelIncr = t.xyz <= t.yxx && t.xyz <= t.zzy;
        t = float3(voxelIncr) * t_delta + t;
        voxel = voxelIncr * step + voxel;
    }
    return result;
}

bool hit_ray_mask(Ray ray, uint64_t mask, int3 v, float t_entrance, float3 delta_t, int3 step, out float outT, out float3 outNormal)
{
    delta_t *= .25;

    float3 pos = ray.at(t_entrance + 0.00001);
    int3 voxel = int3(floor((pos - v) * 4));

    float3 t = delta_t * select(ray.direction > 0, 1 - frac(pos * 4), frac(pos * 4));

    bool3 voxelIncr = false;
    for (int i = 0; i < 10; i++)
    {
        if (any(voxel < 0 || voxel >= 4))
        {
            return false;
        }

        if (((mask >> (voxel.x + voxel.z * 4 + voxel.y * 16)) & 1) != 0)
        {
            outT = min3(t.x, t.y, t.z);
            outNormal = voxelIncr * -step;
            return true;
        }

        voxelIncr = t.xyz <= t.yxx && t.xyz <= t.zzy;
        t = float3(voxelIncr) * delta_t + t;
        voxel = voxelIncr * step + voxel;
    }
    return false;
}

bool dda(Ray ray, out float outT, out float3 outNormal, out int3 outVoxel)
{
    if (all(ray.direction == 0)) 
    {
        return false;
    }

    int3 step = sign(ray.direction);
    float3 delta_t = abs(ray.inverseDirection);
    float3 t = delta_t * select(ray.direction > 0, 1 - frac(ray.origin), frac(ray.origin));
    int3 voxel = int3(floor(ray.origin));

    bool3 voxelIncr = false;
    for (int i = 0; i < 64; i++)
    {
        uint64_t mask = __asuint64(blockMasks[voxel + uniforms.blockMasksOffset]);

#if COUNT_RAY_STEPS
        ray_step_count++;
#endif
        
        if (mask != 0)
        {
            float3 prev_t_3 = t - float3(voxelIncr) * delta_t;
            float prev_t = min3(prev_t_3.x, prev_t_3.y, prev_t_3.z);

            if (mask == uint64_t::maxValue)
            {
                outT = prev_t;
                outNormal = voxelIncr * -step;
                outVoxel = voxel;
                return true;
            }
            else if (hit_ray_mask(ray, mask, voxel, select(all(voxelIncr == 0), 0, prev_t), delta_t, step, outT, outNormal))
            {
                outVoxel = voxel;
                return true;
            }
        }

        voxelIncr = t.xyz <= t.yxx && t.xyz <= t.zzy;
        t = float3(voxelIncr) * delta_t + t;
        voxel = voxelIncr * step + voxel;
    }

    return false;
}

bool dda_lod(Ray ray, out float outT, out float3 outNormal, out int3 outVoxel)
{
    if (all(ray.direction == 0))
    {
        return false;
    }

    int3 step = sign(ray.direction);
    float3 delta_t = abs(ray.inverseDirection);
    float3 t = delta_t * select(ray.direction > 0, 1 - frac(ray.origin), frac(ray.origin));
    int3 voxel = int3(floor(ray.origin));

    int lod = 0;
    bool3 voxelIncr = false;
    for (int i = 0; i < 64; i++)
    {
        uint64_t mask = __asuint64(blockMasks[voxel + uniforms.blockMasksOffset]);
        if (mask != 0)
        {
            float3 prev_t_3 = t - float3(voxelIncr) * delta_t;
            float prev_t = min3(prev_t_3.x, prev_t_3.y, prev_t_3.z);

            if (mask == uint64_t::maxValue)
            {
                outT = prev_t;
                outNormal = voxelIncr * -step;
                outVoxel = voxel;
                return true;
            }
            else if (hit_ray_mask(ray, mask, voxel, select(all(voxelIncr == 0), 0, prev_t), delta_t, step, outT, outNormal))
            {
                outVoxel = voxel;
                return true;
            }
        }

        voxelIncr = t.xyz <= t.yxx && t.xyz <= t.zzy;
        t = float3(voxelIncr) * delta_t + t;
        voxel = voxelIncr * step + voxel;
    }

    return false;
}
