using BlockGame42.Chunks;
using BlockGame42.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Blocks;
internal class DynamicBlockModel : BlockModel
{
    Material textureSet;
    DynamicBlockMeshManager meshes;

    public DynamicBlockModel(Material textureSet)
    {
        this.textureSet = textureSet;
        meshes = new(Game.graphics, this);
    }

    public override Material GetMaterial(BlockState state)
    {
        return textureSet;
    }

    //public override uint GetTextureID(BlockState state, Direction direction)
    //{
    //    return textureId;
    //}

    public override BlockFaceMask GetFaceMask(BlockState state, Direction direction)
    {
        BlockFaceMask result = 0;
        switch (direction)
        {
            case Direction.East:
                if (state.DynamicBlock[0b001]) result |= BlockFaceMask.BottomRight;
                if (state.DynamicBlock[0b011]) result |= BlockFaceMask.BottomLeft;
                if (state.DynamicBlock[0b101]) result |= BlockFaceMask.TopRight;
                if (state.DynamicBlock[0b111]) result |= BlockFaceMask.TopLeft;
                break;
            case Direction.South:
                if (state.DynamicBlock[0b010]) result |= BlockFaceMask.BottomLeft;
                if (state.DynamicBlock[0b011]) result |= BlockFaceMask.BottomRight;
                if (state.DynamicBlock[0b110]) result |= BlockFaceMask.TopLeft;
                if (state.DynamicBlock[0b111]) result |= BlockFaceMask.TopRight;
                break;
            case Direction.West:
                if (state.DynamicBlock[0b000]) result |= BlockFaceMask.BottomLeft;
                if (state.DynamicBlock[0b010]) result |= BlockFaceMask.BottomRight;
                if (state.DynamicBlock[0b100]) result |= BlockFaceMask.TopLeft;
                if (state.DynamicBlock[0b110]) result |= BlockFaceMask.TopRight;
                break;
            case Direction.North:
                if (state.DynamicBlock[0b000]) result |= BlockFaceMask.BottomRight;
                if (state.DynamicBlock[0b001]) result |= BlockFaceMask.BottomLeft;
                if (state.DynamicBlock[0b100]) result |= BlockFaceMask.TopRight;
                if (state.DynamicBlock[0b101]) result |= BlockFaceMask.TopLeft;
                break;
            case Direction.Up:
                if (state.DynamicBlock[0b100]) result |= BlockFaceMask.TopLeft;
                if (state.DynamicBlock[0b101]) result |= BlockFaceMask.TopRight;
                if (state.DynamicBlock[0b110]) result |= BlockFaceMask.BottomLeft;
                if (state.DynamicBlock[0b111]) result |= BlockFaceMask.BottomRight;
                break;
            case Direction.Down:
                if (state.DynamicBlock[0b000]) result |= BlockFaceMask.TopRight;
                if (state.DynamicBlock[0b001]) result |= BlockFaceMask.TopLeft;
                if (state.DynamicBlock[0b010]) result |= BlockFaceMask.BottomRight;
                if (state.DynamicBlock[0b011]) result |= BlockFaceMask.BottomLeft;
                break;
        }
        return result;

        //BlockFaceMask result = 0;
        //for (int i = 0; i < 4; i++)
        //{
        //    for (int j = 0; j < 4; j++)
        //    {
        //        bool occupied;
        //        if (direction == Direction.Up)
        //        {
        //            occupied = state.DynamicBlock[i, 3, j];
        //        }
        //        else if (direction == Direction.Down)
        //        {
        //            occupied = state.DynamicBlock[i, 0, j];
        //        }
        //        else if (direction == Direction.East)
        //        {
        //            occupied = state.DynamicBlock[3, 3 - j, 3 - i];
        //        }
        //        else if (direction == Direction.South)
        //        {
        //            occupied = state.DynamicBlock[i, 3 - j, 3];
        //        }
        //        else if (direction == Direction.North)
        //        {
        //            occupied = state.DynamicBlock[3 - i, 3 - j, 0];
        //        }
        //        else // Direction.West
        //        {
        //            occupied = state.DynamicBlock[0, 3 - j, i];
        //        }
        //        //occupied = this[i, 0, j];
        //        if (occupied)
        //        {
        //            result |= (BlockFaceMask)(1 << j * 4 + i);
        //        }
        //    }
        //}
        //return result;
    }

    public override BlockMesh GetMesh(BlockState state, out Matrix4x4 transform)
    {
        transform = Matrix4x4.Identity;
        return meshes.Get(state);
    }

