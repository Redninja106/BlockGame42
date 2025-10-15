using BlockGame42.Blocks;
using BlockGame42.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Chunks;
internal class Chunk
{
    public const int Width = 32;
    public const int Height = 32;
    public const int Depth = 32;

    public static readonly Vector3 SizeVector = new(Width, Height, Depth);
    public static readonly Coordinates Size = new(Width, Height, Depth);

    public const int BlockCount = Width * Height * Depth;

    public ChunkAttribute<Block> Blocks { get; } = new();
    public ChunkAttribute<byte> Support { get; } = new();
    // public ChunkAttribute<sbyte> Temperature { get; } = new();
    // public ChunkAttribute<byte> Lighting { get; } = new();
    public ChunkAttribute<BlockState> BlockStates { get; } = new();
    public ChunkAttribute<byte> BlockMasks { get; } = new();

    public List<Entity> Entities { get; set; } = new();

    public int[,] HighestPoints { get; } = new int[Width, Depth];

    // public ChunkSparseAttribute<Inventory> Inventories { get; } = new();
    // public ChunkListAttribute<Entity> Entities { get; } = new();

    public Chunk()
    {
    }

    public static bool InBounds(Coordinates localCoordinates)
    {
        return (uint)localCoordinates.X < Width && (uint)localCoordinates.Y < Height && (uint)localCoordinates.Z < Depth;
    }

    public BlockModel GetModel(Coordinates localCoordinates)
    {
        return Blocks[localCoordinates].Model;
    }

    internal static Coordinates ChunkCoordinatesFromWorldPosition(Vector3 worldPosition)
    {
        Vector3 chunkPosition = worldPosition / SizeVector;
        chunkPosition.X = float.Floor(chunkPosition.X);
        chunkPosition.Y = float.Floor(chunkPosition.Y);
        chunkPosition.Z = float.Floor(chunkPosition.Z);
        return Coordinates.Floor(chunkPosition);
    }

    public static Coordinates BlockToChunkCoordinates(Coordinates blockCoordinates)
    {
        return Coordinates.Floor(blockCoordinates.ToVector() / SizeVector);
    }

    public static void DecomposeCoordinates(Coordinates worldCoordinates, out Coordinates chunkCoordinates, out Coordinates localCoordinates)
    {
        Vector3 chunkPosition = worldCoordinates.ToVector() / SizeVector;
        chunkPosition.X = float.Floor(chunkPosition.X);
        chunkPosition.Y = float.Floor(chunkPosition.Y);
        chunkPosition.Z = float.Floor(chunkPosition.Z);
        chunkCoordinates = Coordinates.Floor(chunkPosition);

        localCoordinates = worldCoordinates - chunkCoordinates * Chunk.Size;
    }

    public void Tick()
    {
        foreach (var entity in Entities.ToArray())
        {
            entity.Tick();
        }
    }

    public void Update()
    {
        foreach (var entity in Entities)
        {
            entity.Update();
        }
    }

    //public Coordinates Raycast(Vector3 localPosition, Vector3 localDirection)
    //{
    //    Coordinates coordinates = localPosition + localDirection;
    //}
}

[StructLayout(LayoutKind.Explicit)]
struct BlockState
{
    [FieldOffset(0)]
    public byte Raw;

    [FieldOffset(0)]
    public HalfBlockState HalfBlock;

    [FieldOffset(0)]
    public DynamicBlockState DynamicBlock;
}

readonly struct HalfBlockState
{
    private const byte DoubledMask = 0x80;
    private const byte DirectionMask = 0x7F;

    // highest bit: is doubled -- rest: direction
    private readonly byte value;

    public bool Doubled => (value & DoubledMask) != 0;
    public Direction Direction => (Direction)(value & DirectionMask);

    public HalfBlockState(bool doubled, Direction direction)
    {
        if (doubled)
        {
            value |= 1 << 7;
        }

        value |= (byte)direction;
    }
}

class ChunkAttribute<T>
{
    // X/Z/Y ordering (meaing array contiguous along x axis)
    private T[] data;

    public bool Stale { get; set; } = true;

    public ChunkAttribute()
    {
        data = new T[Chunk.Height * Chunk.Depth * Chunk.Width];
    }

    public ref T this[int x, int y, int z]
    {
        [DebuggerStepThrough]
        get
        {
            return ref this[new Coordinates(x, y, z)];
        }
    }

    public ref T this[Coordinates localCoordinates]
    {
        [DebuggerStepThrough]
        get
        {
            if (!Chunk.InBounds(localCoordinates))
            {
                throw new Exception($"Local coordinates {localCoordinates} out of bounds");
            }

            return ref data[localCoordinates.Y * Chunk.Width * Chunk.Depth + localCoordinates.Z * Chunk.Width + localCoordinates.X];
        }
    }

    public Span<T> AsSpan()
    {
        return data;
    }
}

struct Coordinates
{
    public int X, Y, Z;

    public static readonly Coordinates One = new(1, 1, 1);

    public static readonly Coordinates East = new(1, 0, 0);
    public static readonly Coordinates South = new(0, 0, 1);
    public static readonly Coordinates West = new(-1, 0, 0);
    public static readonly Coordinates North = new(0, 0, -1);
    public static readonly Coordinates Up = new(0, 1, 0);
    public static readonly Coordinates Down = new(0, -1, 0);

    public Coordinates(int x, int y, int z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    public static Coordinates operator +(Coordinates left, Coordinates right)
    {
        return new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static Coordinates operator -(Coordinates left, Coordinates right)
    {
        return new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static Coordinates operator -(Coordinates coordinates)
    {
        return new(-coordinates.X, -coordinates.Y, -coordinates.Z);
    }

    public static Coordinates operator *(Coordinates left, Coordinates right)
    {
        return new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    public static Coordinates operator *(int left, Coordinates right)
    {
        return new(left * right.X, left * right.Y, left * right.Z);
    }

    public static Coordinates operator *(Coordinates left, int right)
    {
        return new(left.X * right, left.Y * right, left.Z * right);
    }

    public static Coordinates operator /(Coordinates left, Coordinates right)
    {
        return new(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    public static Coordinates operator /(Coordinates left, int right)
    {
        return new(left.X / right, left.Y / right, left.Z / right);
    }

    public static bool operator ==(Coordinates left, Coordinates right)
    {
        return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
    }

    public static bool operator !=(Coordinates left, Coordinates right)
    {
        return left.X != right.X && left.Y != right.Y && left.Z != right.Z;
    }

    public override string ToString()
    {
        return $"<{X}, {Y}, {Z}>";
    }

    public Vector3 ToVector()
    {
        return new(X, Y, Z);
    }

    public static Coordinates FromVector(Vector3 vector)
    {
        return new((int)vector.X, (int)vector.Y, (int)vector.Z);

    }

    public static Coordinates Floor(Vector3 vector)
    {
        return new((int)float.Floor(vector.X), (int)float.Floor(vector.Y), (int)float.Floor(vector.Z));
    }

    public override bool Equals(object obj)
    {
        return obj is Coordinates coords && coords == this;
    }
}
