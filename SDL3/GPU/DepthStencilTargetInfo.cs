namespace SDL.GPU;

public unsafe struct DepthStencilTargetInfo
{
    public Texture Texture;
    public float ClearDepth;
    public LoadOp LoadOp;
    public StoreOp StoreOp;
    public LoadOp StencilLoadOp;
    public StoreOp StencilStoreOp;
    public bool Cycle;
    public byte ClearStencil;

    internal SDL_GPUDepthStencilTargetInfo Marshal()
    {
        return new SDL_GPUDepthStencilTargetInfo
        {
            texture = (SDL_GPUTexture*)Texture.Handle,
            clear_depth = ClearDepth,
            load_op = (SDL_GPULoadOp)LoadOp,
            store_op = (SDL_GPUStoreOp)StoreOp,
            stencil_load_op = (SDL_GPULoadOp)StencilLoadOp,
            stencil_store_op = (SDL_GPUStoreOp)StencilStoreOp,
            cycle = Cycle,
            clear_stencil = ClearStencil,
        };
    }
}
