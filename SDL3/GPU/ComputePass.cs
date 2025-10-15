using System;

namespace SDL.GPU;

public unsafe sealed class ComputePass
{
    private readonly SDL_GPUComputePass* handle;
    public nint Handle => (nint)handle;

    public ComputePass(nint handle)
    {
        this.handle = (SDL_GPUComputePass*)handle;
    }

    public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        SDL_DispatchGPUCompute(this.handle, groupCountX, groupCountY, groupCountZ);
    }

    public void BindPipeline(ComputePipeline computePipeline)
    {
        SDL_BindGPUComputePipeline(this.handle, (SDL_GPUComputePipeline*)computePipeline.Handle);
    }

    public void BindStorageTextures(uint firstSlot, ReadOnlySpan<Texture> storageTextures)
    {
        MarshalAllocator allocator = new(stackalloc byte[512]);
        var bufferPointers = (SDL_GPUTexture**)allocator.AllocateRaw(sizeof(SDL_GPUTexture*) * storageTextures.Length);
        for (int i = 0; i < storageTextures.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(storageTextures[i]);
            bufferPointers[i] = (SDL_GPUTexture*)storageTextures[i].Handle;
        }
        SDL_BindGPUComputeStorageTextures(this.handle, firstSlot, bufferPointers, (uint)storageTextures.Length);
    }

    public void BindStorageBuffers(uint firstSlot, ReadOnlySpan<DataBuffer> storageBuffers)
    {
        MarshalAllocator allocator = new(stackalloc byte[512]);
        var bufferPointers = (SDL_GPUBuffer**)allocator.AllocateRaw(sizeof(SDL_GPUBuffer*) * storageBuffers.Length);
        for (int i = 0; i < storageBuffers.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(storageBuffers[i]);
            bufferPointers[i] = (SDL_GPUBuffer*)storageBuffers[i].Handle;
        }
        SDL_BindGPUComputeStorageBuffers(this.handle, firstSlot, bufferPointers, (uint)storageBuffers.Length);
    }

    public void End()
    {
        SDL_EndGPUComputePass(handle);
    }
}

