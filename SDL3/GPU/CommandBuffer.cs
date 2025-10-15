using Interop.Runtime;
using System;
using System.Runtime.InteropServices;

namespace SDL.GPU;

public unsafe sealed class CommandBuffer
{
    private readonly SDL_GPUCommandBuffer* handle;
    private readonly SDL_GPUDevice* deviceHandle;
    public nint Handle => (nint)handle;

    public CommandBuffer(nint handle, nint deviceHandle)
    {
        this.handle = (SDL_GPUCommandBuffer*)handle;
        this.deviceHandle = (SDL_GPUDevice*)deviceHandle;
    }

    public void Cancel()
    {
        SDL_CancelGPUCommandBuffer(handle).ThrowIfFailed();
    }

    public void Submit()
    {
        SDL_SubmitGPUCommandBuffer(handle).ThrowIfFailed();
    }

    public void GenerateMipmapsForTexture(Texture texture)
    {
        SDL_GenerateMipmapsForGPUTexture(this.handle, (SDL_GPUTexture*)texture.Handle);
    }

    public Texture? AcquireSwapchainTexture(Window window, out uint width, out uint height)
    {
        ArgumentNullException.ThrowIfNull(window);

        SDL_GPUTexture* textureHandle = null;

        fixed (uint* widthPtr = &width, heightPtr = &height)
        {
            if (!SDL_AcquireGPUSwapchainTexture(handle, (SDL_Window*)window.Handle, &textureHandle, widthPtr, heightPtr))
            {
                throw new Exception(SDL_GetError().ToString());
            }
        }

        return textureHandle == null ? null : new Texture((nint)textureHandle, (nint)deviceHandle);
    }
    public Texture? WaitAndAcquireSwapchainTexture(Window window)
    {
        return WaitAndAcquireSwapchainTexture(window, out _, out _);
    }

    public Texture? WaitAndAcquireSwapchainTexture(Window window, out uint width, out uint height)
    {
        ArgumentNullException.ThrowIfNull(window);
        
        SDL_GPUTexture* textureHandle = null;

        fixed (uint* widthPtr = &width, heightPtr = &height)
        {
            if (!SDL_WaitAndAcquireGPUSwapchainTexture(handle, (SDL_Window*)window.Handle, &textureHandle, widthPtr, heightPtr))
            {
                throw new Exception(SDL_GetError().ToString());
            }
        }
        
        return textureHandle == null ? null : new Texture((nint)textureHandle, (nint)deviceHandle);
    }

    public RenderPass BeginRenderPass(ReadOnlySpan<ColorTargetInfo> colorTargetInfos, DepthStencilTargetInfo? depthStencilTargetInfo = null)
    {
        for (int i = 0; i < colorTargetInfos.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(colorTargetInfos[i].Texture);
        }

        if (depthStencilTargetInfo != null)
        {
            ArgumentNullException.ThrowIfNull(depthStencilTargetInfo.Value.Texture);
        }

        MarshalAllocator allocator = new(stackalloc byte[1024]);
        var colorTargetInfosPtr = allocator.MarshalArrayToPointer<ColorTargetInfo, SDL_GPUColorTargetInfo>(colorTargetInfos);

        SDL_GPUDepthStencilTargetInfo dstInfo = default;
        SDL_GPUDepthStencilTargetInfo* dstInfoPtr = null;
        if (depthStencilTargetInfo.HasValue)
        {
            dstInfo = depthStencilTargetInfo.Value.Marshal();
            dstInfoPtr = &dstInfo;
        }

        SDL_GPURenderPass* renderPassHandle = SDL_BeginGPURenderPass(handle, colorTargetInfosPtr, (uint)colorTargetInfos.Length, dstInfoPtr);
        return new RenderPass((nint)renderPassHandle);
    }

    public CopyPass BeginCopyPass()
    {
        SDL_GPUCopyPass* copyPassHandle = SDL_BeginGPUCopyPass(handle);
        return new CopyPass((nint)copyPassHandle);
    }

    public ComputePass BeginComputePass(ReadOnlySpan<StorageTextureReadWriteBinding> storageTextureBindings, ReadOnlySpan<StorageBufferReadWriteBinding> storageBufferBindings)
    {
        MarshalAllocator allocator = new(stackalloc byte[2048]);
        var texBindings = allocator.MarshalArrayToPointer<StorageTextureReadWriteBinding, SDL_GPUStorageTextureReadWriteBinding>(storageTextureBindings);
        var bufBindings = allocator.MarshalArrayToPointer<StorageBufferReadWriteBinding, SDL_GPUStorageBufferReadWriteBinding>(storageBufferBindings);
        SDL_GPUComputePass* computePassHandle = SDL_BeginGPUComputePass(
            this.handle,
            texBindings,
            (uint)storageTextureBindings.Length,
            bufBindings,
            (uint)storageBufferBindings.Length
            );
        return new ComputePass((nint)computePassHandle);
    }


    public void PushVertexUniformData<T>(int slotIndex, ref T data)
        where T : unmanaged
    {
        PushVertexUniformData(slotIndex, new ReadOnlySpan<T>(ref data));
    }
    
    public void PushVertexUniformData<T>(int slotIndex, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        PushVertexUniformData(slotIndex, MemoryMarshal.AsBytes(data));
    }

    public void PushVertexUniformData(int slotIndex, ReadOnlySpan<byte> data)
    {
        fixed (byte* dataPtr = data)
        {
            SDL_PushGPUVertexUniformData(handle, (uint)slotIndex, dataPtr, (uint)data.Length);
        }
    }

    public void PushFragmentUniformData<T>(int slotIndex, ref T data)
        where T : unmanaged
    {
        PushFragmentUniformData(slotIndex, new ReadOnlySpan<T>(ref data));
    }

    public void PushFragmentUniformData<T>(int slotIndex, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        PushFragmentUniformData(slotIndex, MemoryMarshal.AsBytes(data));
    }

    public void PushFragmentUniformData(int slotIndex, ReadOnlySpan<byte> data)
    {
        fixed (byte* dataPtr = data)
        {
            SDL_PushGPUFragmentUniformData(handle, (uint)slotIndex, dataPtr, (uint)data.Length);
        }
    }


    public void PushComputeUniformData<T>(int slotIndex, ref T data)
        where T : unmanaged
    {
        PushComputeUniformData(slotIndex, new ReadOnlySpan<T>(ref data));
    }

    public void PushComputeUniformData<T>(int slotIndex, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        PushComputeUniformData(slotIndex, MemoryMarshal.AsBytes(data));
    }

    public void PushComputeUniformData(int slotIndex, ReadOnlySpan<byte> data)
    {
        fixed (byte* dataPtr = data)
        {
            SDL_PushGPUComputeUniformData(handle, (uint)slotIndex, dataPtr, (uint)data.Length);
        }
    }

}

