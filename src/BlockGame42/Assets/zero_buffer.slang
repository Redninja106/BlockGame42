#include "sdl.slang"

SDL_CS_RW_RESOURCE(0, RWByteAddressBuffer buffer);

[shader("compute")]
[numthreads(64)]
void main(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    uint index = dispatchThreadID.x;
    buffer.Store4(index * sizeof(uint4), uint4(0));
}
