using BlockGame42.Blocks.Materials;
using BlockGame42.Chunks;
using BlockGame42.Rendering;
using Protor;

namespace BlockGame42.Blocks.Models;

abstract class BlockModel : Prototype
{
    public BlockMaterial? Material { get; init; }

    // public abstract BlockTextures CreateMaterial(GraphicsContext graphics, BlockState state);

    public abstract BlockFaceMask GetFaceMask(BlockState state, Direction direction);
    public abstract ulong GetVolumeMask(BlockState state);

    public abstract void AddInternalFaces(BlockState state, BlockMeshBuilder mesh);

    public abstract BlockMesh CreateMesh(GraphicsContext graphics, BlockState state, out Matrix4x4 transform);
}
