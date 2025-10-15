using System;

namespace SDL.GPU;

public unsafe sealed class Texture : IDisposable
{
    private readonly SDL_GPUTexture* handle;
    private readonly SDL_GPUDevice* deviceHandle;
    public nint Handle => (nint)handle;

    public static ReadOnlySpan<byte> CreateD3D12ClearDepthFloatProperty => "SDL.gpu.texture.create.d3d12.clear.depth"u8;

    public Texture(nint handle, nint deviceHandle)
    {
        this.handle = (SDL_GPUTexture*)handle;
        this.deviceHandle = (SDL_GPUDevice*)deviceHandle;
    }

    public void Dispose()
    {
        SDL_ReleaseGPUTexture(deviceHandle, handle);
    }

    public static uint GetFormatBlockSize(TextureFormat format)
    {
        return SDL_GPUTextureFormatTexelBlockSize((SDL_GPUTextureFormat)format);
    }
}