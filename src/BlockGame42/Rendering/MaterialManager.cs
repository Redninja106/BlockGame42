using BlockGame42.Blocks.Materials;
using BlockGame42.Blocks.Models;
using BlockGame42.Chunks;
using Protor;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class MaterialManager
{
    private readonly GraphicsContext graphics;
    public Texture AlbedoTextureArray { get; private set; }
    public Texture NormalTextureArray { get; private set; }
    public Texture SpecularTextureArray { get; private set; }

    private uint nextId = 1;
    public uint Empty { get; private set; }

    public MaterialManager(GraphicsContext graphics, uint textureWidth, uint textureHeight)
    {
        this.graphics = graphics;

        AlbedoTextureArray = graphics.device.CreateTexture(new() 
        {
            Type = TextureType._2DArray, 
            Format = TextureFormat.R8G8B8A8_UNorm, 
            Usage = TextureUsageFlags.Sampler | TextureUsageFlags.ColorTarget, 
            Width = textureWidth, 
            Height = textureHeight,
            LayerCountOrDepth = 2048, 
            NumLevels = 3, 
            SampleCount = SampleCount._1,
        });

        NormalTextureArray = graphics.device.CreateTexture(new()
        {
            Type = TextureType._2DArray,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsageFlags.Sampler | TextureUsageFlags.ColorTarget,
            Width = textureWidth,
            Height = textureHeight,
            LayerCountOrDepth = 2048,
            NumLevels = 3,
            SampleCount = SampleCount._1,
        });

        SpecularTextureArray = graphics.device.CreateTexture(new()
        {
            Type = TextureType._2DArray,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsageFlags.Sampler | TextureUsageFlags.ColorTarget,
            Width = textureWidth,
            Height = textureHeight,
            LayerCountOrDepth = 2048,
            NumLevels = 3,
            SampleCount = SampleCount._1,
        });
        // using TransferBuffer transferBuffer = new TransferBuffer();

        // TextureData data = new()
        // {
        //     Width = 16,
        //     Height = 16,
        //     Data = new byte[4 * 16 * 16],
        // };
        //data.Data.AsSpan().Fill(0);
        //Empty = nextId++;
        //UploadTextureData(data, Empty);
    }

    public void Load()
    {
        foreach (var material in Registry.GetAll<CombinedBlockMaterial>())
        {
            uint index = nextId++;
            UploadTextureData(AlbedoTextureArray, graphics.LoadTextureData(material.Albedo), index);
            UploadTextureData(NormalTextureArray, graphics.LoadTextureData(material.Normal), index);
            UploadTextureData(SpecularTextureArray, graphics.LoadTextureData(material.Specular), index);
            material.TexID = index;
        }
    }

    //public void AddTexture(string assetName)
    //{
    //    uint id = nextId;
    //    assetNameToLayerIndexMap[assetName] = id;

    //    nextId++;
    //}

    //public uint Get(string? assetName)
    //{
    //    if (assetName == null)
    //    {
    //        return Empty;
    //    }

    //    if (!assetNameToLayerIndexMap.TryGetValue(assetName, out uint index))
    //    {
    //        index = nextId;
            
    //        TextureData data = graphics.LoadTextureData(assetName);
    //        UploadTextureData(data, index);

    //        assetNameToLayerIndexMap[assetName] = index;
    //        nextId++;
    //    }

    //    return index;
    //}

    void UploadTextureData(Texture target, TextureData data, uint index)
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
            Texture = target,
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
        commandBuffer.GenerateMipmapsForTexture(this.AlbedoTextureArray);
        commandBuffer.GenerateMipmapsForTexture(this.NormalTextureArray);
        commandBuffer.GenerateMipmapsForTexture(this.SpecularTextureArray);
    }
}
