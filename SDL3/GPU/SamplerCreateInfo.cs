namespace SDL.GPU;

public struct SamplerCreateInfo
{
    public Filter MinFilter;
    public Filter MagFilter;
    public SamplerMipmapMode MipmapMode;
    public SamplerAddressMode AddressModeU;
    public SamplerAddressMode AddressModeV;
    public SamplerAddressMode AddressModeW;
    public float MipLodBias;
    public float MaxAnisotropy;
    public CompareOp CompareOp;
    public float MinLod;
    public float MaxLod;
    public bool EnableAnisotropy;
    public bool EnableCompare;
    public Properties? Properties;

    internal SDL_GPUSamplerCreateInfo Marshal()
    {
        return new()
        {
            min_filter = (SDL_GPUFilter)MinFilter,
            mag_filter = (SDL_GPUFilter)MagFilter,
            mipmap_mode = (SDL_GPUSamplerMipmapMode)MipmapMode,
            address_mode_u = (SDL_GPUSamplerAddressMode)AddressModeU,
            address_mode_v = (SDL_GPUSamplerAddressMode)AddressModeV,
            address_mode_w = (SDL_GPUSamplerAddressMode)AddressModeW,
            mip_lod_bias = MipLodBias,
            max_anisotropy = MaxAnisotropy,
            compare_op = (SDL_GPUCompareOp)CompareOp,
            min_lod = MinLod,
            max_lod = MaxLod,
            enable_anisotropy = EnableAnisotropy,
            enable_compare = EnableCompare,
            props = Properties?.propertiesID ?? 0
        };
    }
}
