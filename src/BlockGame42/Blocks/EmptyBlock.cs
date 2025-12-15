using BlockGame42.Chunks;

namespace BlockGame42.Blocks;

class EmptyBlock : Block
{
    public override BlockPlacementHandler PlacementHandler => BlockPlacementHandler.Solid;

    public EmptyBlock()
    {
    }

    public override bool Intersect(BlockState state, Box box)
    {
        return false;
    }

    public override bool Raycast(BlockState state, Ray ray, ref float t, ref Coordinates normal)
    {
        return false;
    }

    public override void OnUpdate(World world, in BlockReference block)
    {
    }

    public override void OnInteract(PlayerEntity player, in BlockReference block)
    {
    }

    public override void OnRandomTick(World world, in BlockReference block)
    {
    }
}
