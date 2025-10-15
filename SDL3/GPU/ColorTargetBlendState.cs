namespace SDL.GPU;

public struct ColorTargetBlendState
{
    public BlendFactor SrcColorBlendFactor;
    public BlendFactor DstColorBlendFactor;
    public BlendOperation ColorBlendOp;
    public BlendFactor SrcAlphaBlendFactor;
    public BlendFactor DstAlphaBlendFactor;
    public BlendOperation AlphaBlendOp;
    public ColorComponentFlags ColorWriteMask;
    public bool EnableBlend;
    public bool EnableColorWriteMask;

    internal SDL_GPUColorTargetBlendState Marshal()
    {
        return new SDL_GPUColorTargetBlendState
        {
            src_color_blendfactor = (SDL_GPUBlendFactor)SrcColorBlendFactor,
            dst_color_blendfactor = (SDL_GPUBlendFactor)DstColorBlendFactor,
            color_blend_op = (SDL_GPUBlendOp)ColorBlendOp,
            src_alpha_blendfactor = (SDL_GPUBlendFactor)SrcAlphaBlendFactor,
            dst_alpha_blendfactor = (SDL_GPUBlendFactor)DstAlphaBlendFactor,
            alpha_blend_op = (SDL_GPUBlendOp)AlphaBlendOp,
            color_write_mask = (byte)ColorWriteMask,
            enable_blend = EnableBlend,
            enable_color_write_mask = EnableColorWriteMask
        };
    }
}
