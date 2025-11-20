#include "sdl.slang"
#include "hash.slang"

struct TileRecord
{
    uint id;
    uint4 accumulatedColor;
};

SDL_FS_RESOURCE(0, Texture2DArray<float4> blockTextures);
SDL_FS_SAMPLER(0, SamplerState sampler);

SDL_FS_RESOURCE(1, Texture2D<float4> positionTarget);
SDL_FS_RESOURCE(2, Texture2D<float4> normalTarget);
SDL_FS_RESOURCE(3, Texture2D<float4> texCoordTarget);

SDL_FS_RESOURCE(4, StructuredBuffer<uint> tileChecksums);
SDL_FS_RESOURCE(5, StructuredBuffer<uint4> tileIrradiances);
SDL_FS_RESOURCE(6, StructuredBuffer<uint4> tileReflections);

struct Uniforms
{
    float4 cameraPosition;
    uint phaseCount;
    uint tilesPerPhase;
    uint currentPhase;
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
    float3 position = positionTarget[location].xyz;
    float3 normal = normalTarget[location].xyz;
    float4 texCoords = texCoordTarget[location];

    float4 texColor = blockTextures.Sample(sampler, texCoords.xyz);
    float4 texEmmission = blockTextures.Sample(sampler, texCoords.xyw);
    
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
        uint idx = find(i, position, normal, hash_detail(position, uniforms.cameraPosition.xyz));

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

        if (texEmmission.w > 0 && i == uniforms.currentPhase)
        {
            uint4 packed_reflection = tileReflections[idx - i * uniforms.tilesPerPhase];
            total_reflection.xyz = float3(packed_reflection.xyz) / 255.0;
            total_reflection.w = packed_reflection.w;
        }
    }

    total_irradiance.xyz /= total_irradiance.w;

    // base color is texture + irradiance
    float3 color = texColor.xyz * total_irradiance.xyz;

    // mix in reflection
    if (total_reflection.w > 0)
    {
        total_reflection.xyz /= total_reflection.w;
        color = lerp(color, total_reflection.xyz, texEmmission.w);
    }

    // add emmission
    color += texEmmission.xyz;

    return float4(color, 1);
}
