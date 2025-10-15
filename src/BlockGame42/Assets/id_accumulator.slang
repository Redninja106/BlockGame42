#include "sdl.slang"

struct TileRecord
{
    uint id;
    uint accumulatedCount;
    uint3 accumulatedColor;
};

SDL_CS_RW_RESOURCE(0, RWStructuredBuffer<TileRecord> tileLookup);

SDL_CS_RESOURCE(0, Texture2D<uint> idTarget);
SDL_CS_RESOURCE(1, Texture2D<unorm float4> colorTarget);

SDL_CS_UNIFORM_BUFFER(0, uint lookupSize);

//static int conflicts = 0;

[shader("compute")]
[numthreads(16, 16, 1)]
void main(uint3 dispatchThreadID : SV_DispatchThreadID, uint3 groupThreadID : SV_GroupThreadID)
{
    uint id = idTarget[dispatchThreadID.xy];
    
    float4 color = colorTarget[dispatchThreadID.xy];

    uint idx = find_or_insert(id);
    InterlockedAdd(tileLookup[idx].accumulatedColor.r, int(255 * color.r));
    InterlockedAdd(tileLookup[idx].accumulatedColor.g, int(255 * color.g));
    InterlockedAdd(tileLookup[idx].accumulatedColor.b, int(255 * color.b));
    InterlockedAdd(tileLookup[idx].accumulatedCount, 1);

    //tileLookup[dispatchThreadID.y * 1920 + dispatchThreadID.x].accumulatedCount = conflicts;
}

uint32_t hash(uint32_t x) {
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = (x >> 16) ^ x;
    return x;
}

uint find_or_insert(uint id)
{
    // linear probing
    uint searchIndex = hash(id) % lookupSize;
    for (int i = 0; i < 32; i++)
    { 
        uint original;
        InterlockedCompareExchange(tileLookup[searchIndex].id, 0, id, original);
        if (original == 0 || original == id)
        {
            return searchIndex;
        }
        // if (tileLookup[searchIndex].id == 0)
        // {
        //     
        // }

        searchIndex++;
        //conflicts++;

        if (searchIndex >= lookupSize)
        {
            searchIndex = 0;
        }
    }

    return searchIndex;
}