using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlockGame42.Rendering;
internal class TransferBufferBatcher
{
    private readonly Device device;

    private TransferBuffer transferBuffer;
    private bool shouldCycle;
    private int position;

    private CommandBuffer commandBuffer;
    private CopyPass copyPass;

    public TransferBufferBatcher(Device device, uint size)
    {
        this.device = device;
        transferBuffer = device.CreateTransferBuffer(TransferBufferUsage.Upload, size);
    }

    public void BeginBatch(CommandBuffer commandBuffer)
    {
        this.commandBuffer = commandBuffer;
        copyPass = commandBuffer.BeginCopyPass();
    }

    public void UploadToBuffer<T>(Span<T> data, DataBufferRegion region, bool cycle)
        where T : unmanaged
    {
        UploadToBuffer(MemoryMarshal.AsBytes(data), region, cycle);
    }

    public void UploadToBuffer(Span<byte> data, DataBufferRegion region, bool cycle)
    {
        var location = Upload(data);
        copyPass.UploadToDataBuffer(location, region, cycle);
    }

    public void UploadToTexture(Span<byte> data, uint pixelsPerRow, uint rowsPerLayer, TextureRegion region, bool cycle)
    {
        TransferBufferLocation location = Upload(data);

        TextureTransferInfo source = new()
        {
            TransferBuffer = location.TransferBuffer,
            Offset = location.Offset,
            PixelsPerRow = pixelsPerRow,
            RowsPerLayer = rowsPerLayer,
        };

        copyPass.UploadToTexture(source, region, cycle);
    }

    private TransferBufferLocation Upload(Span<byte> data)
    {
        if (position + data.Length > transferBuffer.Size)
        {
            EndBatch();
            BeginBatch(this.commandBuffer);
        }

        Span<byte> mapped = transferBuffer.Map(shouldCycle);
        shouldCycle = false;
        data.CopyTo(mapped.Slice(position, data.Length));
        transferBuffer.Unmap();

        TransferBufferLocation result = new(transferBuffer, (uint)position);

        position += data.Length;
        
        return result;
    }


    public void EndBatch()
    {
        copyPass.End();
        shouldCycle = true;
        position = 0;
    }
}
