using System;

namespace SDL.GPU;

[Flags]
public enum ShaderFormat : uint
{

    Invalid = 0,
    Private = 1u << 0,
    SPIRV = 1u << 1,
    DXBC = 1u << 2,
    DXIL = 1u << 3,
    MSL = 1u << 4,
    MetalLib = 1u << 5,
}
