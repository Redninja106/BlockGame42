using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDL.GPU;
public unsafe sealed class DataBuffer : IDisposable
{
    private SDL_GPUBuffer* handle;
    private SDL_GPUDevice* deviceHandle;
    private readonly uint size;

    public nint Handle => (nint)handle;
    public uint Size => size;

    public DataBuffer(nint handle, nint deviceHandle, uint size)
    {
        this.handle = (SDL_GPUBuffer*)handle;
        this.deviceHandle = (SDL_GPUDevice*)deviceHandle;
        this.size = size;
    }

    public void Dispose()
    {
        SDL_ReleaseGPUBuffer(deviceHandle, handle);
        deviceHandle = null;
        handle = null;
    }
}
