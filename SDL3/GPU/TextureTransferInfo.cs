namespace SDL.GPU;

public unsafe struct TextureTransferInfo
{
    public TransferBuffer TransferBuffer;
    public uint Offset;
    public uint PixelsPerRow;
    public uint RowsPerLayer;

    public TextureTransferInfo(TransferBuffer transferBuffer, uint offset, uint pixelsPerRow, uint rowsPerLayer)
    {
        TransferBuffer = transferBuffer;
        Offset = offset;
        PixelsPerRow = pixelsPerRow;
        RowsPerLayer = rowsPerLayer;
    }

    public SDL_GPUTextureTransferInfo Marshal()
    {
        return new SDL_GPUTextureTransferInfo
        {
            transfer_buffer = (SDL_GPUTransferBuffer*)TransferBuffer.Handle,
            offset = Offset,
            pixels_per_row = PixelsPerRow,
            rows_per_layer = RowsPerLayer
        };
    }
}
