using BlockGame42.Chunks;
using BlockGame42.Rendering;

namespace BlockGame42.Blocks.Models;

class SolidBlockModel : BlockModel
{
    private MaterialData textureSet;
    // private BlockMesh mesh;

    public SolidBlockModel()
    {
        // mesh = BlockMesh.CreateFromModel(null, this, default);
    }

    public override BlockFaceMask GetFaceMask(BlockState state, Direction direction)
    {
        return BlockFaceMask.Full;
    }

    //public override uint GetTextureID(BlockState state, Direction direction)
    //{
    //    return this.textureId;
    //}

    public override void AddInternalFaces(BlockState state, BlockMeshBuilder mesh)
    {
    }

    public override BlockMesh CreateMesh(GraphicsContext graphics, BlockState state, out Matrix4x4 transform)
    {
        transform = Matrix4x4.Identity;
        return BlockMesh.CreateFromModel(graphics, this, state);
    }

    //public override bool Intersect(BlockState state, Box box)
    //{
    //    Box localCollider = new(Vector3.Zero, Vector3.One);
    //    return localCollider.Intersects(box);
    //}

    //public override bool Raycast(BlockState state, Ray ray, ref float t, ref Coordinates normal)
    //{
    //    Box localCollider = new(Vector3.Zero, Vector3.One);

    //    if (localCollider.Raycast(ray, out float tNear, out float tFar, out normal))
    //    {
    //        if (tNear >= 0)
    //        {
    //            t = tNear;
    //        }
    //        else
    //        {
    //            t = tFar;
    //        }

    //        return true;
    //    }
    //    else
    //    {
    //        return false;
    //    }
    //}

    public override ulong GetVolumeMask(BlockState state)
    {
        return 0xFFFFFFFFFFFFFFFF;
    }
}
