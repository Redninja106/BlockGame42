using Interop.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDL;

[Flags]
public enum WindowFlags : ulong
{
    None = 0ul,
    AlwaysOnTop = 65536uL,
    Borderless = 16uL,
    External = 2048uL,
    Fullscreen = 1uL,
    Hidden = 8uL,
    HighPixelDensity = 8192uL,
    InputFocus= 512uL,
    KeyboardGrabbed = 1048576uL,
    Maximized = 128uL,
    Metal = 536870912uL,
    Minimized = 64uL,
    Modal = 4096uL,
    MouseCapture = 16384uL,
    MouseFocus = 1024uL,
    MouseGrabbed = 256uL,
    MouseRelativeMode = 32768uL,
    NotFocusable = 2147483648uL,
    Occluded = 4uL,
    OpenGL = 2uL,
    PopupMenu = 524288uL,
    Resizable = 32uL,
}

public unsafe class Window
{
    SDL_Window* handle;

    public nint Handle => (nint)handle;

    public Window(string title, int width, int height, WindowFlags flags = WindowFlags.None)
        : this(Encoding.UTF8.GetBytes(title ?? throw new ArgumentNullException(nameof(title))), width, height, flags)
    {
    }

    public Window(ReadOnlySpan<byte> title, int width, int height, WindowFlags flags = WindowFlags.None)
    {
        handle = SDL_CreateWindow(CString.FromReadOnlySpan(title), width, height, (SDL_WindowFlags)(ulong)flags);
    }

    public int Width
    {
        get
        {
            int width;
            if (!SDL_GetWindowSize(handle, &width, null))
            {
                throw SDLException.GetError();
            }
            return width;
        }
    }

    public int Height
    {
        get
        {
            int height;
            if (!SDL_GetWindowSize(handle, null, &height))
            {
                throw SDLException.GetError();
            }
            return height;
        }
    }

    public void SetRelativeMouseMode(bool enabled)
    {
        if (!SDL_SetWindowRelativeMouseMode(this.handle, enabled))
        {
            throw SDLException.GetError();
        }
    }

    public bool GetRelativeMouseMode()
    {
        return SDL_GetWindowRelativeMouseMode(this.handle);
    }

    public bool HasMouseFocus()
    {
        return SDL_GetMouseFocus() == this.handle;
    }

    public bool HasKeyboardFocus()
    {
        return SDL_GetKeyboardFocus() == this.handle;
    }

    public void SetTitle(string title)
    {
        MarshalAllocator allocator = new(stackalloc byte[1024]);
        
        if (!SDL_SetWindowTitle(this.handle, CString.FromReadOnlySpan(allocator.MarshalString(title))))
        {
            throw SDLException.GetError();
        }
    }
}
