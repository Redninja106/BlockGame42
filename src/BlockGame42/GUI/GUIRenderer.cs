using BlockGame42.Rendering;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.GUI;
internal class GUIRenderer
{
    const int MaxVertices = 64 * 1024;

    private readonly GraphicsManager graphics;

    GraphicsPipeline pipeline;
    DataBuffer vertexBuffer;
    TransferBuffer transferBuffer;
    Sampler sampler;
    GUIVertex[] vertices;
    int vertexCount;
    Texture SolidTexture;
    Texture? activeTexture;
    Matrix4x4 transformMatrix;

    public GUIRenderer(GraphicsManager graphics)
    {
        this.graphics = graphics;

        uint bufferSize = (uint)(MaxVertices * Unsafe.SizeOf<GUIVertex>());
        transferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, bufferSize);
        vertexBuffer = graphics.device.CreateDataBuffer(DataBufferUsageFlags.Vertex, bufferSize);

        vertices = new GUIVertex[MaxVertices];

        ColorTargetBlendState blendState = new()
        {
            EnableBlend = true,

            ColorBlendOp = BlendOperation.Add,
            SrcColorBlendFactor = BlendFactor.One,
            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,

            AlphaBlendOp = BlendOperation.Add,
            SrcAlphaBlendFactor = BlendFactor.SrcAlpha,
            DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
        };

        pipeline = graphics.device.CreateGraphicsPipeline(new()
        {
            VertexShader = graphics.shaders.Get("gui_vs"),
            FragmentShader = graphics.shaders.Get("gui_fs"),
            PrimitiveType = PrimitiveType.TriangleList,
            VertexInputState = new VertexInputState(
                [new VertexBufferDescription(0, (uint)Unsafe.SizeOf<GUIVertex>(), VertexInputRate.Vertex, 0)],
                [new VertexAttribute(0, 0, VertexElementFormat.Float2, 0),
                 new VertexAttribute(1, 0, VertexElementFormat.Float2, 8),
                 new VertexAttribute(2, 0, VertexElementFormat.UByte4Norm, 16)]
                ),
            TargetInfo = new([
                new ColorTargetDescription(graphics.device.GetSwapchainTextureFormat(graphics.Window), blendState)], TextureFormat.Invalid, false)
        });

        sampler = graphics.device.CreateSampler(new()
        {
        });

        SolidTexture = graphics.device.CreateTexture(new()
        {
            Format = TextureFormat.R8G8B8A8_UNorm,
            Width = 1,
            Height = 1,
            NumLevels = 1,
            LayerCountOrDepth = 1,
            Usage = TextureUsageFlags.Sampler,
            Type = TextureType._2D,
        });

        using TransferBuffer tempTransferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, sizeof(uint));
        var mappedBuffer = tempTransferBuffer.Map<uint>(false);
        mappedBuffer[0] = 0xFFFFFFFF;
        tempTransferBuffer.Unmap();

        var cmdBuffer = graphics.device.AcquireCommandBuffer();
        var copyPass = cmdBuffer.BeginCopyPass();
        copyPass.UploadToTexture(new(tempTransferBuffer, 0, 1, 1), new TextureRegion(SolidTexture, 0, 0, 0, 0, 0, 1, 1, 1), false);
        copyPass.End();
        cmdBuffer.Submit();
    }

    public void Begin(int width, int height)
    {
        transformMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0f, 1f);
    }

    public void Flush()
    {
        Span<GUIVertex> transferBufferData = transferBuffer.Map<GUIVertex>(true);
        vertices.AsSpan(0, vertexCount).CopyTo(transferBufferData);
        transferBuffer.Unmap();

        CopyPass copy = graphics.CommandBuffer.BeginCopyPass();
        copy.UploadToDataBuffer(new(transferBuffer), new(vertexBuffer, 0, (uint)(this.vertexCount * Unsafe.SizeOf<GUIVertex>())), false);
        copy.End();

        ColorTargetInfo mainRenderTarget = new()
        {
            Texture = graphics.RenderTargets.SwapchainTexture,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store,
        };

        RenderPass pass = graphics.CommandBuffer.BeginRenderPass([mainRenderTarget]);

        Matrix4x4 transform = this.transformMatrix;
        graphics.CommandBuffer.PushVertexUniformData(0, ref transform);

        pass.BindFragmentSamplers(0, [new(activeTexture ?? SolidTexture, sampler)]);
        pass.BindPipeline(pipeline);
        pass.BindVertexBuffers(0, [new(vertexBuffer)]);

        pass.DrawPrimitives((uint)vertexCount, 1, 0, 0);

        pass.End();
        vertexCount = 0;
        activeTexture = null;
    }

    public void UseTexture(Texture? texture)
    {
        if (texture != activeTexture)
        {
            Flush();
            activeTexture = texture;
        }
    }

    public void PushVertex(GUIVertex vertex)
    {
        vertices[vertexCount++] = vertex;
    }

    public void PushRectangle(Vector2 a, Vector2 b, uint color)
    {
        PushRectangle(a, b, new(0, 0), new(1, 1), color);
    }

    public void PushRectangle(Vector2 a, Vector2 b, Vector2 uv0, Vector2 uv1, uint color)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);

        GUIVertex topLeft     = new(new Vector2(min.X, min.Y), new Vector2(uv0.X, uv0.Y), color);
        GUIVertex topRight    = new(new Vector2(max.X, min.Y), new Vector2(uv1.X, uv0.Y), color);
        GUIVertex bottomLeft  = new(new Vector2(min.X, max.Y), new Vector2(uv0.X, uv1.Y), color);
        GUIVertex bottomRight = new(new Vector2(max.X, max.Y), new Vector2(uv1.X, uv1.Y), color);

        PushVertex(topLeft);
        PushVertex(topRight);
        PushVertex(bottomLeft);

        PushVertex(topRight);
        PushVertex(bottomRight);
        PushVertex(bottomLeft);
    }

    public void PushLine(Vector2 from, Vector2 to, uint color, float thickness)
    {
        Vector2 direction = Vector2.Normalize(to - from);
        Vector2 perp = .5f * thickness * new Vector2(-direction.Y, direction.X);
        Vector2 para = .5f * direction;

        GUIVertex fromUpper = new(from + perp - para, new Vector2(0, 0), color);
        GUIVertex fromLower = new(from - perp - para, new Vector2(1, 0), color);
        GUIVertex toUpper   = new(to +   perp + para, new Vector2(0, 1), color);
        GUIVertex toLower   = new(to -   perp + para, new Vector2(1, 1), color);

        PushVertex(fromUpper);
        PushVertex(fromLower);
        PushVertex(toUpper);

        PushVertex(fromLower);
        PushVertex(toLower);
        PushVertex(toUpper);
    }

    public void PushRectangleOutline(Vector2 a, Vector2 b, uint color, float thickness)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);

        GUIVertex topLeft       = new(new Vector2(min.X, min.Y), new Vector2(0, 0), color);
        GUIVertex topRight      = new(new Vector2(max.X, min.Y), new Vector2(1, 0), color);
        GUIVertex bottomLeft    = new(new Vector2(min.X, max.Y), new Vector2(0, 1), color);
        GUIVertex bottomRight   = new(new Vector2(max.X, max.Y), new Vector2(1, 1), color);

        PushVertex(topLeft);
        PushVertex(topRight);
        PushVertex(bottomLeft);

        PushVertex(topRight);
        PushVertex(bottomRight);
        PushVertex(bottomLeft);
    }
}
