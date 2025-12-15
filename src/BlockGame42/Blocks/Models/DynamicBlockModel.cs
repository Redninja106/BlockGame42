using BlockGame42.Chunks;
using BlockGame42.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Blocks.Models;
internal class DynamicBlockModel : BlockModel
{
    MaterialData Textures;
    DynamicBlockMeshManager meshes;

    public DynamicBlockModel()
    {
        //meshes = new(Game.graphics, this);
    }

    public override void InitializePrototype()
    {
        base.InitializePrototype();


    }

    //public override uint GetTextureID(BlockState state, Direction direction)
    //{
    //    return textureId;
    //}

    private static BlockFaceMask[,] faceMaskTable;

    static DynamicBlockModel()
    {
        faceMaskTable = new BlockFaceMask[6, 256];
        for (int d = 0; d < 6; d++)
        {
            for (int s = 0; s < 256; s++)
            {
                BlockFaceMask result = 0;

                BlockState state = default;
                state.DynamicBlock.Mask = (byte)s;

                switch ((Direction)d)
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

                faceMaskTable[d, s] = result;
            }
        }

    }

    public override BlockFaceMask GetFaceMask(BlockState state, Direction direction)
    {
        return faceMaskTable[(int)direction, state.DynamicBlock.Mask];

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

    public override BlockMesh CreateMesh(GraphicsContext graphics, BlockState state, out Matrix4x4 transform)
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
                this.Material.TexID
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

    public override ulong GetVolumeMask(BlockState state)
    {
        return state.DynamicBlock.GetBlockMask64();
    }
}

class DynamicBlockMeshManager
{
    private readonly GraphicsContext graphics;
    private readonly BlockModel model;

    BlockMesh?[] meshes = new BlockMesh?[256];

    public DynamicBlockMeshManager(GraphicsContext graphics, BlockModel model)
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