    public override void AddInternalFaces(BlockState state, BlockMeshBuilder mesh)
    {
        for (int i = 0; i < 6; i++)
        {
            Coordinates right = ChunkMesh.rightDirs[i];
            Coordinates up = ChunkMesh.upDirs[i];
            Coordinates forward = ChunkMesh.forwardDirs[i];
            Coordinates offset = ChunkMesh.faceOffsets[i];

            Direction direction = (Direction)i;

            BlockFaceMask exposedFaces = ChunkMesh.FlipMaskHorizontal(this.GetFaceMask(state, ChunkMesh.directionInverses[i])) & ~this.GetFaceMask(state, direction);

            mesh.AppendPartialBlockFace(
                exposedFaces,
                offset.ToVector() - forward.ToVector() * .5f,
                right.ToVector(),
                up.ToVector(),
                forward.ToVector(),
                GetMaterial(state).Data.Transmission[direction],
                GetMaterial(state).Data.Emission[direction]
            );
        }

        

        /*
        ReadOnlySpan<Coordinates> offsets = [new(0, 1, 0), new(0, 1, 0), new(0, 1, 0), new(0, 1, 0), new(0, 0, 0), new(0, 1, 0)];
        for (int plane = 0; plane < 6; plane++)
        {
            Coordinates right = ChunkMesh.rightDirs[plane];
            Coordinates up = ChunkMesh.upDirs[plane];
            Coordinates forward = ChunkMesh.forwardDirs[plane];
            Coordinates offset = ChunkMesh.faceOffsets[plane] - forward;

            for (int slice = 0; slice < 3; slice++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        Coordinates bit1Coords = slice * forward + x * right + y * up;
                        Coordinates bit2Coords = (slice + 1) * forward + x * right + y * up;
                        bool bit1 = state.DynamicBlock[bit1Coords];
                        bool bit2 = state.DynamicBlock[bit2Coords];
                        if (bit1 != bit2)
                        {
                            mesh.AppendFace(offset.ToVector() + bit2Coords.ToVector() * .25f, right.ToVector() * .25f, up.ToVector() * .25f, new(x * .25f, y * .25f), .25f * Vector2.UnitX, .25f * Vector2.UnitY, textureId);
                        }
                    }
                }
            }
        }
        */
    }

    public override bool Intersect(BlockState state, Box box)
    {
        foreach (var localColliders in GetLocalColliders(state))
        {
            if (localColliders.Intersects(box))
            {
                return true;
            }
        }

        return false;
    }

    private List<Box> GetLocalColliders(BlockState state)
    {
        if (state.DynamicBlock.Mask == 0xFF)
            return [new Box(Vector3.Zero, Vector3.One)];

        List<Box> result = [];
        // 0bYZX
        if (state.DynamicBlock[0b000]) result.Add(new Box(new(0.0f, 0.0f, 0.0f), new(0.5f, 0.5f, 0.5f)));
        if (state.DynamicBlock[0b001]) result.Add(new Box(new(0.5f, 0.0f, 0.0f), new(1.0f, 0.5f, 0.5f)));
        if (state.DynamicBlock[0b010]) result.Add(new Box(new(0.0f, 0.0f, 0.5f), new(0.5f, 0.5f, 1.0f)));
        if (state.DynamicBlock[0b011]) result.Add(new Box(new(0.5f, 0.0f, 0.5f), new(1.0f, 0.5f, 1.0f)));
        if (state.DynamicBlock[0b100]) result.Add(new Box(new(0.0f, 0.5f, 0.0f), new(0.5f, 1.0f, 0.5f)));
        if (state.DynamicBlock[0b101]) result.Add(new Box(new(0.5f, 0.5f, 0.0f), new(1.0f, 1.0f, 0.5f)));
        if (state.DynamicBlock[0b110]) result.Add(new Box(new(0.0f, 0.5f, 0.5f), new(0.5f, 1.0f, 1.0f)));
        if (state.DynamicBlock[0b111]) result.Add(new Box(new(0.5f, 0.5f, 0.5f), new(1.0f, 1.0f, 1.0f)));
        return result;
    }

    public override bool Raycast(BlockState state, Ray ray, ref float t, ref Coordinates normal)
    {
        var localColliders = GetLocalColliders(state);

        t = float.PositiveInfinity;
        foreach (var collider in localColliders)
        {
            if (collider.Raycast(ray, out float tNear, out float tFar, out Coordinates hitNormal))
            {
                float hitT = tNear > 0 ? tNear : tFar;
                if (hitT > 0 && hitT < t)
                {
                    t = hitT;
                    normal = hitNormal;
                }
            }
        }
        return t != float.PositiveInfinity;
    }

    public override ulong GetVolumeMask(BlockState state)
    {
        return state.DynamicBlock.GetBlockMask64();
    }
}

class DynamicBlockMeshManager
{
    private readonly GraphicsManager graphics;
    private readonly BlockModel model;

    BlockMesh?[] meshes = new BlockMesh?[256];

    public DynamicBlockMeshManager(GraphicsManager graphics, BlockModel model)
    {
        this.graphics = graphics;
        this.model = model;
    }

    public BlockMesh Get(BlockState state)
    {
        BlockMesh? mesh = meshes[state.DynamicBlock.Mask];
        if (mesh == null)
        {
            mesh = BlockMesh.CreateFromModel(graphics, model, state);
            meshes[state.DynamicBlock.Mask] = mesh;
        }
        return mesh;
    }
}