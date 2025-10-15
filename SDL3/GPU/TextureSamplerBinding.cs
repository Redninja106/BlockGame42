using System;
using System.Diagnostics;

namespace SDL.GPU;

public unsafe struct TextureSamplerBinding : IMarshallable<SDL_GPUTextureSamplerBinding>
{
    public Texture Texture;
    public Sampler Sampler;

    public TextureSamplerBinding(Texture texture, Sampler sampler)
    {
        Texture = texture;
        Sampler = sampler;
    }

    SDL_GPUTextureSamplerBinding IMarshallable<SDL_GPUTextureSamplerBinding>.Marshal(ref MarshalAllocator allocator)
    {
        ArgumentNullException.ThrowIfNull(Texture);
        ArgumentNullException.ThrowIfNull(Sampler);

        return new()
        {
            texture = (SDL_GPUTexture*)Texture.Handle,
            sampler = (SDL_GPUSampler*)Sampler.Handle,
        };
    }
}
