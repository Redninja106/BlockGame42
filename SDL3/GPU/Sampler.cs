using System;

namespace SDL.GPU;

public unsafe sealed class Sampler : IDisposable
{
    private readonly SDL_GPUSampler* handle;
    private readonly SDL_GPUDevice* deviceHandle;
    public nint Handle => (nint)handle;

    public Sampler(nint handle, nint deviceHandle)
    {
        this.handle = (SDL_GPUSampler*)handle;
        this.deviceHandle = (SDL_GPUDevice*)deviceHandle;
    }

    public void Dispose()
    {
        SDL_ReleaseGPUSampler(deviceHandle, handle);
    }
}
