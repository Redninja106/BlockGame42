#include "sdl.slang"

struct TexelAccumulator
{
    float3 accumulatedColor;
    uint contributionCount;
};

struct TileLookupPair
{
    uint texelID;
    uint accumulatorIndex;
};

SDL_CS_RW_RESOURCE(0, RWStructuredBuffer<TexelAccumulator> accumulators);
SDL_CS_RW_RESOURCE(1, RWStructuredBuffer<TileLookupPair> accumulatorLookup);

SDL_CS_RESOURCE(0, Texture2D<float4> colorTarget);
SDL_CS_RESOURCE(1, Texture2D<uint> idTarget);

static const int MaxIDs = 16 * 16;
static const int LookupSize = 4 * MaxIDs;

groupshared TexelAccumulator localAccumulators[MaxIDs];
groupshared uint nextAccumulatorIndex;

groupshared TileLookupPair tileLookup[LookupSize];

// uint get_or_find_elem(uint texid, StructuredBuffer)

uint find_or_insert(uint texelID)
{
    // linear probing
    uint searchIndex = texelID % LookupSize;
    while (tileLookup[searchIndex].texelID != texelID)
    {
        if (tileLookup[searchIndex].texelID == 0)
        {
            uint accumulatorIndex;
            InterlockedAdd(nextAccumulatorIndex, 1, accumulatorIndex);
            InterlockedExchange(tileLookup[searchIndex].accumulatorIndex, accumulatorIndex);
            InterlockedExchange(tileLookup[searchIndex].texelID, texelID);
            return accumulatorIndex;
        }

        searchIndex++;

        if (searchIndex > LookupSize)
        {
            searchIndex = 0;
        }
    }

    return tileLookup[searchIndex].accumulatorIndex;
}

[shader("compute")]
[numthreads(16, 16, 1)]
void main(uint3 dispatchThreadID : SV_DispatchThreadID, uint3 groupID : SV_GroupID, uint3 groupThreadID : SV_GroupThreadID)
{
    uint texelID = idTarget.Load(dispatchThreadID);
    float4 color = colorTarget.Load(dispatchThreadID);

    uint accumulatorIndex = find_or_insert(texelID);
    // InterlockedAdd(localAccumulators[accumulatorIndex].accumulatedColor.r, color.r);
    // InterlockedAdd(localAccumulators[accumulatorIndex].accumulatedColor.g, color.g);
    // InterlockedAdd(localAccumulators[accumulatorIndex].accumulatedColor.b, color.b);
    InterlockedAdd(localAccumulators[accumulatorIndex].contributionCount, 1); 

    GroupMemoryBarrierWithGroupSync();

    uint threadAccumulatorIndex = groupThreadID.y * 16 + groupThreadID.x;
    if (threadAccumulatorIndex < nextAccumulatorIndex)
    {
        
    }
}
