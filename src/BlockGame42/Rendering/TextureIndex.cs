using BlockGame42.Chunks;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class TextureIndex
{
    private readonly GraphicsManager graphics;
    private Texture textureArray;

    private Dictionary<string, uint> assetNameToLayerIndexMap = new();
    private uint nextId = 0;
    public uint Empty { get; private set; }

    public TextureIndex(GraphicsManager graphics)
    {
        this.graphics = graphics;

        textureArray = graphics.device.CreateTexture(new() 
        { 
            Type = TextureType._2DArray, 
            Format = TextureFormat.R8G8B8A8_UNorm, 
            Usage = TextureUsageFlags.Sampler | TextureUsageFlags.ColorTarget, 
            Width = 16, 
            Height = 16,
            LayerCountOrDepth = 1024, 
            NumLevels = 5, 
            SampleCount = SampleCount._1,
        });

        TextureData data = new()
        {
            Width = 16,
            Height = 16,
            Data = new byte[4 * 16 * 16],
        };
        data.Data.AsSpan().Fill(0);
        Empty = nextId++;
        UploadTextureData(data, Empty);
    }

    public Texture GetTextureArray()
    {
        return textureArray;
    }

    //public void AddTexture(string assetName)
    //{
    //    uint id = nextId;
    //    assetNameToLayerIndexMap[assetName] = id;

    //    nextId++;
    //}

    public uint Get(string? assetName)
    {
        if (assetName == null)
        {
            return Empty;
        }

        if (!assetNameToLayerIndexMap.TryGetValue(assetName, out uint index))
        {
            index = nextId;
            
            TextureData data = graphics.LoadTextureData(assetName);
            UploadTextureData(data, index);

            assetNameToLayerIndexMap[assetName] = index;
            nextId++;
        }

        return index;
    }

    void UploadTextureData(TextureData data, uint index)
    {
        using TransferBuffer transferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, (uint)(4 * data.Width * data.Height));

        Span<byte> mappedBuffer = transferBuffer.Map(false);
        data.Data.CopyTo(mappedBuffer);
        transferBuffer.Unmap();
            
        CopyPass pass = graphics.CommandBuffer.BeginCopyPass();
        TextureTransferInfo source = new()
        {
            TransferBuffer = transferBuffer,
            Offset = 0,
            PixelsPerRow = (uint)data.Width,
            RowsPerLayer = (uint)data.Height,
        };
        TextureRegion destination = new()
        {
            Texture = this.textureArray,
            W = (uint)data.Width,
            H = (uint)data.Height,
            D = 1,
            Layer = index,
        };
        pass.UploadToTexture(source, destination, false);
        pass.End();
    }

    public void GenerateMipmaps(CommandBuffer commandBuffer)
    {
        commandBuffer.GenerateMipmapsForTexture(this.textureArray);
    }
}
