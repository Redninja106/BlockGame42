using System.Diagnostics.CodeAnalysis;

namespace SDL.GPU;

public unsafe ref struct GraphicsPipelineCreateInfo
{
    public Shader VertexShader;
    public Shader FragmentShader;
    public VertexInputState VertexInputState;
    public PrimitiveType PrimitiveType;
    public RasterizerState RasterizerState;
    public MultisampleState MultisampleState;
    public DepthStencilState DepthStencilState;
    public GraphicsPipelineTargetInfo TargetInfo;

    internal SDL_GPUGraphicsPipelineCreateInfo Marshal(scoped ref MarshalAllocator allocator)
    {
        return new()
        {
            vertex_shader = VertexShader.handle,
            fragment_shader = FragmentShader.handle,
            vertex_input_state = VertexInputState.Marshal(ref allocator),
            primitive_type = (SDL_GPUPrimitiveType)PrimitiveType,
            rasterizer_state = RasterizerState.Marshal(ref allocator),
            multisample_state = MultisampleState.Marshal(ref allocator),
            depth_stencil_state = DepthStencilState.Marshal(ref allocator),
            target_info = TargetInfo.Marshal(ref allocator)
        };
    }
}
