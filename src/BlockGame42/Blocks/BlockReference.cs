using BlockGame42.Chunks;

namespace BlockGame42.Blocks;

readonly ref struct BlockReference
{
    private readonly World World;
    public readonly Coordinates ChunkCoordinates;
    public readonly Coordinates WorldCoordinates;

    public readonly ref Block Block;
    public readonly ref BlockState State;
    public readonly ref byte Support;
    public readonly ref byte Mask;

    public BlockReference(World world, Coordinates chunkCoordinates, Coordinates worldCoordinates, ref Block block, ref BlockState state, ref byte support, ref byte mask)
    {
        World = world;
        ChunkCoordinates = chunkCoordinates;
        WorldCoordinates = worldCoordinates;
        Block = ref block;
        State = ref state;
        Support = ref support;
        Mask = ref mask;
    }

    public readonly BlockReference Offset(Coordinates offsetInBlocks)
    {
        return World.GetBlockReference(this.WorldCoordinates + offsetInBlocks);
    }

    public Chunk? GetChunk()
    {
        return World.Chunks.chunkMap.GetValueOrDefault(ChunkCoordinates).Item1;
    }
}
