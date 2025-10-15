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

    public void End()
    {
        SDL_EndGPUCopyPass(handle);
    }
}
