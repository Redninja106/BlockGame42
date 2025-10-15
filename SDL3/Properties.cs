using Interop.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDL;
public class Properties : IDisposable
{
    internal SDL_PropertiesID propertiesID;

    public Properties()
    {
        propertiesID = SDL_CreateProperties();
    }

    ~Properties()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (propertiesID != 0)
        {
            SDL_DestroyProperties(propertiesID);
            propertiesID = 0;
        }
        GC.SuppressFinalize(this);
    }
    public void SetFloat(string name, float value)
    {
        MarshalAllocator allocator = new(stackalloc byte[1024]);
        ReadOnlySpan<byte> nameUtf8 = allocator.MarshalString(name);
        SetFloat(nameUtf8, value);
    }
    public void SetFloat(ReadOnlySpan<byte> name, float value)
    {
        SDL_SetFloatProperty(propertiesID, new CString(name), value);
    }
    public void SetString(string name, string value)
    {
        MarshalAllocator allocator = new(stackalloc byte[1024]);
        ReadOnlySpan<byte> nameUtf8 = allocator.MarshalString(name);
        ReadOnlySpan<byte> valueUtf8 = allocator.MarshalString(value);
        SDL_SetStringProperty(propertiesID, nameUtf8, valueUtf8);
    }

    public void SetString(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        SDL_SetStringProperty(propertiesID, name, value);
    }
}
