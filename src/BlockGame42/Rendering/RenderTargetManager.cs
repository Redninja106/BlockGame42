using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class RenderTargetManager
{
    public uint Width { get; private set; }
    public uint Height { get; private set; }

    private readonly Device device;

    public Texture SwapchainTexture;

    public Texture PositionTexture;
    public Texture AlbedoTexture;
    public Texture NormalTexture;
    public Texture SpecularTexture;

    public Texture DepthStencilTexture;

    public RenderTargetManager(Device device)
    {
        this.device = device;
    }

    public void Resize(uint width, uint height)
    {
        Width = width;
        Height = height;
    }

    public bool AcquireSwapchainTexture(CommandBuffer commandBuffer, Window window)
    {
        Texture? swapchainTexture = commandBuffer.WaitAndAcquireSwapchainTexture(window, out uint swapchainWidth, out uint swapchainHeight);
        if (swapchainTexture == null)
        {
            return false;
        }

        if (swapchainWidth != Width && swapchainHeight != Height)
        {
            Width = swapchainWidth;
            Height = swapchainHeight;

            var depthStencilProps = new Properties();
            depthStencilProps.SetFloat(Texture.CreateD3D12ClearDepthFloatProperty, 1.0f);
            ResizeTexture(
                ref DepthStencilTexture, 
                TextureFormat.D24_UNorm_S8_UInt, 
                TextureUsageFlags.DepthStencilTarget, 
                depthStencilProps
                );

            ResizeTexture(
                ref PositionTexture, 
                TextureFormat.R32G32B32A32_Float, 
                TextureUsageFlags.ColorTarget | TextureUsageFlags.ComputeStorageRead
                );

            ResizeTexture(
                ref AlbedoTexture,
                TextureFormat.R8G8B8A8_UNorm,
                TextureUsageFlags.ColorTarget | TextureUsageFlags.ComputeStorageRead
                );

            ResizeTexture(
                ref NormalTexture,
                TextureFormat.R8G8B8A8_UNorm,
                TextureUsageFlags.ColorTarget | TextureUsageFlags.ComputeStorageRead
                );

            ResizeTexture(
                ref SpecularTexture, 
                TextureFormat.R8G8B8A8_UNorm, 
                TextureUsageFlags.ColorTarget | TextureUsageFlags.ComputeStorageRead
                );
        }

        this.SwapchainTexture = swapchainTexture;
        return true;
    }

    void ResizeTexture([NotNull] ref Texture? texture, TextureFormat format, TextureUsageFlags usage, Properties? properties = null)
    {
        texture?.Dispose();
        texture = device.CreateTexture(new()
        {
            Type = TextureType._2D,
            Format = format,
            Usage = usage,
            Width = this.Width,
            Height = this.Height,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SampleCount._1,
            Properties = properties
        });
    }

    public void Clear(CommandBuffer commandBuffer)
    {
        ColorTargetInfo swapchainTarget = new()
        {
            Texture = SwapchainTexture,

            ClearColor = new(.392f, .584f, .929f, 1),
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
        };

        ColorTargetInfo positionTarget = new()
        {
            Texture = PositionTexture,

            ClearColor = new(0, 0, 0, 0),
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
        }; 
        
        ColorTargetInfo albedoTarget = new()
        {
            Texture = AlbedoTexture,

            ClearColor = new(0, 0, 0, 0),
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
        };

        ColorTargetInfo normalTarget = new()
        {
            Texture = NormalTexture,

            ClearColor = new(0, 0, 0, 0),
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
        };

        ColorTargetInfo specularTarget = new()
        {
            Texture = SpecularTexture,

            ClearColor = new(0, 0, 0, 0),
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
        };

        DepthStencilTargetInfo depthTarget = new()
        {
            Texture = DepthStencilTexture,

            ClearDepth = 1,
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,

            ClearStencil = 0,
            StencilLoadOp = LoadOp.Clear,
            StencilStoreOp = StoreOp.Store
        };

        RenderPass pass = commandBuffer.BeginRenderPass([swapchainTarget, albedoTarget, normalTarget, specularTarget], depthTarget);
        pass.End();

    }
}
