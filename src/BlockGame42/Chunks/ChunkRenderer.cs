using BlockGame42.Rendering;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Chunks;
internal class ChunkRenderer
{
    private readonly GraphicsManager graphics;
    private readonly GraphicsPipeline chunkPipeline;
    //private readonly GraphicsPipeline depthOnlyChunkPipeline;
    private readonly Sampler sampler;
    // private BlockTextureManager blockTextures;
    private readonly DataBuffer blockMaskVolumeBuffer;
    private readonly TransferBuffer blockMaskVolumeTransferBuffer;
    private readonly BlockMaskManager blockMaskManager;
    
    private ComputePipeline raymarchedGIPipeline;
    private ComputePipeline idAccumulatorPipeline;
    private GraphicsPipeline tileAveragePipeline;
    private DataBuffer tileLookup;

    public Vector4 sundir = new Vector4(0, 1, 0, 1);

    struct TileRecord
    {
        public uint id;
        public uint accumulatedCount;
        public uint accumulatedR;
        public uint accumulatedG;
        public uint accumulatedB;
    }

    public ChunkRenderer(GraphicsManager graphics)
    {
        this.graphics = graphics;

        chunkPipeline = graphics.device.CreateGraphicsPipeline(new()
        {
            VertexShader = graphics.shaders.Get("deferred_chunk_vs"),
            FragmentShader = graphics.shaders.Get("deferred_chunk_fs"),
            TargetInfo = new GraphicsPipelineTargetInfo([
                new ColorTargetDescription(TextureFormat.R32G32B32A32_Float, default),
                new ColorTargetDescription(TextureFormat.R32G32B32A32_Float, default),
                new ColorTargetDescription(TextureFormat.R32G32B32A32_Float, default),
                new ColorTargetDescription(TextureFormat.R32_UInt, default),
                ], 
                TextureFormat.D24_UNorm_S8_UInt,
                true
                ),
            PrimitiveType = PrimitiveType.TriangleList,
            VertexInputState = new VertexInputState([
                new VertexBufferDescription(0, MinimizedChunkVertex.Size, VertexInputRate.Vertex, 0)
                ], MinimizedChunkVertex.Attributes),
            RasterizerState = new RasterizerState(FillMode.Fill, CullMode.Back, FrontFace.CounterClockwise, 0, 0, 0, false, true),
            MultisampleState = new MultisampleState(SampleCount._1, 0, false, false),
            DepthStencilState = new DepthStencilState(CompareOp.Less, default, default, 0, 0, true, true, false),
        });

        //depthOnlyChunkPipeline = graphics.device.CreateGraphicsPipeline(new()
        //{
        //    VertexShader = graphics.shaders.Get("chunk_vs"),
        //    FragmentShader = graphics.shaders.Get("chunk_depth_fs"),
        //    TargetInfo = new GraphicsPipelineTargetInfo(
        //        [],
        //        TextureFormat.D24_UNorm_S8_UInt,
        //        true
        //        ),
        //    PrimitiveType = PrimitiveType.TriangleList,
        //    VertexInputState = new VertexInputState([
        //        new VertexBufferDescription(0, MinimizedChunkVertex.Size, VertexInputRate.Vertex, 0)
        //        ], MinimizedChunkVertex.Attributes),
        //    RasterizerState = new RasterizerState(FillMode.Fill, CullMode.Back, FrontFace.CounterClockwise, 0, 0, 0, false, true),
        //    MultisampleState = new MultisampleState(SampleCount._1, 0, false, false),
        //    DepthStencilState = new DepthStencilState(CompareOp.Less, default, default, 0, 0, true, true, false),
        //});



        //graphics.device.CreateTexture(new()
        //{
        //    Format = TextureFormat.R8G8B8A8_UNorm,
        //    Height = 1024,
        //    Width = 1024,
        //    LayerCountOrDepth = 10,
        //    NumLevels = 0,
        //    Usage = TextureUsageFlags.ComputeStorageSimultaneousReadWrite,
        //});
        // blockMaskVolumeBuffer = graphics.device.CreateDataBuffer(DataBufferUsageFlags.GraphicsStorageRead, 128 * 128 * 64);

        // blockMaskVolumeTransferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, Chunk.Width * Chunk.Height * Chunk.Depth);


        // blockTextures = new(graphics);

        sampler = graphics.device.CreateSampler(new()
        {
            MipmapMode = SamplerMipmapMode.Linear,
            MagFilter = Filter.Nearest,
            MinFilter = Filter.Nearest,
            MinLod = 0,
            MaxLod = 5,
        });

        this.blockMaskManager = new BlockMaskManager(graphics, 128, 64, 128);

        raymarchedGIPipeline = graphics.shaders.GetComputePipeline("raymarched_gi");
        idAccumulatorPipeline = graphics.shaders.GetComputePipeline("id_accumulator");

        tileAveragePipeline = graphics.device.CreateGraphicsPipeline(new()
        {
            VertexShader = graphics.shaders.Get("fullscreen_triangle"),
            FragmentShader = graphics.shaders.Get("tile_average_fs"),
            PrimitiveType = PrimitiveType.TriangleList,
            TargetInfo = new GraphicsPipelineTargetInfo([new ColorTargetDescription(graphics.device.GetSwapchainTextureFormat(graphics.Window), default)], 0, false),
            RasterizerState = default,
            DepthStencilState = default,
            MultisampleState = default,
            VertexInputState = default,
        });
        
        tileLookup = graphics.device.CreateDataBuffer(DataBufferUsageFlags.ComputeStorageRead | DataBufferUsageFlags.ComputeStorageWrite, (uint)Unsafe.SizeOf<TileRecord>() * 4*1920*1080);
        Console.WriteLine($"using {tileLookup.Size >> 20}MB tile lookup");
    }

