using System;

namespace SDL.GPU;

public unsafe struct DataBufferRegion
{
    public DataBuffer Buffer;
    public uint Offset;
    public uint Size;

    public DataBufferRegion(DataBuffer buffer, uint offset, uint size)
    {
        Buffer = buffer;
        Offset = offset;
        Size = size;
    }

    internal SDL_GPUBufferRegion Marshal()
    {
        ArgumentNullException.ThrowIfNull(Buffer);

        return new()
        {
            buffer = (SDL_GPUBuffer*)Buffer.Handle,
            offset = Offset,
            size = Size,
        };
    }
}