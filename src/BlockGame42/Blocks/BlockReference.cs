using BlockGame42.Chunks;

namespace BlockGame42.Blocks;

readonly ref struct BlockReference
{
    private readonly World World;
    public readonly Chunk? Chunk;
    public readonly Coordinates ChunkCoordinates;
    public readonly Coordinates WorldCoordinates;

    public readonly ref Block Prototype;
    public readonly ref BlockState State;
    public readonly ref byte Support;
    public readonly ref ulong Mask;

    public bool IsNull => Chunk == null;

    public BlockReference(World world, Chunk? chunk, Coordinates chunkCoordinates, Coordinates worldCoordinates, ref Block block, ref BlockState state, ref byte support, ref ulong mask)
    {
        World = world;
        Chunk = chunk;
        ChunkCoordinates = chunkCoordinates;
        WorldCoordinates = worldCoordinates;
        Prototype = ref block;
        State = ref state;
        Support = ref support;
        Mask = ref mask;
    }

    public readonly BlockReference Offset(Coordinates offsetInBlocks)
    {
        Coordinates newCoordinates = WorldCoordinates + offsetInBlocks;
        Coordinates newChunkCoordinates = Chunk.WorldToChunkCoordinates(newCoordinates);
        if (newChunkCoordinates == ChunkCoordinates)
        {
            if (Chunk == null)
            {
                return this;
            }

            Coordinates newLocalCoordinates = newCoordinates - Chunk.Size * newChunkCoordinates;
            return new BlockReference(
                this.World,
                this.Chunk,
                this.ChunkCoordinates,
                newCoordinates,
                ref Chunk.Blocks[newLocalCoordinates],
                ref Chunk.BlockStates[newLocalCoordinates],
                ref Chunk.Support[newLocalCoordinates],
                ref Chunk.BlockMasks[newLocalCoordinates]
                );
        }
        return World.GetBlockReference(this.WorldCoordinates + offsetInBlocks);
    }

    public void Set(Block block)
    {
        Set(block, default);
    }

    public void Set(Block block, BlockState state)
    {
        if (Chunk != null)
        {
            Prototype = block;
            State = state;
            Support = block.Strength.Tension;
            Mask = block.Model.GetVolumeMask(State);
        }
    }
}
