using BlockGame42.Chunks;
using BlockGame42.GUI;
using SDL;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;

internal class GraphicsManager
{
    private readonly IAssetSource assets;

    public Window Window { get; }

    public Device device;
    public ShaderManager shaders;
    // public TransferBufferBatcher transferBatcher;
    
    public CommandBuffer CommandBuffer { get; private set; }
    //public Texture SwapchainTexture { get; private set; }

    //public Texture DepthStencilTexture { get; private set; } = null!;
    //public uint BackBufferTextureWidth { get; private set; }
    //public uint BackBufferTextureHeight { get; private set; }

    //public Texture TexelIDTexture { get; private set; } = null!;
    //public uint TexelIDTextureWidth { get; private set; }
    //public uint TexelIDTextureHeight { get; private set; }

    private ComputePipeline zeroBufferPipeline;

    //public Texture PositionTexture { get; private set; } = null!;

    public RenderTargetManager RenderTargets { get; }

    public GraphicsManager(Window window, IAssetSource assets)
    {
        this.Window = window;
        this.assets = assets;

        Properties deviceProps = new();
        
        deviceProps.SetBoolean(Device.PropertyCreateDebugModeBoolean, true);
        deviceProps.SetBoolean(Device.PropertyCreateShadersDXILBoolean, true);
        deviceProps.SetBoolean(Device.PropertyCreatePreferLowPowerBoolean, false);

        this.device = new Device(deviceProps);

        // var deviceProperties = this.device.GetProperties();
        // Console.WriteLine("device: " + deviceProperties.GetString(SDL.Native.Functions.SDL_PROP_GPU_DEVICE_NAME_STRING, default));

        device.ClaimWindow(window);
        device.SetSwapchainParameters(window, SwapchainComposition.SDRLinear, PresentMode.Immediate);

        shaders = new(device, assets);

        //transferBatcher = new(device, 64 * 1024 * 1024);

        CommandBuffer = null!;

        // BlockMeshRenderer = new(this);
        // ChunkRenderer = new(this);
        // GUIRenderer = new(this);
        // OverlayRenderer = new(this);

        zeroBufferPipeline = shaders.GetComputePipeline("zero_buffer");

        RenderTargets = new(device);
    }

    public void AcquireCommandBuffer()
    {
        CommandBuffer = device.AcquireCommandBuffer();
    }

    public bool BeginFrame()
    {
        return RenderTargets.AcquireSwapchainTexture(CommandBuffer, Window);
    }


    public void EndFrame()
    {
        this.RenderTargets.SwapchainTexture = null!;
        CommandBuffer.Submit();
    }

    public TextureData LoadTextureData(string assetName)
    {
        TextureData result = default;
        byte[] data = assets.Load(assetName + ".texture");
        result.Width = MemoryMarshal.Read<int>(data.AsSpan(0..4));
        result.Height = MemoryMarshal.Read<int>(data.AsSpan(4..8));
        result.Data = data[8..];
        return result;
    }


    public Texture LoadTexture(string assetName)
    {
        return CreateTexture(LoadTextureData(assetName));
    }

    public Texture CreateTexture(TextureData data)
    {
        Texture texture = device.CreateTexture(new()
        {
            Type = TextureType._2D,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsageFlags.Sampler | TextureUsageFlags.ColorTarget,
            Width = (uint)data.Width,
            Height = (uint)data.Height,
            LayerCountOrDepth = 1,
            NumLevels = 5,
            SampleCount = SampleCount._1,
        });

        using TransferBuffer transferBuffer = device.CreateTransferBuffer(TransferBufferUsage.Upload, (uint)(4 * data.Width * data.Height));
        
        Span<byte> mappedData = transferBuffer.Map(false);
        data.Data.CopyTo(mappedData);
        transferBuffer.Unmap();

        CopyPass pass = CommandBuffer.BeginCopyPass();
        TextureTransferInfo source = new()
        {
            TransferBuffer = transferBuffer,
            Offset = 0,
            PixelsPerRow = (uint)data.Width,
            RowsPerLayer = (uint)data.Height,
        };
        TextureRegion destination = new()
        {
            Texture = texture,
            W = (uint)data.Width,
            H = (uint)data.Height,
            D = 1,
        };
        pass.UploadToTexture(source, destination, false);
        pass.End();

        return texture;
    }

    /// <summary>
    /// note: offset &amp; length rounded down to nearest word
    /// </summary>
    public void ClearDataBufferRange(DataBuffer buffer, uint offset, uint length, bool cycle)
    {
        const int bytesPerGroup = (4 * 64);
        ComputePass pass = CommandBuffer.BeginComputePass([], [new StorageBufferReadWriteBinding(buffer, cycle)]);
        
        ClearDataBufferArgs args = new() { uint4_offset = offset / 4, uint4_length = length / 4 };
        CommandBuffer.PushComputeUniformData(0, ref args);

        pass.BindPipeline(zeroBufferPipeline);
        pass.Dispatch((buffer.Size + bytesPerGroup - 1) / bytesPerGroup, 1, 1);
        pass.End();
    }

    struct ClearDataBufferArgs
    {
        public uint uint4_offset;
        public uint uint4_length;
    }

}
