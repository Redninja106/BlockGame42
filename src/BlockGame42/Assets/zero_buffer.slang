#include "sdl.slang"

SDL_CS_RW_RESOURCE(0, RWByteAddressBuffer buffer);

struct Args
{
    uint word_offset;
    uint word_length;
};

SDL_CS_UNIFORM_BUFFER(0, Args args)


[shader("compute")]
[numthreads(64)]
void main(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    uint addr = args.word_offset + dispatchThreadID.x;
    if (dispatchThreadID.x < args.word_length)
    {
        buffer.Store(addr * sizeof(uint), 0);
    }
}
