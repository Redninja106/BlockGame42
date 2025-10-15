using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlockGame42.Rendering;
internal class ShaderManager
{
    private Dictionary<string, Shader> shaders = [];
    private Dictionary<string, ComputePipeline> computePipelines = [];
    private readonly Device device;
    private readonly IAssetSource assets;

    public ShaderManager(Device device, IAssetSource assets)
    {
        this.device = device;
        this.assets = assets;
    }

    public Shader Get(string shaderName)
    {
        if (shaders.TryGetValue(shaderName, out Shader? shader))
        {
            return shader;
        }

        byte[] bytecode = assets.Load(shaderName + ".cso");

        JsonDocument metadataDocument = JsonDocument.Parse(assets.LoadText(shaderName + ".metadata.json"));
        JsonElement metadata = metadataDocument.RootElement;

        shader = device.CreateShader(new ShaderCreateInfo()
        {
            Code = bytecode,
            EntryPoint = metadata.GetProperty("entryPoint").GetString()!,
            Format = ShaderFormat.DXIL,
            Stage = Enum.Parse<ShaderStage>(metadata.GetProperty("stage").GetString()!, true),
            NumSamplers = metadata.GetProperty("numSamplers").GetUInt32(),
            NumStorageBuffers = metadata.GetProperty("numStorageBuffers").GetUInt32(),
            NumStorageTextures = metadata.GetProperty("numStorageTextures").GetUInt32(),
            NumUniformBuffers = metadata.GetProperty("numUniformBuffers").GetUInt32(),
        });

        shaders[shaderName] = shader;
        return shader;
    }

    public ComputePipeline GetComputePipeline(string shaderName)
    {
        if (computePipelines.TryGetValue(shaderName, out var computePipeline))
        {
            return computePipeline;
        }

        byte[] bytecode = assets.Load(shaderName + ".cso");

        JsonDocument metadataDocument = JsonDocument.Parse(assets.LoadText(shaderName + ".metadata.json"));
        JsonElement metadata = metadataDocument.RootElement;

        computePipeline = device.CreateComputePipeline(new()
        {
            Code = bytecode,
            EntryPoint = metadata.GetProperty("entryPoint").GetString()!,
            Format = ShaderFormat.DXIL,
            NumSamplers = metadata.GetProperty("numSamplers").GetUInt32(),
            NumReadonlyStorageBuffers = metadata.GetProperty("numStorageBuffers").GetUInt32(),
            NumReadonlyStorageTextures = metadata.GetProperty("numStorageTextures").GetUInt32(),
            NumUniformBuffers = metadata.GetProperty("numUniformBuffers").GetUInt32(),
            NumReadWriteStorageBuffers = metadata.GetProperty("numRWStorageBuffers").GetUInt32(),
            NumReadWriteStorageTextures = metadata.GetProperty("numRWStorageTextures").GetUInt32(),
            ThreadCountX = metadata.GetProperty("threadCountX").GetUInt32(),
            ThreadCountY = metadata.GetProperty("threadCountY").GetUInt32(),
            ThreadCountZ = metadata.GetProperty("threadCountZ").GetUInt32(),
        });

        computePipelines[shaderName] = computePipeline;
        return computePipeline;
    }
}
