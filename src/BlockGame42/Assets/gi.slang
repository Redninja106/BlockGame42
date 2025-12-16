#include "sdl.slang"
#include "random.slang"
#include "hash.slang"
#include "util.slang"

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

SDL_CS_RESOURCE(7, Texture3D<uint2> blockMasks);
SDL_CS_RESOURCE(8, Texture3D<uint> materialIds);

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


[shader("compute")]
[numthreads(16, 16, 1)]
void main(uint3 dispatchThreadID: SV_DispatchThreadID)
{
    float4 pos_and_norm = positionTarget[dispatchThreadID.xy];
    float4 encoded_normal = normalTarget[dispatchThreadID.xy];
    float4 specular = specularTarget[dispatchThreadID.xy];
    // float4 albedo = specularTarget[dispatchThreadID.xy];

    float3 position = pos_and_norm.xyz;
    float3 original_normal = unpackSnorm4x8ToFloat(asuint(pos_and_norm.w)).xyz;

    float3 normal = original_normal;// decode_normal(encoded_normal.xy);

    random.seed = uniforms.ticks * 0x3f05aa5f +
                  (dispatchThreadID.x + 0x8523) * 0xb7e35a7d + 
                  (dispatchThreadID.y + 0x7423) * 0x3f05aa5f;
    random.Next();

    const int samples = 4;

    // const float3 reflectiveness = 1;
    // float3 reflection_color = 0;
    // float3 reflect_dir = reflect(normalize(uniforms.cameraPosition.xyz - position), normal);
    // Ray reflect_ray = Ray(position, reflect_dir, 1 / reflect_dir, 64);
    // 
    // float hitT;
    // float3 hitNormal;
    // int3 hitVoxel;
    // if (dda(reflect_ray, hitT, hitNormal, hitVoxel))
    // {
    //     MaterialData material = materials[materialIds[hitVoxel + uniforms.blockMasksOffset]];
    //     float2 uv = calc_face_uv(reflect_ray.at(hitT), hitNormal);
    //     reflection_color = materialTextures[material.transmissions[0]].xyz;
    // }
    // else 
    // {
    //     reflection_color = .25;
    // }

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
    // totalColor = (1 - reflectiveness) * totalColor + reflectiveness * reflection_color;
    //totalColor = original_normal * .5 + .5;

    uint idx = find_or_insert(position, original_normal, hash_detail(position, uniforms.cameraPosition.xyz));
    InterlockedAdd(tileIrradiances[idx].r, int(255 * totalColor.r));
    InterlockedAdd(tileIrradiances[idx].g, int(255 * totalColor.g));
    InterlockedAdd(tileIrradiances[idx].b, int(255 * totalColor.b));
    InterlockedAdd(tileIrradiances[idx].a, 1);

    // MaterialData material = materials[materialIds[(int3)floor(position) + uniforms.blockMasksOffset]];
    // float2 uv = calc_face_uv(frac(position), normal);

    // float4 sampledNormal = normalTextureArray.SampleLevel(sampler, texCoord.xyz, 0);
    // float4 specular = specularTextureArray.SampleLevel(sampler, texCoord.xyz, 0);

    float reflection = specular.g <= 229 ? specular.g : 0;

    // float reflection = albedoTextureArray.SampleLevel(sampler, texCoord.xyw, 0).w;

    if (reflection > 0)
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
        InterlockedAdd(tileReflections[idx - uniforms.tileOffset].r, int(255 * reflect_color.r));
        InterlockedAdd(tileReflections[idx - uniforms.tileOffset].g, int(255 * reflect_color.g));
        InterlockedAdd(tileReflections[idx - uniforms.tileOffset].b, int(255 * reflect_color.b));
        InterlockedAdd(tileReflections[idx - uniforms.tileOffset].a, reflect_ray_count);
    }
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
            specular[i].w += skystrength; // + 0.5 * float3(.392f, .584f, .929f);
            albedo[i] = float3(1);
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
        //specular[0].w += sunstrength * max(0, sunbrightness);
    }

    float3 color = float3(0, 0, 0);
    for (int j = MaxBounces - 1; j >= 0; j--)
    {
        float emission = specular[j].w;
        if (emission == 1) 
        {
            emission = 0;
        }

        color = albedo[j] * (color + emission);
    }
    return color;
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

