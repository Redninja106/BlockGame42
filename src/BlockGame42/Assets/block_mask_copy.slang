#include "sdl.slang"

// Copies block mask data from single 32^3 chunk buffer to combined buffer

SDL_CS_RESOURCE(0, ByteAddressBuffer srcBuffer);        // chunk's individual buffer
SDL_CS_RW_RESOURCE(0, RWByteAddressBuffer destBuffer);  // shared world buffer

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
    constexpr uint3 chunkSize = int3(32, 32, 32);

    // threadID.x should always be 1
    uint sourceLocation = threadID.y * 32 * 32 +
                          threadID.z * 32;

    uint destinationLocation = (threadID.y + destination.position.y) * destination.size.z * destination.size.x +
                               (threadID.z + destination.position.z) * destination.size.x + 
                               destination.position.x;

    // copy 1 row (32 bytes, 8 ints)
    [unroll]
    for (int i = 0; i < 8; i++)
    {
        uint blocks = srcBuffer.Load(sourceLocation + i * 4);
        destBuffer.Store(destinationLocation + i * 4, blocks);
    }
}
