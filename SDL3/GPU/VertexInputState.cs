using System;

namespace SDL.GPU;

public unsafe ref struct VertexInputState
{
    public ReadOnlySpan<VertexBufferDescription> VertexBufferDescriptions;
    public ReadOnlySpan<VertexAttribute> VertexAttributes;

    public VertexInputState(ReadOnlySpan<VertexBufferDescription> vertexBufferDescriptions, ReadOnlySpan<VertexAttribute> vertexAttributes)
    {
        VertexBufferDescriptions = vertexBufferDescriptions;
        VertexAttributes = vertexAttributes;
    }

    internal SDL_GPUVertexInputState Marshal(ref MarshalAllocator allocator)
    {
        return new SDL_GPUVertexInputState
        {
            vertex_buffer_descriptions = allocator.MarshalArrayToPointer<VertexBufferDescription, SDL_GPUVertexBufferDescription>(VertexBufferDescriptions),
            num_vertex_buffers = (uint)VertexBufferDescriptions.Length,
            vertex_attributes = allocator.MarshalArrayToPointer<VertexAttribute, SDL_GPUVertexAttribute>(VertexAttributes),
            num_vertex_attributes = (uint)VertexAttributes.Length
        };
    }
}
