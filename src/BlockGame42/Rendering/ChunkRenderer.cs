using BlockGame42.Chunks;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class ChunkRenderer
{
    private readonly GraphicsManager graphics;
    private readonly GraphicsPipeline chunkPipeline;
    private readonly Sampler sampler;
    private readonly TransferBuffer blockMaskVolumeTransferBuffer;
    private readonly BlockMaskManager blockMaskManager;
    
    private ComputePipeline giPipeline;
    private GraphicsPipeline tileRenderPipeline;
    private DataBuffer tileLookup;

    public Vector4 sundir = new Vector4(0, 1, 0, 1);

    struct TileRecord
    {
        public uint id;
        public uint accumulatedR;
        public uint accumulatedG;
        public uint accumulatedB;
        public uint accumulatedCount;
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

        blockMaskManager = new BlockMaskManager(graphics, 512, 64, 512);

        giPipeline = graphics.shaders.GetComputePipeline("gi");

        tileRenderPipeline = graphics.device.CreateGraphicsPipeline(new()
        {
            VertexShader = graphics.shaders.Get("fullscreen_triangle"),
            FragmentShader = graphics.shaders.Get("tile_render"),
            PrimitiveType = PrimitiveType.TriangleList,
            TargetInfo = new GraphicsPipelineTargetInfo([new ColorTargetDescription(graphics.device.GetSwapchainTextureFormat(graphics.Window), default)], 0, false),
            RasterizerState = default,
            DepthStencilState = default,
            MultisampleState = default,
            VertexInputState = default,
        });
        
        tileLookup = graphics.device.CreateDataBuffer(DataBufferUsageFlags.ComputeStorageRead | DataBufferUsageFlags.ComputeStorageWrite, (uint)Unsafe.SizeOf<TileRecord>() * 8*1920*1080);
        Console.WriteLine($"using {tileLookup.Size >> 20}MB tile lookup");
    }

    struct Uniforms
    {
        public Vector4 sundir;
        public Vector4 cameraPosition;
        public Coordinates blockMasksOffset;
        public uint lookupSize;
        public uint ticks;

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

        giPass.BindComputeSamplers(0, [
            new(Game.Textures.GetTextureArray(), sampler),
            ]);

        giPass.BindStorageTextures(0, [
            graphics.RenderTargets.PositionTexture, 
            graphics.RenderTargets.NormalTexture, 
            graphics.RenderTargets.TexelIDTexture,

            blockMaskManager.GetBlockMaskTexture(),
            blockMaskManager.GetMaterialIDTexture(), 
            ]);

        giPass.BindStorageBuffers(0, [
            Game.Materials.GetMaterialBuffer(),
            ]);

        giPass.BindPipeline(giPipeline);

        Uniforms uniforms = default;
        uniforms.lookupSize = tileLookup.Size / (uint)Unsafe.SizeOf<TileRecord>();
        uniforms.ticks = (uint)Application.GetTicksNS();
        uniforms.sundir = this.sundir;
        uniforms.blockMasksOffset = Chunk.Size * blockMaskManager.ChunkOffset;
        uniforms.cameraPosition = new(Game.player.Camera.transform.Position, 0);
        graphics.CommandBuffer.PushComputeUniformData(0, ref uniforms);

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

        graphics.CommandBuffer.PushFragmentUniformData(0, ref uniforms.lookupSize);

        Matrix4x4 mat = Matrix4x4.CreateFromQuaternion(Game.player.Camera.transform.Rotation);

        graphics.CommandBuffer.PushFragmentUniformData(0, ref uniforms.lookupSize);
        graphics.CommandBuffer.PushFragmentUniformData(1, ref mat);

        tileAveragePass.BindFragmentSamplers(0, new TextureSamplerBinding(Game.Textures.GetTextureArray(), sampler));
        tileAveragePass.BindFragmentStorageTextures(0, [graphics.RenderTargets.TexCoordTexture, graphics.RenderTargets.TexelIDTexture]);
        tileAveragePass.BindFragmentStorageBuffers(0, [tileLookup]);
        tileAveragePass.BindPipeline(tileRenderPipeline);
        tileAveragePass.DrawPrimitives(3, 1, 0, 0);
        tileAveragePass.End();
    }

    struct ChunkData
    {
        public Matrix4x4 transform;
        public uint id;
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
        ambientOcclusion = ao;
    }
}
