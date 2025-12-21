#include "sdl.slang"
#include "hash.slang"
#include "util.slang"

struct TileRecord
{
    uint id;
    uint4 accumulatedColor;
};

SDL_FS_SAMPLER(0, SamplerState sampler);
SDL_FS_SAMPLER(1, SamplerState sampler1);
SDL_FS_SAMPLER(2, SamplerState sampler2);

SDL_FS_RESOURCE(0, Texture2DArray<float4> albedoTextureArray);
SDL_FS_RESOURCE(1, Texture2DArray<float4> normalTextureArray);
SDL_FS_RESOURCE(2, Texture2DArray<float4> specularTextureArray);

SDL_FS_RESOURCE(3, Texture2D<float4> positionTarget);
SDL_FS_RESOURCE(4, Texture2D<float4> albedoTarget);
SDL_FS_RESOURCE(5, Texture2D<float4> normalTarget);
SDL_FS_RESOURCE(6, Texture2D<float4> specularTarget);

SDL_FS_RESOURCE(7, StructuredBuffer<uint> tileChecksums);
SDL_FS_RESOURCE(8, StructuredBuffer<uint4> tileIrradiances);
SDL_FS_RESOURCE(9, StructuredBuffer<uint4> tileReflections);

struct Uniforms
{
    float4 cameraPosition;
    uint phaseCount;
    uint tilesPerPhase;
    uint currentPhase;
    uint frameCount;
};

SDL_FS_UNIFORM_BUFFER(0, Uniforms uniforms);

uint find(uint phase, float3 position, float3 normal, float detail)
{
    uint phaseOffset = phase * uniforms.tilesPerPhase;

    uint hash, checksum;
    get_hash_and_checksum(position, normal, detail, hash, checksum);

    uint searchIndex = hash % uniforms.tilesPerPhase;
    for (int i = 0; i < 32; i++)
    {
        uint searchIndexChecksum = tileChecksums[phaseOffset + searchIndex];

        if (searchIndexChecksum == checksum)
        {
            return phaseOffset + searchIndex;
        }

        if (searchIndexChecksum == 0)
        {
            return 0xFFFFFFFF;
        }

        searchIndex = pcg_hash(searchIndex) % uniforms.tilesPerPhase;
    }

    return 0xFFFFFFFF;
}

[shader("fragment")]
float4 main(float4 position : SV_Position) : SV_Target
{
    uint2 location = uint2(position.xy);

    float4 pos_and_norm = positionTarget[location];
    float4 albedo = albedoTarget[location];
    float4 encoded_normal = normalTarget[location];
    float4 specular = specularTarget[location];

    float3 position = pos_and_norm.xyz;
    float3 original_normal = unpackSnorm4x8ToFloat(asuint(pos_and_norm.w)).xyz;

    float3 normal;
    normal.xy = encoded_normal.xy;
    normal.z = sqrt(1 - dot(normal.xy, normal.xy));

    // return float4(original_normal * .5 + .5, 1);

    float emission = specular.a;
    if (emission == 1)
    {
        emission = 0;
    }

    float3 reflectance = calc_reflectance(albedo, normal, specular, normalize(uniforms.cameraPosition.xyz - position));

    if (all(normal == 0))
    {
        return float4(.392f, .584f, .929f, 1);
    }

    // return float4((tileLookup[int(position.y) * 1920 + int(position.x)].accumulatedCount).xxx / 32.0, 1);
    // return float4(sin((idx * 1234.678954) % 15634.51414), sin((idx * 6243.16324) % 7123.72346), sin((idx * 2645.6234) % 6234.172378), 1);
    // return float4((conflicts).xxx / 32.0, 1);

    float4 total_reflection = 0;
    float4 total_irradiance = 0;
    for (int i = 0; i < uniforms.phaseCount; i++)
    {
        uint idx = find(i, position, original_normal, hash_detail(position, uniforms.cameraPosition.xyz));

        if (idx == 0xFFFFFFFF)
        {
            continue;
        }

        uint4 packed_irradiance = tileIrradiances[idx];
        float3 irradiance = float3(packed_irradiance.xyz) / 255.0;
        uint accumulatedCount = packed_irradiance.w;
        if (accumulatedCount == 0)
        {
            continue;
        }

        // return float4(sin((idx * 1234.678954) % 15634.51414), sin((idx * 6243.16324) % 7123.72346), sin((idx * 2645.6234) % 6234.172378), 1);

        total_irradiance += float4(irradiance, float(accumulatedCount));

        if (any(reflectance > 0) && i == uniforms.currentPhase)
        {
            uint current_reflections_phase_offset = (uniforms.frameCount & 1) * uniforms.tilesPerPhase;

            uint4 packed_reflection = tileReflections[idx - i * uniforms.tilesPerPhase + current_reflections_phase_offset];
            total_reflection.xyz = float3(packed_reflection.xyz) / 255.0;
            total_reflection.w = packed_reflection.w;
        }
    }

    total_irradiance.xyz /= total_irradiance.w;

    // base color is texture * irradiance * ao
    float3 color = albedo.xyz * total_irradiance.xyz * encoded_normal.z;

    // mix in reflection
    if (total_reflection.w > 0)
    {
        total_reflection.xyz /= total_reflection.w;
        color = lerp(color, total_reflection.xyz, reflectance);
    }

    // add emmission
    color += emission * albedo.xyz;

    return float4(color.xyz, 1);
}
