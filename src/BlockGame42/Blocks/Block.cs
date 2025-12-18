using BlockGame42.Blocks.Models;
using BlockGame42.Chunks;
using Protor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Blocks;

abstract class Block : Prototype
{
    public abstract BlockPlacementHandler PlacementHandler { get; }
    public BlockModel Model { get; set; }

    public virtual BlockState DefaultState { get; } = default;

    public Item Item { get; }

    public BlockStrength Strength { get; init; }

    public Block()
    {
        Item = new BlockItem(this);
    }

    public virtual bool Intersect(BlockState state, Box box)
    {
        return true;
    }

    public virtual bool Raycast(BlockState state, Ray ray, ref float t, ref Coordinates normal)
    {
        return true;
    }

    public virtual void OnInteract(PlayerEntity player, in BlockReference block)
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
            if (chunk.Blocks[offset] != BlockRegistry.Air)
            {
                Random.Shared.Shuffle(offsets);
                bool moved = false;
                for (int i = 0; i < 4; i++)
                {
                    Coordinates side = localLocation + offsets[i];
                    Chunk? sideChunk = neighborhood.At(side, out Coordinates sideOff); 
                    Coordinates sideDown = side + Coordinates.Down;
                    Chunk? sideDownChunk = neighborhood.At(side, out Coordinates sideDownOff);
                    if (sideChunk?.Blocks[sideOff] == BlockRegistry.Air && sideDownChunk?.Blocks[sideDownOff] == BlockRegistry.Air)
                    {
                        chunk.Blocks[offset] = BlockRegistry.Air;
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
            chunk.Blocks[offset] = BlockRegistry.Air;

            Coordinates above = localLocation + new Coordinates(0, 1, 0);
            world.UpdateBlock(neighborhood.LocalToWorld(above));
        }
    }

    protected void TryFall2(World world, in BlockReference block)
    {
        if (block.Offset(Coordinates.Down).Prototype == BlockRegistry.Air)
        {
            world.AddEntity(new BlockEntity(world, this, block.State, block.WorldCoordinates));
            block.Set(BlockRegistry.Air);
            world.UpdateBlock(block.WorldCoordinates + Coordinates.Up);
            return;
        }

        Random.Shared.Shuffle(offsets);
        for (int i = 0; i < 4; i++)
        {
            var side = block.Offset(offsets[i]);
            if (side.Prototype == BlockRegistry.Air)
            {
                if (side.Offset(Coordinates.Down).Prototype == BlockRegistry.Air)
                {
                    world.AddEntity(new BlockEntity(world, this, block.State, side.WorldCoordinates));
                    block.Set(BlockRegistry.Air);

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
