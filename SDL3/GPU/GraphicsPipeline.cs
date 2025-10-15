using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SDL.GPU;

public unsafe sealed class GraphicsPipeline : IDisposable
{
    private readonly SDL_GPUGraphicsPipeline* handle;
    private readonly SDL_GPUDevice* deviceHandle;
    public nint Handle => (nint)handle;

    public GraphicsPipeline(nint handle, nint deviceHandle)
    {
        this.handle = (SDL_GPUGraphicsPipeline*)handle;
        this.deviceHandle = (SDL_GPUDevice*)deviceHandle;
    }

    public void Dispose()
    {
        SDL_ReleaseGPUGraphicsPipeline(deviceHandle, handle);
    }
}
