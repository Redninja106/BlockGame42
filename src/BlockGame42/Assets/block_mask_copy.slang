#include "sdl.slang"

// Copies block mask data from single 32^3 chunk buffer to combined buffer

SDL_CS_RESOURCE(0, ByteAddressBuffer srcBuffer);        // chunk's individual buffer
SDL_CS_RW_RESOURCE(0, RWTexture3D<uint> destTexture);   // shared world buffer

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

    // copy 1 row (32 bytes, 8 ints)
    [unroll]
    for (int i = 0; i < 8; i++)
    {
        uint blocks = srcBuffer.Load(sourceLocation + i * 4);

        destTexture.Store(destinationLocation + uint3(i * 4 + 0, 0, 0), (blocks >> 0 ) & 0xFF);
        destTexture.Store(destinationLocation + uint3(i * 4 + 1, 0, 0), (blocks >> 8 ) & 0xFF);
        destTexture.Store(destinationLocation + uint3(i * 4 + 2, 0, 0), (blocks >> 16) & 0xFF);
        destTexture.Store(destinationLocation + uint3(i * 4 + 3, 0, 0), (blocks >> 24) & 0xFF);
    }
}
