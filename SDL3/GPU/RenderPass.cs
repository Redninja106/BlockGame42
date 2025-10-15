using System;
using System.Runtime.InteropServices;

namespace SDL.GPU;

public unsafe sealed class RenderPass
{
    private readonly SDL_GPURenderPass* handle;
    public nint Handle => (nint)handle;

    public RenderPass(nint handle)
    {
        this.handle = (SDL_GPURenderPass*)handle;
    }

    public void BindPipeline(GraphicsPipeline graphicsPipeline)
    {
        SDL_BindGPUGraphicsPipeline(handle, (SDL_GPUGraphicsPipeline*)graphicsPipeline.Handle);
    }

    public void BindFragmentStorageTextures(uint firstSlot, params ReadOnlySpan<Texture> storageTextures)
    {
        nint* storageTextureHandles = stackalloc nint[storageTextures.Length];
        for (int i = 0; i < storageTextures.Length; i++)
        {
            storageTextureHandles[i] = storageTextures[i].Handle;
        }
        SDL_BindGPUFragmentStorageTextures(this.handle, firstSlot, (SDL_GPUTexture**)storageTextureHandles, (uint)storageTextures.Length);
    }

    public void BindFragmentStorageBuffers(uint firstSlot, params ReadOnlySpan<DataBuffer> storageBuffers)
    {
        nint* storageTextureHandles = stackalloc nint[storageBuffers.Length];
        for (int i = 0; i < storageBuffers.Length; i++)
        {
            storageTextureHandles[i] = storageBuffers[i].Handle;
        }
        SDL_BindGPUFragmentStorageBuffers(this.handle, firstSlot, (SDL_GPUBuffer**)storageTextureHandles, (uint)storageBuffers.Length);
    }

    public void BindFragmentSamplers(uint firstSlot, params ReadOnlySpan<TextureSamplerBinding> textureSamplerBindings)
    {
        MarshalAllocator allocator = new(stackalloc byte[1024]);
        Span<SDL_GPUTextureSamplerBinding> bindings = allocator.MarshalArray<TextureSamplerBinding, SDL_GPUTextureSamplerBinding>(textureSamplerBindings);
        fixed (SDL_GPUTextureSamplerBinding* bindingsPtr = bindings)
        {
            SDL_BindGPUFragmentSamplers(handle, firstSlot, bindingsPtr, (uint)bindings.Length);
        }
    }

    public void DrawPrimitives(uint numVertices, uint numInstances, uint firstVertex, uint firstInstance)
    {
        SDL_DrawGPUPrimitives(handle, numVertices, numInstances, firstVertex, firstInstance);
    }

    public void DrawPrimitivesIndexed(uint numIndices, uint numInstances, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        SDL_DrawGPUIndexedPrimitives(handle, numIndices, numInstances, firstIndex, vertexOffset, firstInstance);
    }

    public void BindVertexBuffers(uint firstSlot, ReadOnlySpan<DataBufferBinding> bindings)
    {
        MarshalAllocator allocator = new(stackalloc byte[1024]);
        SDL_GPUBufferBinding* bindingsPtr = allocator.MarshalArrayToPointer<DataBufferBinding, SDL_GPUBufferBinding>(bindings);
        
        SDL_BindGPUVertexBuffers(handle, firstSlot, bindingsPtr, (uint)bindings.Length);
    }

    public void BindIndexBuffer(DataBufferBinding binding, IndexElementSize elementSize)
    {
        MarshalAllocator allocator = new([]);
        SDL_GPUBufferBinding nativeBinding = IMarshallable<SDL_GPUBufferBinding>.Marshal(binding, ref allocator);
        SDL_BindGPUIndexBuffer(handle, &nativeBinding, (SDL_GPUIndexElementSize)elementSize);
    }

    public void End()
    {
        SDL_EndGPURenderPass(handle);
    }
}
