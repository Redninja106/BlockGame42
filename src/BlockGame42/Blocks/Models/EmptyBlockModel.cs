using BlockGame42.Chunks;
using BlockGame42.Rendering;

namespace BlockGame42.Blocks.Models;

class EmptyBlockModel : BlockModel
{
    public EmptyBlockModel()
    {
    }

    public override BlockFaceMask GetFaceMask(BlockState state, Direction direction)
    {
        return BlockFaceMask.Empty;
    }

    public override void AddInternalFaces(BlockState state, BlockMeshBuilder mesh)
    {
    }

    public override BlockMesh CreateMesh(GraphicsContext graphics, BlockState state, out Matrix4x4 transform)
    {
        transform = default;
        return null!;
    }

    public override ulong GetVolumeMask(BlockState state)
    {
        return 0x0;
    }
}
