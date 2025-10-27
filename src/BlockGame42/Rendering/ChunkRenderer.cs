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
    private readonly TileLookupManager tileLookupManager;

    private ComputePipeline giPipeline;
    private GraphicsPipeline tileRenderPipeline;
    // private DataBuffer tileLookup;

    public Vector4 sundir = new Vector4(MathF.Cos(60 / 180f * MathF.PI), MathF.Sin(60 / 180f * MathF.PI), 0, 1);
    public bool animateSun = true;

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

        tileLookupManager = new TileLookupManager(graphics, 10, 5_000_000, 10);

        // tileLookup = graphics.device.CreateDataBuffer(DataBufferUsageFlags.ComputeStorageRead | DataBufferUsageFlags.ComputeStorageWrite, (uint)Unsafe.SizeOf<TileRecord>() * 10_000_000);
    }

    struct Uniforms
    {
        public Vector4 sundir;
        public Vector4 cameraPosition;
        public Coordinates blockMasksOffset;
        public uint tileOffset;
        public uint tileCount;
        public uint phaseCount;
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


        // if (anyStale)
        // {
        //     graphics.ClearDataBufferRange(tileLookup, 0, tileLookup.Size, false);
        // }

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

        //graphics.ClearDataBuffer(tileLookup, false);

        StorageBufferReadWriteBinding checksumsBinding = new(tileLookupManager.GetChecksums(), false);
        StorageBufferReadWriteBinding irradiancesBinding = new(tileLookupManager.GetIrradiances(), false);
        StorageBufferReadWriteBinding reflectionsBinding = new(tileLookupManager.GetReflections(), false);
        var giPass = graphics.CommandBuffer.BeginComputePass([], [checksumsBinding, irradiancesBinding, reflectionsBinding]);

        giPass.BindComputeSamplers(0, [
            new(Game.Textures.GetTextureArray(), sampler),
            ]);

        giPass.BindStorageTextures(0, [
            graphics.RenderTargets.PositionTexture, 
            graphics.RenderTargets.NormalTexture, 
            graphics.RenderTargets.TexCoordTexture,

            blockMaskManager.GetBlockMaskTexture(),
            blockMaskManager.GetMaterialIDTexture(), 
            ]);

        giPass.BindStorageBuffers(0, [
            Game.Materials.GetMaterialBuffer(),
            ]);

        giPass.BindPipeline(giPipeline);

        Uniforms uniforms = default;
        uniforms.tileOffset = tileLookupManager.CurrentPhase * tileLookupManager.TilesPerPhase;
        uniforms.tileCount = tileLookupManager.TilesPerPhase;
        uniforms.phaseCount = tileLookupManager.PhaseCount;
        uniforms.ticks = (uint)Application.GetTicksNS();
        uniforms.sundir = this.sundir;

        if (animateSun)
        {
            uniforms.sundir = Vector4.Transform(uniforms.sundir, Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, (Application.GetTicks() / 1000f) * MathF.Tau * (1f / 240)));
        }

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
        
        var tileRenderPass = graphics.CommandBuffer.BeginRenderPass([new ColorTargetInfo() 
        { 
            Texture = graphics.RenderTargets.SwapchainTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        }], default);

        graphics.CommandBuffer.PushFragmentUniformData(0, ref uniforms);

        TileRenderUniforms tileUniforms = new()
        {
            phaseCount = tileLookupManager.PhaseCount,
            tileCount = tileLookupManager.TilesPerPhase,
            currentPhase = tileLookupManager.CurrentPhase,
        };

        graphics.CommandBuffer.PushFragmentUniformData(0, ref tileUniforms);

        tileRenderPass.BindFragmentSamplers(0, new TextureSamplerBinding(Game.Textures.GetTextureArray(), sampler));
        tileRenderPass.BindFragmentStorageTextures(0, [
            graphics.RenderTargets.PositionTexture, 
            graphics.RenderTargets.NormalTexture, 
            graphics.RenderTargets.TexCoordTexture,
            ]);
        tileRenderPass.BindFragmentStorageBuffers(0, [
            tileLookupManager.GetChecksums(), 
            tileLookupManager.GetIrradiances(), 
            tileLookupManager.GetReflections(),
            ]);
        tileRenderPass.BindPipeline(tileRenderPipeline);
        tileRenderPass.DrawPrimitives(3, 1, 0, 0);
        tileRenderPass.End();

        tileLookupManager.PhaseTick();
    }

    struct TileRenderUniforms
    {
        public uint phaseCount;
        public uint tileCount;
        public uint currentPhase;
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
