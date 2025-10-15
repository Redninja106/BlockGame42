using BlockGame42.Chunks;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class BlockTextureManager
{
    private readonly GraphicsManager graphics;
    private Texture textureArray;

    private Dictionary<string, uint> assetNameToLayerIndexMap = new();
    private uint nextId = 0;

    public BlockTextureManager(GraphicsManager graphics)
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
    }

    public Texture GetBlockTextureArray()
    {
        return textureArray;
    }

    //public void AddTexture(string assetName)
    //{
    //    uint id = nextId;
    //    assetNameToLayerIndexMap[assetName] = id;

    //    nextId++;
    //}

    public uint Get(string assetName)
    {
        if (!assetNameToLayerIndexMap.TryGetValue(assetName, out uint index))
        {
            index = nextId;

            TextureData data = graphics.LoadTextureData(assetName);

            TextureRegion region = new()
            {
                Texture = textureArray,
                W = (uint)data.Width,
                H = (uint)data.Height,
                D = 1,
                Layer = index
            };

            graphics.transferBatcher.UploadToTexture(data.Data, (uint)data.Width, (uint)data.Height, region, false);


            assetNameToLayerIndexMap[assetName] = index;
            nextId++;
        }

        return index;
    }

    public void GenerateMipmaps(CommandBuffer commandBuffer)
    {
        commandBuffer.GenerateMipmapsForTexture(this.textureArray);
    }
}
