using System;
using System.Runtime.InteropServices;

namespace SDL.GPU;

public unsafe sealed class CopyPass
{
    private readonly SDL_GPUCopyPass* handle;
    public nint Handle => (nint)handle;

    public CopyPass(nint handle)
    {
        this.handle = (SDL_GPUCopyPass*)handle;
    }

    public void UploadToDataBuffer(TransferBufferLocation source, DataBufferRegion destination, bool cycle)
    {
        var sourceMarshalled = source.Marshal();
        var destinationMarshalled = destination.Marshal();
        
        SDL_UploadToGPUBuffer(handle, &sourceMarshalled, &destinationMarshalled, cycle);
    }

    public void UploadToTexture(TextureTransferInfo source, TextureRegion destination, bool cycle)
    {
        var sourceMarshalled = source.Marshal();
        var destinationMarshalled = destination.Marshal();
        
        SDL_UploadToGPUTexture(this.handle, &sourceMarshalled, &destinationMarshalled, cycle);
    }

    public void CopyTextureToTexture(TextureLocation source, TextureLocation destination, uint width, uint height, uint depth, bool cycle)
    {
        var src = source.Marshal();
        var dst = destination.Marshal();
        SDL_CopyGPUTextureToTexture(this.handle, &src, &dst, width, height, depth, cycle);
    }

    public void End()
    {
        SDL_EndGPUCopyPass(handle);
    }
}

public unsafe struct TextureLocation
{
    public unsafe Texture Texture;
    public uint MipLevel;
    public uint Layer;
    public uint X;
    public uint Y;
    public uint Z;

    public TextureLocation(Texture texture, uint mipLevel, uint layer, uint x, uint y, uint z)
    {
        Texture = texture;
        MipLevel = mipLevel;
        Layer = layer;
        X = x;
        Y = y;
        Z = z;
    }

    internal SDL_GPUTextureLocation Marshal()
    {
        return new()
        {
            texture = (SDL_GPUTexture*)Texture.Handle,
            mip_level = MipLevel,
            layer = Layer,
            x = X,
            y = Y,
            z = Z
        };
    }
}