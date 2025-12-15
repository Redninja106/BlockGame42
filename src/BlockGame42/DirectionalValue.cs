using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BlockGame42;

struct DirectionalValue<T>
{
    public T East;
    public T South;
    public T West;
    public T North;
    public T Up;
    public T Down;

    [UnscopedRef]
    public ref T this[Direction direction]
    {
        get => ref Unsafe.Add(ref East, (int)direction);
    }

    public void Fill(T value)
    {
        this[Direction.East] = value;
        this[Direction.South] = value;
        this[Direction.West] = value;
        this[Direction.North] = value;
        this[Direction.Up] = value;
        this[Direction.Down] = value;
    }
}
