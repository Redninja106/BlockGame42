using Interop.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDL;

internal static class Utils
{
    public static void ThrowIfFailed(this CBool sdlFunctionResult)
    {
        if (!sdlFunctionResult)
        {
            throw SDLException.GetError();
        }
    }

    public static byte[] Marshal(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);
        return Encoding.UTF8.GetBytes(str);
    }

    public static byte[]? NullableMarshal(this string? str)
    {
        if (str == null)
        {
            return null;
        }

        return Encoding.UTF8.GetBytes(str);
    }

}
