using System;

namespace SDL.GPU;

[Flags]
public enum ColorComponentFlags : uint
{
    R = 1u << 0,
    G = 1u << 1,
    B = 1u << 2,
    A = 1u << 3,
}
