namespace SDL.GPU;

public struct StencilOpState
{
    public StencilOp FailOp;
    public StencilOp PassOp;
    public StencilOp DepthFailOp;
    public CompareOp CompareOp;

    public StencilOpState(StencilOp failOp, StencilOp passOp, StencilOp depthFailOp, CompareOp compareOp)
    {
        FailOp = failOp;
        PassOp = passOp;
        DepthFailOp = depthFailOp;
        CompareOp = compareOp;
    }

    internal SDL_GPUStencilOpState Marshal()
    {
        return new()
        {
            fail_op = (SDL_GPUStencilOp)FailOp,
            pass_op = (SDL_GPUStencilOp)PassOp,
            depth_fail_op = (SDL_GPUStencilOp)DepthFailOp,
            compare_op = (SDL_GPUCompareOp)CompareOp,
        };
    }
}