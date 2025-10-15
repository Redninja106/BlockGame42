using BlockGame42.Rendering;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Chunks;
internal class BlockMaskManager
{
    private readonly GraphicsManager graphics;
    private readonly DataBuffer blockMaskBuffer;
    private readonly ComputePipeline copyPipeline;
    private readonly TransferBuffer blockMaskUploadBuffer;
    private int width, height, depth;

    public BlockMaskManager(GraphicsManager graphics, int width, int height, int depth)
    {
        this.graphics = graphics;
        this.width = width;
        this.height = height;
        this.depth = depth;

        blockMaskBuffer = graphics.device.CreateDataBuffer(
            DataBufferUsageFlags.ComputeStorageWrite | DataBufferUsageFlags.GraphicsStorageRead,
            (uint)(width * height * depth)
            );

        blockMaskUploadBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, Chunk.BlockCount);

        copyPipeline = graphics.shaders.GetComputePipeline("block_mask_copy");
    }

    public DataBuffer GetBlockMaskBuffer()
    {
        return blockMaskBuffer;
    }


    public void UpdateChunk(CommandBuffer commandBuffer, Coordinates chunkCoordinates, Chunk chunk, ChunkMesh chunkMesh)
    {
        // copy into transfer buffer
        Span<byte> mappedBlockMask = blockMaskUploadBuffer.Map(true);
        chunk.BlockMasks.AsSpan().CopyTo(mappedBlockMask);
        //for (int y = 0; y < 32; y++)
        //{
        //    for (int z = 0; z < 32; z++)
        //    {
        //        for (int x = 0; x < 32; x++)
        //        {
        //            mappedBlockMask[y * 32 * 32 + z * 32 + x] = (byte)(x * 8);
        //        }
        //    }
        //}
        blockMaskUploadBuffer.Unmap();
        
        // copy into chunkmesh's buffer
        CopyPass copyPass = commandBuffer.BeginCopyPass();
        TransferBufferLocation src = new(blockMaskUploadBuffer);
        DataBufferRegion dst = new(chunkMesh.BlockMaskBuffer, 0, Chunk.BlockCount);
        copyPass.UploadToDataBuffer(src, dst, false);
        copyPass.End();

        // copy into world
        ComputePass computePass = commandBuffer.BeginComputePass([], [new(blockMaskBuffer, false)]);
        WorldBlockMaskInfo worldMaskInfo = new()
        {
            position = (chunkCoordinates + new Coordinates(2, 0, 2)) * Chunk.Size,
            size = new(width, height, depth),
        };
        commandBuffer.PushComputeUniformData(0, ref worldMaskInfo);
        computePass.BindStorageBuffers(0, [chunkMesh.BlockMaskBuffer]);
        computePass.BindPipeline(copyPipeline);
        computePass.Dispatch(1, 1, 1);
        computePass.End();
    }

    struct WorldBlockMaskInfo
    {
        public Coordinates position;
        private int pad0;
        public Coordinates size;
        private int pad1;
    };
}