static const Box boxes[] = 
{
    Box(float3(0.0f, 0.0f, 0.0f), float3(0.5f, 0.5f, 0.5f)),
    Box(float3(0.5f, 0.0f, 0.0f), float3(1.0f, 0.5f, 0.5f)),
    Box(float3(0.0f, 0.0f, 0.5f), float3(0.5f, 0.5f, 1.0f)),
    Box(float3(0.5f, 0.0f, 0.5f), float3(1.0f, 0.5f, 1.0f)),
    Box(float3(0.0f, 0.5f, 0.0f), float3(0.5f, 1.0f, 0.5f)),
    Box(float3(0.5f, 0.5f, 0.0f), float3(1.0f, 1.0f, 0.5f)),
    Box(float3(0.0f, 0.5f, 0.5f), float3(0.5f, 1.0f, 1.0f)),
    Box(float3(0.5f, 0.5f, 0.5f), float3(1.0f, 1.0f, 1.0f)),
};

bool BoxRaycast(Box box, Ray ray, out float t, out float3 normal)
{
    float t1 = (box.min.x - ray.origin.x) * ray.inverseDirection.x;
    float t2 = (box.max.x - ray.origin.x) * ray.inverseDirection.x;
    float t3 = (box.min.y - ray.origin.y) * ray.inverseDirection.y;
    float t4 = (box.max.y - ray.origin.y) * ray.inverseDirection.y;
    float t5 = (box.min.z - ray.origin.z) * ray.inverseDirection.z;
    float t6 = (box.max.z - ray.origin.z) * ray.inverseDirection.z;

    float tNear = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    float tFar = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

    if (tNear <= tFar && tFar > 0 && tNear < ray.length)
    {
        t = tNear < 0 ? tFar : tNear;
        normal = 0;

        // if (t == t1)
        //     normal = float3(-1, 0, 0);
        // else if (t == t2)
        //     normal = float3(1, 0, 0);
        // else if (t == t3)
        //     normal = float3(0, -1, 0);
        // else if (t == t4)
        //     normal = float3(0, 1, 0);
        // else if (t == t5)
        //     normal = float3(0, 0, -1);
        // else if (t == t6)
        //     normal = float3(0, 0, 1);
        // else
        //     normal = float3(0, 0, 0); // huh?

        return true;
    }

    return false;
}

// bool raycast(Ray ray, out float t, out float3 normal, out int3 voxel)
// {
//     voxel = int3(floor(ray.origin)) + uniforms. blockMasksOffset;
//     int3 step = int3(sign(ray.direction));
// 
//     if (all(step == 0))
//     {
//         t = 0;
//         normal = 0;
//         voxel = 0;
//         return false;
//     }
// 
//     float3 start = ray.at(0);
//     float3 end = ray.at(ray.length);
// 
//     float3 d = end - start;
//     float3 tDelta = ray.inverseDirection;
//     float3 tMax = tDelta * select(step > 0, 1 - frac(start), frac(start));
// 
//     bool3 voxelIncr = false;
//     int steps = 50;
//     while (bool(--steps))
//     {
//         uint2 block_mask = blockMasks.Load(int4(voxel, 0));
//         uint64_t mask = block_mask.x << 32 | block_mask.y;
// 
//         if (mask == 0xFF)
//         {
//            tMax -= float3(voxelIncr) * tDelta;
//            t = min3(tMax.x, tMax.y, tMax.z);
//            normal = voxelIncr * -step;
//            voxel -= uniforms.blockMasksOffset;
//            return true;
//         }
//         else if (byte != 0)
//         {
//            // int3 mask = (ray.at(t) - voxel) > .5;
//            // int bitIdx = mask.x << 2 | mask.y << 1 | mask.z << 0;
//
//            bool hit = false;
//            Ray localRay = ray;
//            localRay.origin -= voxel - uniforms.blockMasksOffset;
//            for (int i = 0; i < 8; i++)
//            {
//                if (((byte >> i) & 1) == 1)
//                {
//                    float boxT;
//                    float3 boxNormal;
//                    if (BoxRaycast(boxes[i], localRay, boxT, boxNormal))
//                    {
//                        if (boxT > 0 && boxT <= t)
//                        {
//                            hit = true;
//                            t = boxT;
//                            normal = boxNormal;
//                        }
//                    }
//                }
//            }
//
//            if (hit)
//            {
//                voxel -= uniforms.blockMasksOffset;
//                return true;
//            }
//         }
//         
//         voxelIncr = tMax.xyz <= tMax.yxx && tMax.xyz <= tMax.zzy;
//         tMax = float3(voxelIncr) * tDelta + tMax;
//         voxel = voxelIncr * step + voxel;
// 
//         // if (step.x != 0 && tMax.x < tMax.y)
//         // {
//         //     if (step.x != 0 && tMax.x < tMax.z)
//         //     {
//         //         voxel.x += step.x;
//         //         tMax.x += tDelta.x;
//         //         normal = float3(-step.x, 0, 0);
//         //     }
//         //     else
//         //     {
//         //         voxel.z += step.z;
//         //         tMax.z += tDelta.z;
//         //         normal = float3(0, 0, -step.z);
//         //     }
//         // }
//         // else
//         // {
//         //     if (step.y != 0 && tMax.y < tMax.z)
//         //     {
//         //         voxel.y += step.y;
//         //         tMax.y += tDelta.y;
//         //         normal = float3(0, -step.y, 0);
//         //     }
//         //     else
//         //     {
//         //         voxel.z += step.z;
//         //         tMax.z += tDelta.z;
//         //         normal = float3(0, 0, -step.z);
//         //     }
//         // }
//     }
// 
//     return false;
// }

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
                if (all(outNormal == 0))
                {
                    outNormal = voxelIncr * -step;
                }
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

