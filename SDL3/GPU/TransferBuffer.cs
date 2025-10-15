using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace SDL.GPU;

public unsafe sealed class TransferBuffer : IDisposable
{
    public const string CreateNameStringProperty = "SDL.gpu.transferbuffer.create.name";

    private readonly SDL_GPUTransferBuffer* handle;
    private readonly SDL_GPUDevice* deviceHandle;
    private readonly uint size;

    public nint Handle => (nint)handle;
    public uint Size => size;

    public TransferBuffer(nint handle, nint deviceHandle, uint size)
    {
        this.handle = (SDL_GPUTransferBuffer*)handle;
        this.deviceHandle = (SDL_GPUDevice*)deviceHandle;
        this.size = size;
    }

    public Span<byte> Map(bool cycle)
    {
        void* data = SDL_MapGPUTransferBuffer(deviceHandle, handle, cycle);
        return new Span<byte>(data, (int)size);
    }

    public Span<T> Map<T>(bool cycle)
        where T : unmanaged
    {
        return MemoryMarshal.Cast<byte, T>(Map(cycle));
    }

    public void Unmap()
    {
        SDL_UnmapGPUTransferBuffer(deviceHandle, handle);
    }

    public void Dispose()
    {
        SDL_ReleaseGPUTransferBuffer(this.deviceHandle, this.handle);
    }
}
