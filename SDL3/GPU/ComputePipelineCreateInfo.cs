using System;

namespace SDL.GPU;

public ref struct ComputePipelineCreateInfo
{
    public ReadOnlySpan<byte> Code;
    public string EntryPoint;
    public ShaderFormat Format;
    public uint NumSamplers;
    public uint NumReadonlyStorageTextures;
    public uint NumReadonlyStorageBuffers;
    public uint NumReadWriteStorageTextures;
    public uint NumReadWriteStorageBuffers;
    public uint NumUniformBuffers;
    public uint ThreadCountX;
    public uint ThreadCountY;
    public uint ThreadCountZ;
    public Properties? Properties;
}
