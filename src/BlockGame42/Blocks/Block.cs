using BlockGame42.Chunks;
using BlockGame42.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Blocks;

struct BlockStrength
{
    public byte Compression;
    public byte Tension;
    public byte Lateral;

    public BlockStrength(byte compressive, byte tension, byte lateral)
    {
        Compression = compressive;
        Tension = tension;
        Lateral = lateral;
    }
}

abstract class Block
{
    public abstract BlockPlacementHandler PlacementHandler { get; }

    public abstract BlockModel Model { get; }

    public BlockStrength Strength { get; }

    public Block(BlockStrength strength)
    {
        this.Strength = strength;
    }

    public virtual void OnInteract(Player player, in BlockReference block)
    {
    }

    public virtual void OnUpdate(World world, in BlockReference block)
    {
        // if (block.Support == 0)
        // {
        //     TryFall2(world, in block);
        // }
    }

    static Coordinates[] offsets = [Coordinates.East, Coordinates.South, Coordinates.West, Coordinates.North];

    protected void TryFall(World world, BlockState state, ChunkNeighborhood neighborhood, Coordinates localLocation)
    {
        Coordinates below = localLocation + new Coordinates(0, -1, 0);
        Chunk? chunk = neighborhood.At(below, out Coordinates offset);
        if (chunk != null)
        {
            if (chunk.Blocks[offset] != Game.Blocks.Air)
            {
                Random.Shared.Shuffle(offsets);
                bool moved = false;
                for (int i = 0; i < 4; i++)
                {
                    Coordinates side = localLocation + offsets[i];
                    Chunk? sideChunk = neighborhood.At(side, out Coordinates sideOff); 
                    Coordinates sideDown = side + Coordinates.Down;
                    Chunk? sideDownChunk = neighborhood.At(side, out Coordinates sideDownOff);
                    if (sideChunk?.Blocks[sideOff] == Game.Blocks.Air && sideDownChunk?.Blocks[sideDownOff] == Game.Blocks.Air)
                    {
                        chunk.Blocks[offset] = Game.Blocks.Air;
                        sideChunk.Blocks[sideOff] = this;
                        sideChunk.BlockStates[sideOff] = state;
                        moved = true;
                        offset = sideOff;
                        chunk = sideChunk;
                        localLocation += offsets[i];
                        break;
                    }
                }
                if (!moved)
                {
                    return;
                }
            }

            world.AddEntity(new BlockEntity(world, this, state, neighborhood.LocalToWorld(localLocation)));
            chunk.Blocks[offset] = Game.Blocks.Air;

            Coordinates above = localLocation + new Coordinates(0, 1, 0);
            world.UpdateBlock(neighborhood.LocalToWorld(above));
        }
    }

    protected void TryFall2(World world, in BlockReference block)
    {
        if (block.Offset(Coordinates.Down).Prototype == Game.Blocks.Air)
        {
            world.AddEntity(new BlockEntity(world, this, block.State, block.WorldCoordinates));
            block.Set(Game.Blocks.Air);
            world.UpdateBlock(block.WorldCoordinates + Coordinates.Up);
            return;
        }

        Random.Shared.Shuffle(offsets);
        for (int i = 0; i < 4; i++)
        {
            var side = block.Offset(offsets[i]);
            if (side.Prototype == Game.Blocks.Air)
            {
                if (side.Offset(Coordinates.Down).Prototype == Game.Blocks.Air)
                {
                    world.AddEntity(new BlockEntity(world, this, block.State, side.WorldCoordinates));
                    block.Set(Game.Blocks.Air);

                    world.UpdateBlock(block.WorldCoordinates + Coordinates.Up);
                    return;
                }
            }
        }
    }

    public virtual void OnRandomTick(World world, in BlockReference block)
    {
    }
}

abstract class BlockModel
{
    public abstract BlockFaceMask GetFaceMask(BlockState state, Direction direction);
    // public abstract uint GetTextureID(BlockState state, Direction direction);

    public abstract Material GetMaterial(BlockState state);

