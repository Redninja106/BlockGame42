namespace SDL.GPU;

public unsafe struct StorageTextureReadWriteBinding : IMarshallable<SDL_GPUStorageTextureReadWriteBinding>
{
    public Texture Texture;
    public uint MipLevel;
    public uint Layer;
    public bool Cycle;

    public StorageTextureReadWriteBinding(Texture texture, uint mipLevel, uint layer, bool cycle)
    {
        Texture = texture;
        MipLevel = mipLevel;
        Layer = layer;
        Cycle = cycle;
    }

    SDL_GPUStorageTextureReadWriteBinding IMarshallable<SDL_GPUStorageTextureReadWriteBinding>.Marshal(ref MarshalAllocator allocator)
    {
        return new()
        {
            texture = (SDL_GPUTexture*)Texture.Handle,
            mip_level = MipLevel,
            layer = Layer,
            cycle = Cycle,
        };
    }
}

