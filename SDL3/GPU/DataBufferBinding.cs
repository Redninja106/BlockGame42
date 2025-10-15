using System;

namespace SDL.GPU;

public unsafe struct DataBufferBinding : IMarshallable<SDL_GPUBufferBinding>
{
    public DataBuffer Buffer;
    public uint Offset;

    public DataBufferBinding(DataBuffer buffer, uint offset = 0)
    {
        this.Buffer = buffer;
        this.Offset = offset;
    }

    SDL_GPUBufferBinding IMarshallable<SDL_GPUBufferBinding>.Marshal(ref MarshalAllocator allocator)
    {
        ArgumentNullException.ThrowIfNull(Buffer);

        return new()
        {
            buffer = (SDL_GPUBuffer*)Buffer.Handle,
            offset = Offset,
        };
    }
}