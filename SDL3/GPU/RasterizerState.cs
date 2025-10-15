namespace SDL.GPU;

public struct RasterizerState
{
    public FillMode FillMode;
    public CullMode CullMode;
    public FrontFace FrontFace;
    public float DepthBiasConstantFactor;
    public float DepthBiasClamp;
    public float DepthBiasSlopeFactor;
    public bool EnabledDepthBias;
    public bool EnableDepthClip;

    public RasterizerState(FillMode fillMode, CullMode cullMode, FrontFace frontFace, float depthBiasConstantFactor, float depthBiasClamp, float depthBiasSlopeFactor, bool enabledDepthBias, bool enableDepthClip)
    {
        FillMode = fillMode;
        CullMode = cullMode;
        FrontFace = frontFace;
        DepthBiasConstantFactor = depthBiasConstantFactor;
        DepthBiasClamp = depthBiasClamp;
        DepthBiasSlopeFactor = depthBiasSlopeFactor;
        EnabledDepthBias = enabledDepthBias;
        EnableDepthClip = enableDepthClip;
    }

    internal SDL_GPURasterizerState Marshal(ref MarshalAllocator allocator)
    {
        return new SDL_GPURasterizerState
        {
            fill_mode = (SDL_GPUFillMode)FillMode,
            cull_mode = (SDL_GPUCullMode)CullMode,
            front_face = (SDL_GPUFrontFace)FrontFace,
            depth_bias_constant_factor = DepthBiasConstantFactor,
            depth_bias_clamp = DepthBiasClamp,
            depth_bias_slope_factor = DepthBiasSlopeFactor,
            enable_depth_bias = EnabledDepthBias,
            enable_depth_clip = EnableDepthClip
        };
    }
}
