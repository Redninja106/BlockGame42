using Interop.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SDL.GPU;

public unsafe sealed class Device : IDisposable
{
    private readonly SDL_GPUDevice* handle;
    public nint Handle => (nint)handle;

    public Device(ShaderFormat formatFlags, bool debug, string? name = null)
        : this(formatFlags, debug, name == null ? default : Encoding.UTF8.GetBytes(name))
    {
    }

    public Device(ShaderFormat formatFlags, bool debug, ReadOnlySpan<byte> name)
    {
        handle = SDL_CreateGPUDevice((uint)formatFlags, debug, CString.FromReadOnlySpan(name));
        if (handle == null)
        {
            throw SDLException.GetError();
        }
    }

    public Device(nint handle)
    {
        this.handle = (SDL_GPUDevice*)handle;
    }

    public void Dispose()
    {
        SDL_DestroyGPUDevice(this.handle);
    }

    public void ClaimWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!SDL_ClaimWindowForGPUDevice(this.handle, (SDL_Window*)window.Handle))
        {
            throw SDLException.GetError();
        }
    }

    public Shader CreateShader(ShaderCreateInfo createInfo)
    {
        fixed (byte* codePtr = createInfo.Code)
        {
            fixed (byte* entrypointPtr = createInfo.EntryPoint.Marshal()) 
            {
                fixed (byte* namePtr = createInfo.EntryPoint.NullableMarshal())
                {
                    SDL_GPUShaderCreateInfo ci = default;
                    ci.code = codePtr;
                    ci.code_size = (ulong)createInfo.Code.Length;
                    ci.entrypoint = entrypointPtr;
                    ci.format = (uint)createInfo.Format;
                    ci.stage = (SDL_GPUShaderStage)createInfo.Stage;
                    ci.num_samplers = createInfo.NumSamplers;
                    ci.num_storage_textures = createInfo.NumStorageTextures;
                    ci.num_storage_buffers = createInfo.NumStorageBuffers;
                    ci.num_uniform_buffers = createInfo.NumUniformBuffers;

                    SDL_GPUShader* shader = SDL_CreateGPUShader(this.handle, &ci);
                    return new Shader((nint)shader);
                }
            }
        }
    }

    public ComputePipeline CreateComputePipeline(ComputePipelineCreateInfo createInfo)
    {
        MarshalAllocator allocator = new(stackalloc byte[1024]);
        fixed (byte* codePtr = createInfo.Code)
        {
            SDL_GPUComputePipelineCreateInfo ci = new()
            {
                code = codePtr,
                code_size = (ulong)createInfo.Code.Length,
                entrypoint = new(allocator.MarshalString(createInfo.EntryPoint)),
                format = (SDL_GPUShaderFormat)(uint)createInfo.Format,
                num_samplers = createInfo.NumSamplers,
                num_readonly_storage_textures = createInfo.NumReadonlyStorageTextures,
                num_readonly_storage_buffers = createInfo.NumReadonlyStorageBuffers,
                num_readwrite_storage_textures = createInfo.NumReadWriteStorageTextures,
                num_readwrite_storage_buffers = createInfo.NumReadWriteStorageBuffers,
                num_uniform_buffers = createInfo.NumUniformBuffers,
                threadcount_x = createInfo.ThreadCountX,
                threadcount_y = createInfo.ThreadCountY,
                threadcount_z = createInfo.ThreadCountZ,
                props = createInfo.Properties?.propertiesID ?? 0
            };

            SDL_GPUComputePipeline* computePipelineHandle = SDL_CreateGPUComputePipeline(this.handle, &ci);

            if (computePipelineHandle == null)
            {
                throw SDLException.GetError();
            }

            return new ComputePipeline((nint)computePipelineHandle, (nint)this.handle);
        }
    }

    public GraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineCreateInfo createInfo)
    {
        ArgumentNullException.ThrowIfNull(createInfo.VertexShader);
        ArgumentNullException.ThrowIfNull(createInfo.FragmentShader);

        scoped MarshalAllocator allocator = new(stackalloc byte[2048]);
        SDL_GPUGraphicsPipelineCreateInfo ci = createInfo.Marshal(ref allocator);
        
        SDL_GPUGraphicsPipeline* pipelineHandle = SDL_CreateGPUGraphicsPipeline(handle, &ci);
        if (pipelineHandle == null)
        {
            throw SDLException.GetError();
        }

        return new((nint)pipelineHandle, (nint)this.handle);
    }
    public Texture CreateTexture(TextureCreateInfo createInfo)
    {
        SDL_GPUTextureCreateInfo ci = createInfo.Marshal();

        SDL_GPUTexture* handle = SDL_CreateGPUTexture(this.handle, &ci);
        if (handle == null)
        {
            throw SDLException.GetError();
        }

        return new Texture((nint)handle, (nint)this.handle);
    }

    public CommandBuffer AcquireCommandBuffer()
    {
        var commandBuffer = SDL_AcquireGPUCommandBuffer(this.handle);

        if (commandBuffer == null)
        {
            throw SDLException.GetError();
        }

        return new CommandBuffer((nint)commandBuffer, (nint)this.handle);
    }

    public TransferBuffer CreateTransferBuffer(TransferBufferUsage usage, uint size, Properties? properties = null)
    {
        SDL_GPUTransferBufferCreateInfo ci = new()
        {
            size = size,
            usage = (SDL_GPUTransferBufferUsage)usage,
            props = properties?.propertiesID ?? 0,
        };

        SDL_GPUTransferBuffer* transferBufferHandle = SDL_CreateGPUTransferBuffer(this.handle, &ci);
        
        if (transferBufferHandle == null)
        {
            throw SDLException.GetError();
        }

        return new TransferBuffer((nint)transferBufferHandle, (nint)this.handle, size);
    }
    
    public DataBuffer CreateDataBuffer<T>(DataBufferUsageFlags usage, int length)
        where T : unmanaged
    {
        return CreateDataBuffer(usage, (uint)(length * Unsafe.SizeOf<T>()));
    }

    public DataBuffer CreateDataBuffer(DataBufferUsageFlags usage, uint size)
    {
        SDL_GPUBufferCreateInfo ci = new()
        {
            size = size,
            usage = (SDL_GPUBufferUsageFlags)(uint)usage
        };

        SDL_GPUBuffer* bufferHandle = SDL_CreateGPUBuffer(this.handle, &ci);
        if (bufferHandle == null)
        {
            throw SDLException.GetError();
        }

        return new DataBuffer((nint)bufferHandle, (nint)this.handle, size);
    }

    public TextureFormat GetSwapchainTextureFormat(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        TextureFormat result = (TextureFormat)SDL_GetGPUSwapchainTextureFormat(this.handle, (SDL_Window*)window.Handle);

        if (result == TextureFormat.Invalid)
        {
            throw SDLException.GetError();
        }

        return result;
    }

    public Sampler CreateSampler(SamplerCreateInfo createInfo)
    {
        SDL_GPUSamplerCreateInfo ci = createInfo.Marshal();

        SDL_GPUSampler* handle = SDL_CreateGPUSampler(this.handle, &ci);
        if (handle == null)
        {
            throw SDLException.GetError();
        }
        
        return new Sampler((nint)handle, (nint)this.handle);
    }

    public void SetSwapchainParameters(Window window, SwapchainComposition swapchainComposition, PresentMode presentMode)
    {
        if (!SDL_SetGPUSwapchainParameters(this.handle, (SDL_Window*)window.Handle, (SDL_GPUSwapchainComposition)swapchainComposition, (SDL_GPUPresentMode)presentMode))
        {
            throw SDLException.GetError();
        }
    }
}
