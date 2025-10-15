using BlockGame42.GUI;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class OverlayRenderer
{
    const int MaxVertices = 64 * 1024;

    private readonly GraphicsManager graphics;

    GraphicsPipeline pipeline;
    DataBuffer vertexBuffer;
    TransferBuffer transferBuffer;
    OverlayVertex[] vertices;
    int vertexCount;
    Matrix4x4 viewProjMatrix;

    public OverlayRenderer(GraphicsManager graphics)
    {
        this.graphics = graphics;

        uint bufferSize = (uint)(MaxVertices * Unsafe.SizeOf<OverlayVertex>());
        transferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, bufferSize);
        vertexBuffer = graphics.device.CreateDataBuffer(DataBufferUsageFlags.Vertex, bufferSize);

        vertices = new OverlayVertex[MaxVertices];

        pipeline = graphics.device.CreateGraphicsPipeline(new()
        {
            VertexShader = graphics.shaders.Get("overlay_vs"),
            FragmentShader = graphics.shaders.Get("overlay_fs"),
            PrimitiveType = PrimitiveType.LineList,
            VertexInputState = new VertexInputState(
                [new VertexBufferDescription(0, (uint)Unsafe.SizeOf<OverlayVertex>(), VertexInputRate.Vertex, 0)],
                [new VertexAttribute(0, 0, VertexElementFormat.Float3, 0),
                 new VertexAttribute(1, 0, VertexElementFormat.UByte4Norm, 12)]
                ),
            DepthStencilState = new()
            {
                CompareOp = CompareOp.LessOrEqual,
                EnableDepthTest = true,
                EnableDepthWrite = true,
            },
            RasterizerState = new()
            {
                EnabledDepthBias = true,
                EnableDepthClip = true,
                DepthBiasConstantFactor = -1000,
                DepthBiasSlopeFactor = 0.01f,
                DepthBiasClamp = 0.00001f,
            },
            TargetInfo = new([
                new ColorTargetDescription(graphics.device.GetSwapchainTextureFormat(graphics.Window), default)
                ], TextureFormat.D24_UNorm_S8_UInt, true)
        });
    }

    public void Begin(Camera camera)
    {
        viewProjMatrix = camera.ViewMatrix() * camera.ProjectionMatrix();
    }

    public void Flush()
    {
        Span<OverlayVertex> transferBufferData = transferBuffer.Map<OverlayVertex>(true);
        vertices.AsSpan(0, vertexCount).CopyTo(transferBufferData);
        transferBuffer.Unmap();

        CopyPass copy = graphics.CommandBuffer.BeginCopyPass();
        copy.UploadToDataBuffer(new(transferBuffer), new(vertexBuffer, 0, (uint)(this.vertexCount * Unsafe.SizeOf<OverlayVertex>())), false);
        copy.End();

        ColorTargetInfo mainRenderTarget = new()
        {
            Texture = graphics.RenderTargets.SwapchainTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        };

        DepthStencilTargetInfo depthTarget = new()
        {
            Texture = graphics.RenderTargets.DepthStencilTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        };
        
        RenderPass pass = graphics.CommandBuffer.BeginRenderPass([mainRenderTarget], depthTarget);

        Matrix4x4 viewProj = this.viewProjMatrix;
        graphics.CommandBuffer.PushVertexUniformData(0, ref viewProj);

        pass.BindPipeline(pipeline);
        pass.BindVertexBuffers(0, [new(vertexBuffer)]);

        pass.DrawPrimitives((uint)vertexCount, 1, 0, 0);

        pass.End();
        vertexCount = 0;
    }

    public void PushVertex(OverlayVertex vertex)
    {
        vertices[vertexCount++] = vertex;
    }

    public void PushLine(Vector3 from, Vector3 to, uint color)
    {
        PushVertex(new(from, color));
        PushVertex(new(to, color));
    }

    public void PushBox(Box box, uint color)
    {
        // bottom rect
        PushLine(new(box.Min.X, box.Min.Y, box.Min.Z), new(box.Max.X, box.Min.Y, box.Min.Z), color);
        PushLine(new(box.Max.X, box.Min.Y, box.Min.Z), new(box.Max.X, box.Min.Y, box.Max.Z), color);
        PushLine(new(box.Max.X, box.Min.Y, box.Max.Z), new(box.Min.X, box.Min.Y, box.Max.Z), color);
        PushLine(new(box.Min.X, box.Min.Y, box.Max.Z), new(box.Min.X, box.Min.Y, box.Min.Z), color);

        // vertical lines
        PushLine(new(box.Min.X, box.Min.Y, box.Min.Z), new(box.Min.X, box.Max.Y, box.Min.Z), color);
        PushLine(new(box.Max.X, box.Min.Y, box.Min.Z), new(box.Max.X, box.Max.Y, box.Min.Z), color);
        PushLine(new(box.Max.X, box.Min.Y, box.Max.Z), new(box.Max.X, box.Max.Y, box.Max.Z), color);
        PushLine(new(box.Min.X, box.Min.Y, box.Max.Z), new(box.Min.X, box.Max.Y, box.Max.Z), color);

        // top rect
        PushLine(new(box.Min.X, box.Max.Y, box.Min.Z), new(box.Max.X, box.Max.Y, box.Min.Z), color);
        PushLine(new(box.Max.X, box.Max.Y, box.Min.Z), new(box.Max.X, box.Max.Y, box.Max.Z), color);
        PushLine(new(box.Max.X, box.Max.Y, box.Max.Z), new(box.Min.X, box.Max.Y, box.Max.Z), color);
        PushLine(new(box.Min.X, box.Max.Y, box.Max.Z), new(box.Min.X, box.Max.Y, box.Min.Z), color);
    }
}

struct OverlayVertex
{
    public Vector3 position;
    public uint color;

    public OverlayVertex(Vector3 position, uint color)
    {
        this.position = position;
        this.color = color;
    }
}