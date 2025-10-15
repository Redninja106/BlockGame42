using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SDL;

ref struct MarshalAllocator
{
    private readonly Span<byte> stackMemory;
    private int position;

    public MarshalAllocator(Span<byte> stackMemory)
    {
        this.stackMemory = stackMemory;
    }

    public Span<byte> MarshalString(string str)
    {
        Span<byte> bytes = AllocateBytes(Encoding.UTF8.GetByteCount(str) + 1);
        Encoding.UTF8.GetBytes(str.AsSpan(), bytes);
        bytes[^1] = 0;
        return bytes;
    }

    public Span<byte> AllocateBytes(int length)
    {
        if (position + length > stackMemory.Length)
        {
            throw new Exception($"marshal allocator overflow: {position}/{stackMemory.Length} bytes used, {length} more bytes requested");
        }

        Span<byte> bytes = stackMemory.Slice(position, length);
        position += length;
        return bytes;
    }

    public unsafe void* AllocateRaw(int length)
    {
        Span<byte> bytes = AllocateBytes(length);
        fixed (byte* ptr = bytes)
        {
            return (void*)ptr;
        }
    }

    public Span<T> AllocateArray<T>(int length)
        where T : unmanaged
    {
        Span<byte> bytes = AllocateBytes(length * Unsafe.SizeOf<T>());
        return MemoryMarshal.Cast<byte, T>(bytes);
    }

    public Span<TResult> MarshalArray<TMarshallable, TResult>(ReadOnlySpan<TMarshallable> marshallables)
        where TMarshallable : IMarshallable<TResult>
        where TResult : unmanaged
    {
        if (marshallables.IsEmpty)
        {
            return [];
        }

        var results = AllocateArray<TResult>(marshallables.Length);
        for (int i = 0; i < results.Length; i++)
        {
            results[i] = marshallables[i].Marshal(ref this);
        }

        return results;
    }

    public unsafe TResult* MarshalArrayToPointer<TMarshallable, TResult>(ReadOnlySpan<TMarshallable> marshallables)
        where TMarshallable : IMarshallable<TResult>
        where TResult : unmanaged
    {
        fixed (TResult* resultPtr = MarshalArray<TMarshallable, TResult>(marshallables))
        {
            return resultPtr;
        }
    }
}
