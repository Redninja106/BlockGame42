namespace SDL.GPU;

public unsafe struct StorageBufferReadWriteBinding : IMarshallable<SDL_GPUStorageBufferReadWriteBinding>
{
    public DataBuffer Buffer;
    public bool Cycle;

    public StorageBufferReadWriteBinding(DataBuffer buffer, bool cycle)
    {
        Buffer = buffer;
        Cycle = cycle;
    }

    SDL_GPUStorageBufferReadWriteBinding IMarshallable<SDL_GPUStorageBufferReadWriteBinding>.Marshal(ref MarshalAllocator allocator)
    {
        return new()
        {
            buffer = (SDL_GPUBuffer*)Buffer.Handle,
            cycle = Cycle,
        };
    }
}

