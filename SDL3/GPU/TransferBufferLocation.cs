namespace SDL.GPU;

public unsafe struct TransferBufferLocation
{
    public TransferBuffer TransferBuffer;
    public uint Offset;

    public TransferBufferLocation(TransferBuffer transferBuffer, uint offset = 0)
    {
        this.TransferBuffer = transferBuffer;
        this.Offset = offset;
    }

    internal SDL_GPUTransferBufferLocation Marshal()
    {
        return new()
        {
            offset = Offset,
            transfer_buffer = (SDL_GPUTransferBuffer*)TransferBuffer.Handle,
        };
    }
}
