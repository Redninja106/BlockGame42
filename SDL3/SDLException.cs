using Interop.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDL;
public class SDLException : Exception
{
    public SDLException(string? message) : base(message)
    {
    }

    public static SDLException GetError()
    {
        return new(CString.ToString(SDL_GetError()));
    }
}
