using System;

namespace SDL.GPU;

public unsafe sealed class ComputePipeline : IDisposable
{
    private readonly SDL_GPUComputePipeline* handle;
    private readonly SDL_GPUDevice* deviceHandle;
    public nint Handle => (nint)handle;

    public ComputePipeline(nint handle, nint deviceHandle)
    {
        this.handle = (SDL_GPUComputePipeline*)handle;
        this.deviceHandle = (SDL_GPUDevice*)deviceHandle;
    }

    public void Dispose()
    {
        SDL_ReleaseGPUComputePipeline(deviceHandle, handle);
    }
}

