namespace SDL.GPU;

public struct ColorTargetDescription : IMarshallable<SDL_GPUColorTargetDescription>
{
    public TextureFormat Format;
    public ColorTargetBlendState BlendState;

    public ColorTargetDescription(TextureFormat format, ColorTargetBlendState blendState)
    {
        Format = format;
        BlendState = blendState;
    }

    SDL_GPUColorTargetDescription IMarshallable<SDL_GPUColorTargetDescription>.Marshal(ref MarshalAllocator allocator)
    {
        return new SDL_GPUColorTargetDescription
        {
            format = (SDL_GPUTextureFormat)Format,
            blend_state = BlendState.Marshal()
        };
    }
}
