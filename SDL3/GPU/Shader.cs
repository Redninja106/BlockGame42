using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDL.GPU;
public unsafe class Shader
{
    public SDL_GPUShader* handle;
    public nint Handle => (nint)handle;

    public Shader(nint handle)
    {
        this.handle = (SDL_GPUShader*)handle;
    }
}
