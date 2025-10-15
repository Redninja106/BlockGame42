using System.Numerics;

namespace SDL.GPU;

public unsafe struct ColorTargetInfo : IMarshallable<SDL_GPUColorTargetInfo>
{
    public Texture Texture;
    public uint MipLevel;
    public uint LayerOrDepthPlane;
    public Vector4 ClearColor;
    public LoadOp LoadOp;
    public StoreOp StoreOp;
    public Texture ResolveTexture;
    public uint ResolveMipLevel;
    public uint ResolveLayer;
    public bool Cycle;
    public bool CycleResolveTexture;

    SDL_GPUColorTargetInfo IMarshallable<SDL_GPUColorTargetInfo>.Marshal(ref MarshalAllocator allocator)
    {
        return new()
        {
            texture = (SDL_GPUTexture*)Texture.Handle,
            mip_level = MipLevel,
            layer_or_depth_plane = LayerOrDepthPlane,
            clear_color = ClearColor,
            load_op = (SDL_GPULoadOp)LoadOp,
            store_op = (SDL_GPUStoreOp)StoreOp,
            resolve_texture = ResolveTexture == null ? null : (SDL_GPUTexture*)ResolveTexture.Handle,
            resolve_mip_level = ResolveMipLevel,
            resolve_layer = ResolveLayer,
            cycle = Cycle,
            cycle_resolve_texture = CycleResolveTexture
        };
    }
}
