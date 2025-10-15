using System;

namespace SDL.GPU;

public unsafe ref struct GraphicsPipelineTargetInfo
{
    public ReadOnlySpan<ColorTargetDescription> ColorTargetDescriptions;
    public TextureFormat DepthStencilFormat;
    public bool HasDepthStencilTarget;

    public GraphicsPipelineTargetInfo(ReadOnlySpan<ColorTargetDescription> colorTargetDescriptions, TextureFormat depthStencilFormat, bool hasDepthStencilTarget)
    {
        ColorTargetDescriptions = colorTargetDescriptions;
        DepthStencilFormat = depthStencilFormat;
        HasDepthStencilTarget = hasDepthStencilTarget;
    }

    internal SDL_GPUGraphicsPipelineTargetInfo Marshal(ref MarshalAllocator allocator)
    {
        return new SDL_GPUGraphicsPipelineTargetInfo
        {
            color_target_descriptions = allocator.MarshalArrayToPointer<ColorTargetDescription, SDL_GPUColorTargetDescription>(ColorTargetDescriptions),
            num_color_targets = (uint)ColorTargetDescriptions.Length,
            depth_stencil_format = (SDL_GPUTextureFormat)DepthStencilFormat,
            has_depth_stencil_target = HasDepthStencilTarget
        };
    }
}
