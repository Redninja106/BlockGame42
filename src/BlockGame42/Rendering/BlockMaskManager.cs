using BlockGame42.Chunks;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class BlockMaskManager
{
    private readonly GraphicsManager graphics;
    // private readonly DataBuffer blockMaskBuffer;
    private readonly Texture blockMaskTexture;
    private readonly Texture materialIDTexture;

    private readonly DataBuffer blockMaskSourceBuffer;
    private readonly DataBuffer materialIDSourceBuffer;

    private readonly ComputePipeline copyPipeline;
    private readonly TransferBuffer uploadBuffer;
    private int width, height, depth;
    
    public Coordinates ChunkOffset { get; private set; }

    public BlockMaskManager(GraphicsManager graphics, int width, int height, int depth)
    {
        this.graphics = graphics;
        this.width = width;
        this.height = height;
        this.depth = depth;
        ChunkOffset = new(width / (Chunk.Width * 2), 0, depth / (Chunk.Depth * 2));

        //blockMaskBuffer = graphics.device.CreateDataBuffer(
        //    DataBufferUsageFlags.ComputeStorageWrite | DataBufferUsageFlags.GraphicsStorageRead,
        //    (uint)(width * height * depth)
        //    );

        blockMaskTexture = graphics.device.CreateTexture(new()
        {
            Type = TextureType._3D,
            Format = TextureFormat.R32G32_UInt,
            Usage = TextureUsageFlags.ComputeStorageRead | TextureUsageFlags.ComputeStorageWrite,
            Width = (uint)width,
            Height = (uint)height,
            LayerCountOrDepth = (uint)depth,
            NumLevels = 1,
            SampleCount = SampleCount._1,
        });
        blockMaskSourceBuffer = graphics.device.CreateDataBuffer(DataBufferUsageFlags.GraphicsStorageRead, Chunk.BlockCount * sizeof(ulong));
        Console.WriteLine($"Block mask texture is {sizeof(ulong) * width * height * depth >> 20}MB");

        materialIDTexture = graphics.device.CreateTexture(new()
        {
            Type = TextureType._3D,
            Format = TextureFormat.R16_UInt,
            Usage = TextureUsageFlags.ComputeStorageRead | TextureUsageFlags.ComputeStorageWrite,
            Width = (uint)width,
            Height = (uint)height,
            LayerCountOrDepth = (uint)depth,
            NumLevels = 1,
            SampleCount = SampleCount._1,
        });
        materialIDSourceBuffer = graphics.device.CreateDataBuffer(DataBufferUsageFlags.GraphicsStorageRead, Chunk.BlockCount * sizeof(ushort));
        Console.WriteLine($"material id texture is {sizeof(ushort) * width * height * depth >> 20}MB");

        uploadBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, materialIDSourceBuffer.Size + blockMaskSourceBuffer.Size);

        copyPipeline = graphics.shaders.GetComputePipeline("block_mask_copy");
    }

    //public DataBuffer GetBlockMaskBuffer()
    //{
    //    return blockMaskBuffer;
    //}

    public Texture GetBlockMaskTexture()
    {
        return blockMaskTexture;
    }

    public Texture GetMaterialIDTexture()
    {
        return materialIDTexture;
    }

    public void UpdateChunk(CommandBuffer commandBuffer, Coordinates chunkCoordinates, Chunk chunk, ChunkMesh chunkMesh)
    {
        // copy into transfer buffer
        Span<byte> mappedUploadBuffer = uploadBuffer.Map(true);
        
        Span<ulong> mappedBlockMasks = MemoryMarshal.Cast<byte, ulong>(mappedUploadBuffer.Slice(0, (int)blockMaskSourceBuffer.Size));
        chunk.BlockMasks.AsSpan().CopyTo(mappedBlockMasks);

        Span<ushort> mappedMaterialIDs = MemoryMarshal.Cast<byte, ushort>(mappedUploadBuffer.Slice((int)blockMaskSourceBuffer.Size, (int)materialIDSourceBuffer.Size));
        for (int y = 0; y < 32; y++)
        {
            for (int z = 0; z < 32; z++)
            {
                for (int x = 0; x < 32; x++)
                {
                    mappedMaterialIDs[y * 32 * 32 + z * 32 + x] = (ushort)Game.Materials.Get(chunk.Blocks[x, y, z].Model.GetMaterial(chunk.BlockStates[x, y, z]));
                }
            }
        }
        uploadBuffer.Unmap();

        CopyPass copyPass = commandBuffer.BeginCopyPass();
        
        TransferBufferLocation src = new(uploadBuffer);
        DataBufferRegion dst = new(blockMaskSourceBuffer, 0, blockMaskSourceBuffer.Size);
        copyPass.UploadToDataBuffer(src, dst, true);

        src = new(uploadBuffer, blockMaskSourceBuffer.Size);
        dst = new(materialIDSourceBuffer, 0, materialIDSourceBuffer.Size);
        copyPass.UploadToDataBuffer(src, dst, false);

        copyPass.End();

        // copy into world
        ComputePass computePass = commandBuffer.BeginComputePass([new(blockMaskTexture, 0, 0, false), new(materialIDTexture, 0, 0, false)], []);
        WorldBlockMaskInfo worldMaskInfo = new()
        {
            position = (chunkCoordinates + ChunkOffset) * Chunk.Size,
            size = new(width, height, depth),
        };
        commandBuffer.PushComputeUniformData(0, ref worldMaskInfo);
        computePass.BindStorageBuffers(0, [blockMaskSourceBuffer, materialIDSourceBuffer]);
        computePass.BindPipeline(copyPipeline);
        computePass.Dispatch(1, 1, 1);
        computePass.End();
    }

    public void DrawDebugOverlay()
    {
        Game.gameRenderer.OverlayRenderer.PushBox(new(new(-width/2, 0, -depth/2), new(width/2, height, depth/2)), 0xFF343434);
    }

    struct WorldBlockMaskInfo
    {
        public Coordinates position;
        private int pad0;
        public Coordinates size;
        private int pad1;
    };
}