// // https://www.researchgate.net/publication/233899848_Efficient_implementation_of_the_3D-DDA_ray_traversal_algorithm_on_GPU_and_its_application_in_radiation_dose_calculation
// 
// bool raycast_dda_lod(Ray ray, out float outT, out float3 outNormal, out int3 outVoxel)
// {
//     // convert ray to half-block space
//     ray.origin *= 2;
//     int3 voxel = int3(floor(ray.origin));
//     int3 step = int3(sign(ray.direction));
// 
//     if (all(step == 0))
//     {
//         return false;
//     }
// 
//     float3 start = ray.at(0);
//     float3 end = ray.at(ray.length);
//     float3 tDelta = step / (end - start);
//     float3 t = tDelta * float3(
// 		step.x > 0 ? 1 - frac(start.x) : frac(start.x),
// 		step.y > 0 ? 1 - frac(start.y) : frac(start.y),
//         step.z > 0 ? 1 - frac(start.z) : frac(start.z)
//     );
// 
//     constexpr int maxlod = 1;
//     int lod = 0;
//     float lodScale = pow(2, lod);
// 
//     int mask = 0;
//     bool3 voxelIncr = bool3(false);
//     while (all(voxel >= 0) && all(voxel < 0))
//     {
//         if (lod < 0)
//         {
//             outT = max3(t.x, t.y, t.z);
//             outNormal = -step * voxelIncr;
//             return true;
//         }
//         
//         mask = blockMasks.Load(int4(voxel >> 1, max(lod - 1, 0)));
// 
//         if (mask == 0)
//         {
//             if (lod < maxlod) 
//             {
//                 lod++;
//                 lodScale = pow(2, lod);
//             }
//         }
//         else // if (mask == 0xFF)
//         {
//             lod--;
//             lodScale = pow(2, lod);
//             continue;
//         }
//         // else
//         // {
//         //     int3 localVoxel = (voxel >> lod) & int3(1, 1, 1);
//         //     int bit = localVoxel.x * 4 + localVoxel.y * 2 + localVoxel.z;
//         //     if ((mask >> bit) != 0)
//         //     {
//         //         lod--;
//         //         lodScale = pow(2, lod);
//         //         continue;
//         //     }
//         // }
// 
//         voxelIncr = t.xyz <= t.yxx && t.xyz <= t.zzy;
//         t = float3(voxelIncr) * tDelta + t;
//         voxel = voxelIncr * step + voxel;
//     }
// 
//     return false;
// }
// 
// bool raycast_dda(Ray ray, out float outT, out float3 outNormal, out int3 outVoxel)
// {
//     int3 voxel = int3(floor(ray.origin));
//     int3 step = int3(sign(ray.direction));
// 
//     if (all(step == 0))
//     {
//         return false;
//     }
// 
//     float3 start = ray.at(0);
//     float3 end = ray.at(ray.length);
//     float3 tDelta = step / (end - start);
//     float3 t = tDelta * float3(
// 		step.x > 0 ? 1 - frac(start.x) : frac(start.x),
// 		step.y > 0 ? 1 - frac(start.y) : frac(start.y),
//         step.z > 0 ? 1 - frac(start.z) : frac(start.z)
//     );
// 
//     constexpr int3 blockMasksSize = 2 * int3(128, 64, 128);
// 
//     int mask = 0;
//     bool3 voxelIncr = bool3(false);
//     while (all(voxel >= 0) && all(voxel < blockMasksSize))
//     {
//         mask = blockMasks.Load(int4(voxel, 0));
// 
//         voxelIncr = t.xyz <= t.yxx && t.xyz <= t.zzy;
//         t = float3(voxelIncr) * tDelta + t;
//         voxel = voxelIncr * step + voxel;
// 
//         if (mask != 0)
//         {
//             outT = fmax3(t.x, t.y, t.z);
//             outVoxel = voxel;
//             outNormal = -step * voxelIncr;
//             return true;
//         }
//     }
// 
//     return false;
// }