    public void Render(Camera camera, ChunkManager chunks)
    {
        bool anyStale = false;
        foreach (var (coords, (chunk, mesh)) in chunks.chunkMap)
        {
            if (chunk.BlockMasks.Stale)
            {
                blockMaskManager.UpdateChunk(graphics.CommandBuffer, coords, chunk, mesh);
                chunk.BlockMasks.Stale = false;
                anyStale = true;
            }
        }

        if (anyStale)
        {
            graphics.ClearDataBuffer(tileLookup, false);
        }

        // var copyPass = graphics.CommandBuffer.BeginCopyPass();
        // foreach (var (coords, (chunk, mesh)) in chunks.chunkMap)
        // {
        //     if (chunk.BlockMasks.Stale)
        //     {
        //         UpdateBlockMask(copyPass, coords, chunk);
        //         chunk.BlockMasks.Stale = false;
        //     }
        // }
        // copyPass.End();

        //var depthOnlyPass = graphics.CommandBuffer.BeginRenderPass([], new()
        //{
        //    Texture = graphics.RenderTargets.DepthStencilTexture,
        //    LoadOp = LoadOp.Load,
        //    StoreOp = StoreOp.Store,
        //});
        //depthOnlyPass.BindPipeline(depthOnlyChunkPipeline);

        //Matrix4x4 viewProj = camera.ViewMatrix() * camera.ProjectionMatrix();
        //graphics.CommandBuffer.PushVertexUniformData(1, ref viewProj);

        //ChunkData data = default;
        //foreach (var (coordinates, (chunk, mesh)) in chunks.chunkMap)
        //{
        //    data.transform = Matrix4x4.CreateTranslation(coordinates.ToVector() * Chunk.SizeVector);
        //    data.id += (uint)(mesh.VertexCount / 6);
        //    graphics.CommandBuffer.PushVertexUniformData(0, ref data);

        //    mesh.Draw(graphics.CommandBuffer, depthOnlyPass);

        //    foreach (var entity in chunk.Entities)
        //    {
        //        entity.Draw(graphics.CommandBuffer, depthOnlyPass);
        //    }
        //}
        //depthOnlyPass.End();

        ColorTargetInfo positionTarget = new()
        {
            Texture = graphics.RenderTargets.PositionTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        }; 
        
        ColorTargetInfo texCoordTarget = new()
        {
            Texture = graphics.RenderTargets.TexCoordTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        };

        ColorTargetInfo normalTarget = new()
        {
            Texture = graphics.RenderTargets.NormalTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        };

        ColorTargetInfo texelIDTarget = new()
        {
            Texture = graphics.RenderTargets.TexelIDTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        };

        DepthStencilTargetInfo depthStencilTarget = new()
        {
            Texture = graphics.RenderTargets.DepthStencilTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        };

        var renderPass = graphics.CommandBuffer.BeginRenderPass([positionTarget, texCoordTarget, normalTarget, texelIDTarget], depthStencilTarget);

        // renderPass.BindFragmentStorageBuffers(0, [blockMaskManager.GetBlockMaskBuffer()]);
        renderPass.BindPipeline(chunkPipeline);

        Matrix4x4 viewProj = camera.ViewMatrix() * camera.ProjectionMatrix();
        graphics.CommandBuffer.PushVertexUniformData(1, ref viewProj);

        ChunkData data = default;
        foreach (var (coordinates, (chunk, mesh)) in chunks.chunkMap)
        {
            data.transform = Matrix4x4.CreateTranslation(coordinates.ToVector() * Chunk.SizeVector);
            data.id += (uint)(mesh.VertexCount / 6);
            graphics.CommandBuffer.PushVertexUniformData(0, ref data);
            
            mesh.Draw(graphics.CommandBuffer, renderPass);

            foreach (var entity in chunk.Entities)
            {
                entity.Draw(graphics.CommandBuffer, renderPass);
            }
        }
        
        renderPass.End();

        // graphics.ClearDataBuffer(tileLookup, true);

        StorageBufferReadWriteBinding tileLookupBinding = new(tileLookup, false);
        var giPass = graphics.CommandBuffer.BeginComputePass([], [tileLookupBinding]);

        giPass.BindStorageBuffers(0, [
            blockMaskManager.GetBlockMaskBuffer()
            ]);

        giPass.BindStorageTextures(0, [
            graphics.RenderTargets.PositionTexture, 
            graphics.RenderTargets.NormalTexture, 
            graphics.RenderTargets.TexelIDTexture
            ]);
        giPass.BindPipeline(raymarchedGIPipeline);

        uint buffersize = this.tileLookup.Size / 20;
        graphics.CommandBuffer.PushComputeUniformData(0, ref buffersize);

        uint ticks = (uint)Application.GetTicksNS();
        graphics.CommandBuffer.PushComputeUniformData(1, ref ticks);

        Vector4 sundir = this.sundir;
        graphics.CommandBuffer.PushComputeUniformData(2, ref sundir);

        giPass.Dispatch((graphics.RenderTargets.Width + 15) / 16, (graphics.RenderTargets.Height + 15) / 16, 1);

        giPass.End();


        //StorageBufferReadWriteBinding tileLookupBinding = new(tileLookup, false);
        //var idAccumulatorPass = graphics.CommandBuffer.BeginComputePass([], [tileLookupBinding]);
        //idAccumulatorPass.BindStorageTextures(0, [graphics.RenderTargets.TexelIDTexture, graphics.RenderTargets.SwapchainTexture]);
        //idAccumulatorPass.BindPipeline(idAccumulatorPipeline);
        //uint buffersize = this.tileLookup.Size / 20;
        //graphics.CommandBuffer.PushComputeUniformData(0, ref buffersize);
        //idAccumulatorPass.Dispatch((graphics.RenderTargets.Width + 15) / 16, (graphics.RenderTargets.Height + 15) / 16, 1);
        //idAccumulatorPass.End();

        StorageTextureReadWriteBinding colorTargetBinding = new(graphics.RenderTargets.SwapchainTexture, 0, 0, false);
        
        var tileAveragePass = graphics.CommandBuffer.BeginRenderPass([new ColorTargetInfo() 
        { 
            Texture = graphics.RenderTargets.SwapchainTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        }], default);

        graphics.CommandBuffer.PushFragmentUniformData(0, ref buffersize);
        tileAveragePass.BindFragmentSamplers(0, new TextureSamplerBinding(Game.Textures.GetBlockTextureArray(), this.sampler));
        tileAveragePass.BindFragmentStorageTextures(0, [graphics.RenderTargets.TexCoordTexture, graphics.RenderTargets.TexelIDTexture]);
        tileAveragePass.BindFragmentStorageBuffers(0, [tileLookup]);
        tileAveragePass.BindPipeline(tileAveragePipeline);
        tileAveragePass.DrawPrimitives(3, 1, 0, 0);
        tileAveragePass.End();
    }

