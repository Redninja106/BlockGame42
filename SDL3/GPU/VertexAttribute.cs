namespace SDL.GPU;

public struct VertexAttribute : IMarshallable<SDL_GPUVertexAttribute>
{
    public uint Location;
    public uint BufferSlot;
    public VertexElementFormat Format;
    public uint Offset;

    public VertexAttribute(uint location, uint bufferSlot, VertexElementFormat format, uint offset)
    {
        Location = location;
        BufferSlot = bufferSlot;
        Format = format;
        Offset = offset;
    }

    SDL_GPUVertexAttribute IMarshallable<SDL_GPUVertexAttribute>.Marshal(ref MarshalAllocator allocator)
    {
        return new()
        {
            location = Location,
            buffer_slot = BufferSlot,
            format = (SDL_GPUVertexElementFormat)Format,
            offset = Offset
        };
    }
}
