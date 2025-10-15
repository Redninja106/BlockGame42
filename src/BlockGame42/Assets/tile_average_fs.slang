#include "sdl.slang"

struct TileRecord
{
    uint id;
    uint accumulatedCount;
    uint3 accumulatedColor;
};

SDL_FS_RESOURCE(0, Texture2DArray<float4> blockTextures);
SDL_FS_SAMPLER(0, SamplerState sampler);

SDL_FS_RESOURCE(1, Texture2D<float4> texCoordTarget);
SDL_FS_RESOURCE(2, Texture2D<uint> idTarget);

SDL_FS_RESOURCE(3, StructuredBuffer<TileRecord> tileLookup);

SDL_FS_UNIFORM_BUFFER(0, uint lookupSize);

uint32_t hash(uint32_t x) {
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = (x >> 16) ^ x;
    return x;
}

//static int conflicts = 0;

uint find(uint id)
{
    // linear probing
    uint searchIndex = hash(id) % lookupSize;
    for (int i = 0; i < 32; i++) 
    {
        if (tileLookup[searchIndex].id == id)
        {
            return searchIndex;
        }

        if (tileLookup[searchIndex].id == 0)
        {
            return 0xFFFFFFFF;
        }

        searchIndex++;
        //conflicts++;

        if (searchIndex > lookupSize)
        {
            searchIndex = 0;
        }
    }

    return searchIndex;
}

[shader("fragment")]
float4 main(float4 position : SV_Position) : SV_Target
{
    uint2 location = uint2(position.xy);
    float4 texCoords = texCoordTarget[location];
    uint id = idTarget[location];

    float4 texColor = blockTextures.Sample(sampler, texCoords.xyz);
    
    if (id == 0)
    {
        return float4(.392f, .584f, .929f, 1);
    }
    
    // return float4(sin((id * 1234.678954) % 15634.51414), sin((id * 6243.16324) % 7123.72346), sin((id * 2645.6234) % 6234.172378), 1);
    // return float4((tileLookup[int(position.y) * 1920 + int(position.x)].accumulatedCount).xxx / 32.0, 1);
    
    uint idx = find(id);
    
    // return float4((conflicts).xxx / 32.0, 1);

    float3 color = texColor.xyz * float3(tileLookup[idx].accumulatedColor) / 255.0;

    if (idx == 0xFFFFFFFF)
    {
        return float4(0, 1, 0, 1);
    }

    if (tileLookup[idx].accumulatedCount == 0)
    {
        return float4(1, 0, 0, 1);
    }

    return float4(color / float(tileLookup[idx].accumulatedCount), 1);
}