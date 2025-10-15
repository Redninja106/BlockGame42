namespace SDL.GPU;

public struct VertexBufferDescription : IMarshallable<SDL_GPUVertexBufferDescription>
{
    public uint Slot;
    public uint Pitch;
    public VertexInputRate InputRate;
    public uint InstanceStepRate;

    public VertexBufferDescription(uint slot, uint pitch, VertexInputRate inputRate, uint instanceStepRate)
    {
        Slot = slot;
        Pitch = pitch;
        InputRate = inputRate;
        InstanceStepRate = instanceStepRate;
    }

    SDL_GPUVertexBufferDescription IMarshallable<SDL_GPUVertexBufferDescription>.Marshal(ref MarshalAllocator allocator)
    {
        return new()
        {
            input_rate = (SDL_GPUVertexInputRate)(int)InputRate,
            instance_step_rate = InstanceStepRate,
            pitch = Pitch,
            slot = Slot
        };
    }
}
