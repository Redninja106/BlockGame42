namespace SDL.GPU;

public struct TextureCreateInfo
{
    public TextureType Type;
    public TextureFormat Format;
    public TextureUsageFlags Usage;
    public uint Width;
    public uint Height;
    public uint LayerCountOrDepth;
    public uint NumLevels;
    public SampleCount SampleCount;
    public Properties? Properties;

    internal SDL_GPUTextureCreateInfo Marshal()
    {
        return new()
        {
            type = (SDL_GPUTextureType)Type,
            format = (SDL_GPUTextureFormat)Format,
            usage = (uint)Usage,
            width = Width,
            height = Height,
            layer_count_or_depth = LayerCountOrDepth,
            num_levels = NumLevels,
            sample_count = (SDL_GPUSampleCount)SampleCount,
            props = Properties?.propertiesID ?? 0
        };
    }
}