    struct ChunkData
    {
        public Matrix4x4 transform;
        public uint id;
    }

    Coordinates blockMasksOffset = new(2, 0, 2);
    private void UpdateBlockMask(CopyPass copyPass, Coordinates chunkCoordinates, Chunk chunk)
    {
        Span<byte> blockMasksMapped = blockMaskVolumeTransferBuffer.Map(true);
        chunk.BlockMasks.AsSpan().CopyTo(blockMasksMapped);
        blockMaskVolumeTransferBuffer.Unmap();

        chunkCoordinates += blockMasksOffset;

        int offset = chunkCoordinates.Y * 4 * 4 + chunkCoordinates.Z * 4 + chunkCoordinates.X;
        TransferBufferLocation source = new(blockMaskVolumeTransferBuffer);
        DataBufferRegion region = new DataBufferRegion(this.blockMaskVolumeBuffer, (uint)offset * Chunk.BlockCount, Chunk.BlockCount);
        copyPass.UploadToDataBuffer(source, region, false);
    }

}

struct ChunkVertex
{
    public Vector3 position;
    public Vector2 textureCoordinates;
    public uint blockTextureId;
    public Vector4 ambientOcclusion;

    public ChunkVertex(Vector3 position, Vector2 textureCoordinates, uint blockTextureId, Vector4 ao)
    {
        this.position = position;
        this.textureCoordinates = textureCoordinates;
        this.blockTextureId = blockTextureId;
        this.ambientOcclusion = ao;
    }
}