    public abstract bool Intersect(BlockState state, Box box);
    public abstract bool Raycast(BlockState state, Ray ray, ref float t, ref Coordinates normal);

    public abstract void AddInternalFaces(BlockState state, BlockMeshBuilder mesh);

    public abstract BlockMesh GetMesh(BlockState state, out Matrix4x4 transform);

    public abstract ulong GetVolumeMask(BlockState state);
}

[Flags]
enum BlockFaceMask
{
    Empty = 0,
    TopLeft = 0x0033,
    TopCenter = 0x0066,
    TopRight = 0x00CC,
    
    CenterLeft = 0x0330,
    Center = 0x0660,
    CenterRight = 0x0CC0,

    BottomLeft = 0x3300,
    BottomCenter = 0x6600,
    BottomRight = 0xCC00,
    
    TopHalf = 0x00FF,
    LeftHalf = 0x3333,
    RightHalf = 0xCCCC,
    BottomHalf = 0xFF00,
    Full = 0xFFFF,
}

class SolidBlockModel : BlockModel
{
    private Material textureSet;
    private BlockMesh mesh;

    public SolidBlockModel(Material textureSet)
    {
        this.textureSet = textureSet;
        mesh = BlockMesh.CreateFromModel(Game.graphics, this, default);
    }

    public override BlockFaceMask GetFaceMask(BlockState state, Direction direction)
    {
        return BlockFaceMask.Full;
    }

    public override Material GetMaterial(BlockState state)
    {
        return textureSet;
    }

    //public override uint GetTextureID(BlockState state, Direction direction)
    //{
    //    return this.textureId;
    //}

    public override bool Intersect(BlockState state, Box box)
    {
        Box localCollider = new(Vector3.Zero, Vector3.One);
        return localCollider.Intersects(box);
    }

    public override void AddInternalFaces(BlockState state, BlockMeshBuilder mesh)
    {
    }

    public override BlockMesh GetMesh(BlockState state, out Matrix4x4 transform)
    {
        transform = Matrix4x4.Identity;
        return mesh;
    }

    public override bool Raycast(BlockState state, Ray ray, ref float t, ref Coordinates normal)
    {
        Box localCollider = new(Vector3.Zero, Vector3.One);

        if (localCollider.Raycast(ray, out float tNear, out float tFar, out normal))
        {
            if (tNear >= 0)
            {
                t = tNear;
            }
            else
            {
                t = tFar;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public override ulong GetVolumeMask(BlockState state)
    {
        return 0xFFFFFFFFFFFFFFFF;
    }
}

class EmptyBlockModel : BlockModel
{
    Material material;

    public EmptyBlockModel()
    {
        material = Material.CreateUniform(0, 0);
    }

    public override BlockFaceMask GetFaceMask(BlockState state, Direction direction)
    {
        return BlockFaceMask.Empty;
    }

    public override Material GetMaterial(BlockState state)
    {
        return material;
    }

    public override void AddInternalFaces(BlockState state, BlockMeshBuilder mesh)
    {
    }

    public override bool Raycast(BlockState state, Ray ray, ref float t, ref Coordinates normal)
    {
        return false;
    }

    public override bool Intersect(BlockState state, Box box)
    {
        return false;
    }

    public override BlockMesh GetMesh(BlockState state, out Matrix4x4 transform)
    {
        transform = Matrix4x4.Identity;
        return null;
    }

    public override ulong GetVolumeMask(BlockState state)
    {
        return 0x0;
    }
}

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

class EmptyBlock : Block
{
    public override BlockModel Model { get; } = new EmptyBlockModel();
    public override BlockPlacementHandler PlacementHandler => BlockPlacementHandler.Solid;

    public EmptyBlock() : base(new BlockStrength(0, 0, 0))
    {
    }

    public override void OnUpdate(World world, in BlockReference block)
    {
    }

    public override void OnInteract(Player player, in BlockReference block)
    {
    }

    public override void OnRandomTick(World world, in BlockReference block)
    {
    }
}
