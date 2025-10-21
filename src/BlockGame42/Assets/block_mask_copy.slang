#include "sdl.slang"

// Copies block mask data from single 32^3 chunk buffer to combined buffer

SDL_CS_RESOURCE(0, ByteAddressBuffer maskBuffer);           // individual chunk mask buffer
SDL_CS_RW_RESOURCE(0, RWTexture3D<uint64_t> maskTexture);   // shared world mask texture

SDL_CS_RESOURCE(1, ByteAddressBuffer materialBuffer);       // individual chunk material buffer
SDL_CS_RW_RESOURCE(1, RWTexture3D<uint> materialTexture);   // shared world material texture

struct WorldBlockMaskInfo
{
    int4 position;
    int4 size;
};

SDL_CS_UNIFORM_BUFFER(0, WorldBlockMaskInfo destination);

// numthreads: 1, Chunk.Height, Chunk.Depth
[shader("compute")]
[numthreads(1, 32, 32)]
void main(uint3 threadID: SV_DispatchThreadID)
{
    const uint3 chunkSize = int3(32, 32, 32);

    // threadID.x should always be 0
    uint sourceLocation = threadID.y * 32 * 32 +
                          threadID.z * 32;

    uint3 destinationLocation = destination.position.xyz + threadID;

    // copy 1 row (32 blocks, 16 uint4s)
    for (int i = 0; i < 16; i++)
    {
        uint4 blocks = maskBuffer.Load4(sourceLocation * sizeof(uint64_t) + i * sizeof(uint4));

        maskTexture.Store(destinationLocation + uint3(i * 2 + 0, 0, 0), __asuint64(blocks.xy));
        maskTexture.Store(destinationLocation + uint3(i * 2 + 1, 0, 0), __asuint64(blocks.zw));
    }

    for (int i = 0; i < 16; i++)
    {
        uint materials = materialBuffer.Load(sourceLocation * 2 + i * 4);

        materialTexture.Store(destinationLocation + uint3(i * 2 + 0, 0, 0), (materials >> 0) & 0xFFFF);
        materialTexture.Store(destinationLocation + uint3(i * 2 + 1, 0, 0), (materials >> 16) & 0xFFFF);
    }
}
