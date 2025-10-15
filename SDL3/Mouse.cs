using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SDL;
public unsafe class Mouse
{
    public static MouseButtonFlags GetState(out float x, out float y)
    {
        fixed (float* xPtr = &x)            
        {
            fixed (float* yPtr = &y)
            {
                return (MouseButtonFlags)(uint)SDL_GetMouseState(xPtr, yPtr);
            }
        }
    }

    public static MouseButtonFlags GetRelativeState(out float x, out float y)
    {
        fixed (float* xPtr = &x)
        {
            fixed (float* yPtr = &y)
            {
                return (MouseButtonFlags)(uint)SDL_GetRelativeMouseState(xPtr, yPtr);
            }
        }
    }

    public static bool TryCapture(bool enabled)
    {
        return SDL_CaptureMouse(enabled);
    }

    public static void Capture(bool enabled)
    {
        if (!SDL_CaptureMouse(enabled))
        {
            throw SDLException.GetError();
        }
    }
}

[Flags]
public enum MouseButtonFlags
{
    Left = 1 << 0,
    Middle = 1 << 1,
    Right = 1 << 2,
    X1 = 1 << 3,
    X2 = 1 << 4,
}

