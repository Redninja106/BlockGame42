using System;

namespace SDL.GPU;

public ref struct ShaderCreateInfo
{
    public ReadOnlySpan<byte> Code;
    public string EntryPoint;
    public ShaderFormat Format;
    public ShaderStage Stage;
    public uint NumSamplers;
    public uint NumStorageTextures;
    public uint NumStorageBuffers;
    public uint NumUniformBuffers;
}
