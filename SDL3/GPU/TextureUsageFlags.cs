using System;

namespace SDL.GPU;

[Flags]
public enum TextureUsageFlags : uint
{
    Sampler = (1u << 0),
    ColorTarget = (1u << 1),
    DepthStencilTarget = (1u << 2),
    GraphicsStorageRead = (1u << 3),
    ComputeStorageRead = (1u << 4),
    ComputeStorageWrite= (1u << 5),
    ComputeStorageSimultaneousReadWrite = (1u << 6),
}
