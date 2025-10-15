using BlockGame42.Blocks;
using BlockGame42.Rendering;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Chunks;
internal class ChunkMesh
{
    private const int BufferSizeIncrement = 4 * 1024;

    private readonly GraphicsManager graphics;
    public DataBuffer VertexBuffer;
    public DataBuffer BlockMaskBuffer;
    public int VertexCount;

    public ChunkMesh(GraphicsManager graphics)
    {
        this.graphics = graphics;

        VertexBuffer = this.graphics.device.CreateDataBuffer(DataBufferUsageFlags.Vertex, BufferSizeIncrement);
        BlockMaskBuffer = this.graphics.device.CreateDataBuffer(DataBufferUsageFlags.ComputeStorageRead, Chunk.BlockCount);
    }

    static int[] nibbleReverseLookup = [
        0b0000,
        0b1000,
        0b0100,
        0b1100,
        0b0010,
        0b1010,
        0b0110,
        0b1110,
        0b0001,
        0b1001,
        0b0101,
        0b1101,
        0b0011,
        0b1011,
        0b0111,
        0b1111,
        // 0b0000, 0b1000, 0b0100, 0b1100, 0b0010, 0b1010, 0b0110, 0b1110, 0b0001, 0b1001, 0b0101, 0b1101, 0b0011, 0b1011, 0b0111, 0b1111,
        ];
    public static BlockFaceMask FlipMaskHorizontal(BlockFaceMask mask)
    {
        BlockFaceMask result = 0;
        result |= (BlockFaceMask)(nibbleReverseLookup[((int)mask >> 0)  & 0xF] << 0 );
        result |= (BlockFaceMask)(nibbleReverseLookup[((int)mask >> 4)  & 0xF] << 4 );
        result |= (BlockFaceMask)(nibbleReverseLookup[((int)mask >> 8)  & 0xF] << 8 );
        result |= (BlockFaceMask)(nibbleReverseLookup[((int)mask >> 12) & 0xF] << 12);
        return result;
    }

    public static readonly ImmutableArray<Direction> directionInverses = [
        Direction.West,
        Direction.North,
        Direction.East,
        Direction.South,
        Direction.Down,
        Direction.Up,
        ];

    public static readonly ImmutableArray<Coordinates> faceOffsets = [
        new(1, 1, 1),
        new(0, 1, 1),
        new(0, 1, 0),
        new(1, 1, 0),
        new(0, 1, 0),
        new(0, 0, 1),
        ];

    public static readonly ImmutableArray<Coordinates> forwardDirs = [
        new(1, 0, 0), 
        new(0, 0, 1),
        new(-1, 0, 0),
        new(0, 0, -1),
        new(0, 1, 0), 
        new(0, -1, 0),
        ];

    public static readonly ImmutableArray<Coordinates> rightDirs = [
        new(0, 0, -1),
        new(1, 0, 0),
        new(0, 0, 1),
        new(-1, 0, 0),
        new(1, 0, 0),
        new(1, 0, 0),
        ];
    public static readonly ImmutableArray<Coordinates> upDirs = [
        new(0, -1, 0),
        new(0, -1, 0),
        new(0, -1, 0),
        new(0, -1, 0),
        new(0, 0, 1),
        new(0, 0, -1),
        ];
    static readonly ImmutableArray<float> aos = [
        .8f,
        .7f,
        .6f,
        .9f,
        1f,
        .5f,
        ];

    static void EmitBlockFace(in ChunkNeighborhood neighborhood, BlockMeshBuilder mesh, Coordinates coords, BlockModel model, BlockState state, Direction direction)
    {
        // Block block = neighborhood.Center.Blocks[coords];
        // BlockModel model = block.Model;

        Direction inverseDirection = directionInverses[(int)direction];

        if (model.GetFaceMask(state, direction) == BlockFaceMask.Empty)
        {
            return; 
        }
        
        Coordinates coordsOffset = coords + forwardDirs[(int)direction];
        Chunk? chunk = neighborhood.At(coordsOffset, out var blockOffset);
        
        if (chunk == null)
        {
            return;
        }
        
        Block neighborBlock = chunk.Blocks[blockOffset];
        BlockState neighborState = chunk.BlockStates[blockOffset];

        BlockFaceMask visibleFaces = model.GetFaceMask(state, direction) & ~FlipMaskHorizontal(neighborBlock.Model.GetFaceMask(neighborState, inverseDirection));

        if (visibleFaces == 0)
        {
            return;
        }

        Coordinates offset = faceOffsets[(int)direction];
        Coordinates right = rightDirs[(int)direction];
        Coordinates up = upDirs[(int)direction];
        Coordinates normal = forwardDirs[(int)direction];
        float ao = aos[(int)direction];
        if (visibleFaces == BlockFaceMask.Full)
        {
            mesh.AppendBlockFace(offset, right, up, normal, model.GetTextureID(state, direction));
        }
        else
        {
            mesh.AppendPartialBlockFace(visibleFaces, offset.ToVector(), right.ToVector(), up.ToVector(), normal.ToVector(), model.GetTextureID(state, direction), ao);
            //mesh.AppendPartialBlockFace(visibleFaces, neighborhood, coords, offset, right, up, model.TextureIDs[direction], ao);
        }
    }

    public void Build(in ChunkNeighborhood neighborhood)
    {
        scoped BlockMeshBuilder mesh = new();
        mesh.neighborhood = ref neighborhood;

        for (int y = 0; y < Chunk.Height; y++)
        {
            for (int z = 0; z < Chunk.Depth; z++)
            {
                for (int x = 0; x < Chunk.Width; x++)
                {
                    Coordinates coords = new(x, y, z);
                    mesh.coordinates = coords;

                    Block block = neighborhood.Center.Blocks[coords];
                    BlockState state = neighborhood.Center.BlockStates[coords];

                    EmitBlockFace(neighborhood, mesh, coords, block.Model, state, Direction.East);
                    EmitBlockFace(neighborhood, mesh, coords, block.Model, state, Direction.South);
                    EmitBlockFace(neighborhood, mesh, coords, block.Model, state, Direction.West);
                    EmitBlockFace(neighborhood, mesh, coords, block.Model, state, Direction.North);
                    EmitBlockFace(neighborhood, mesh, coords, block.Model, state, Direction.Up);
                    EmitBlockFace(neighborhood, mesh, coords, block.Model, state, Direction.Down);

                    block.Model.AddInternalFaces(state, mesh);
                    //Coordinates coordsEast = coords + new Coordinates(1, 0, 0);
                    //if (model.Faces[Direction.East] != BlockFaceMask.Empty)
                    //{
                    //    Block? blockEast = neighborhood.At(coordsEast, out var offset)?.Blocks[offset];

                    //    if (blockEast != null && model.Faces[Direction.East] != blockEast.Model.Faces[Direction.West])
                    //    {
                    //        // BlockFaceMask exposedFace = model.Faces & ~blockEast.Model.Faces[Direction.West];

                    //        mesh.AppendBlockFace(neighborhood, coords, new Coordinates(1, 1, 1), new(0, 0, -1), new(0, -1, 0), model.TextureIDs[Direction.East], .6f);
                    //    }
                    //}

                    //Coordinates coordsSouth = coords + new Coordinates(0, 0, 1);
                    //if (model.Faces[Direction.South] != BlockFaceMask.Empty)
                    //{
                    //    Block? blockSouth = neighborhood.At(coordsSouth, out var offset)?.Blocks[offset];

                    //    if (blockSouth != null && model.Faces[Direction.South] != blockSouth.Model.Faces[Direction.North])
                    //    {
                    //        mesh.AppendBlockFace(neighborhood, coords, new Coordinates(0, 1, 1), new(1, 0, 0), new(0, -1, 0), model.TextureIDs[Direction.South], .7f);
                    //    }
                    //}

                    //Coordinates coordsWest = coords + new Coordinates(-1, 0, 0);
                    //if (model.Faces[Direction.West] != BlockFaceMask.Empty)
                    //{
                    //    Block? blockWest = neighborhood.At(coordsWest, out var offset)?.Blocks[offset];

                    //    if (blockWest != null && model.Faces[Direction.West] != blockWest.Model.Faces[Direction.East])
                    //    {
                    //        mesh.AppendBlockFace(neighborhood, coords, new Coordinates(0, 1, 0), new(0, 0, 1), new(0, -1, 0), model.TextureIDs[Direction.West], .8f);
                    //    }
                    //}

                    //Coordinates coordsNorth = coords + new Coordinates(0, 0, -1);
                    //if (model.Faces[Direction.North] != BlockFaceMask.Empty)
                    //{
                    //    Block? blockNorth = neighborhood.At(coordsNorth, out var offset)?.Blocks[offset];

                    //    if (blockNorth != null && model.Faces[Direction.North] != blockNorth.Model.Faces[Direction.South])
                    //    {
                    //        mesh.AppendBlockFace(neighborhood, coords, new Coordinates(1, 1, 0), new(-1, 0, 0), new(0, -1, 0), model.TextureIDs[Direction.North], .9f);
                    //    }
                    //}


                    //Coordinates coordsAbove = coords + new Coordinates(0, 1, 0);
                    //if (model.Faces[Direction.Up] != BlockFaceMask.Empty)
                    //{
                    //    Block? blockAbove = neighborhood.At(coordsAbove, out var offset)?.Blocks[offset];

                    //    if (blockAbove == null || model.Faces[Direction.Up] != blockAbove.Model.Faces[Direction.Down])
                    //    {
                    //        mesh.AppendBlockFace(neighborhood, coords, new Coordinates(0, 1, 0), new(1, 0, 0), new(0, 0, 1), model.TextureIDs[Direction.Up], 1f);
                    //    }
                    //}

                    //Coordinates coordsBelow = coords + new Coordinates(0, -1, 0);
                    //if (model.Faces[Direction.Down] != BlockFaceMask.Empty)
                    //{
                    //    Block? blockBelow = neighborhood.At(coordsBelow, out var offset)?.Blocks[offset];

                    //    if (blockBelow == null || model.Faces[Direction.Down] != blockBelow.Model.Faces[Direction.Up])
                    //    {
                    //        mesh.AppendBlockFace(neighborhood, coords, new Coordinates(0, 0, 1), new(1, 0, 0), new(0, 0, -1), model.TextureIDs[Direction.Down], .5f);
                    //    }
                    //}

                }
            }
        }

        UploadVertices(CollectionsMarshal.AsSpan(mesh.vertices));
        VertexCount = mesh.vertices.Count;
    }

    private int RoundToIncrement(int size)
    {
        return size + (BufferSizeIncrement - size % BufferSizeIncrement);
    }

    private void UploadVertices(Span<MinimizedChunkVertex> vertices)
    {
        int newVerticesSize = RoundToIncrement(vertices.Length * Unsafe.SizeOf<MinimizedChunkVertex>());

        if (VertexBuffer.Size < newVerticesSize)
        {
            VertexBuffer?.Dispose();
            VertexBuffer = graphics.device.CreateDataBuffer(DataBufferUsageFlags.Vertex, (uint)newVerticesSize);
        }

        graphics.transferBatcher.UploadToBuffer(vertices, new(this.VertexBuffer, 0, (uint)newVerticesSize), false);
    }

    public void Draw(CommandBuffer commandBuffer, RenderPass pass)
    {
        if (VertexCount != 0)
        {
            pass.BindVertexBuffers(0, [new(VertexBuffer)]);
            pass.DrawPrimitives((uint)VertexCount, 1, 0, 0);
        }
    }
}

ref struct BlockMeshBuilder
{
    public List<MinimizedChunkVertex> vertices;
    public ref readonly ChunkNeighborhood neighborhood;
    public Coordinates coordinates;

    public void AppendFace(Vector3 offset, Vector3 right, Vector3 up, Vector2 uvOffset, Vector2 uvRight, Vector2 uvUp, uint textureId)
    {
        MinimizedChunkVertex v0 = new(coordinates.ToVector() + offset,              uvOffset,                  textureId, Vector4.One, Vector3.Cross(right, up));
        MinimizedChunkVertex v1 = new(coordinates.ToVector() + offset + right,      uvOffset + uvRight,        textureId, Vector4.One, Vector3.Cross(right, up));
        MinimizedChunkVertex v2 = new(coordinates.ToVector() + offset + right + up, uvOffset + uvRight + uvUp, textureId, Vector4.One, Vector3.Cross(right, up));
        MinimizedChunkVertex v3 = new(coordinates.ToVector() + offset + up,         uvOffset + uvUp,           textureId, Vector4.One, Vector3.Cross(right, up));

        vertices.AddRange(v0, v2, v1, v0, v3, v2);
    }

    public BlockMeshBuilder()
    {
        vertices = new();
    }

    public void AppendPartialBlockFace(BlockFaceMask mask, Vector3 offset, Vector3 right, Vector3 up, Vector3 normal, uint blockTextureId, float ao)
    {
        BlockFaceMask remainingMask = mask;
        for (int index = 0; index < 16; index++) 
        { 
            BlockFaceMask currentBit = (BlockFaceMask)(1 << index);
            
            if ((remainingMask & currentBit) == 0)
            {
                continue;
            }

            remainingMask &= ~currentBit;

            int y = index >> 2;
            int x = index & 0b11;

            float ul = x * .25f;
            float vl = y * .25f;
            float uh = (x + 1) * .25f;
            float vh = (y + 1) * .25f;

            BlockFaceMask rowMask = currentBit;
            // try to expand quad to right
            for (int i = 1; i < 4 - x; i++)
            {
                BlockFaceMask m = (BlockFaceMask)(1 << index+i);
                if ((remainingMask & m) != 0)
                {
                    remainingMask &= ~m;
                    rowMask |= m;
                    uh += .25f;
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < 4 - y; i++)
            {
                BlockFaceMask nextRowMask = (BlockFaceMask)((int)rowMask << 4 * i);
                if ((remainingMask & nextRowMask) == nextRowMask)
                {
                    vh += .25f;
                    remainingMask &= ~nextRowMask;
                }
                else
                {
                    break;
                }
            }

            MinimizedChunkVertex v0 = new(coordinates.ToVector() + offset + ul * right + vl * up, new(ul, vl), blockTextureId, default, normal);
            MinimizedChunkVertex v1 = new(coordinates.ToVector() + offset + uh * right + vl * up, new(uh, vl), blockTextureId, default, normal);
            MinimizedChunkVertex v2 = new(coordinates.ToVector() + offset + uh * right + vh * up, new(uh, vh), blockTextureId, default, normal);
            MinimizedChunkVertex v3 = new(coordinates.ToVector() + offset + ul * right + vh * up, new(ul, vh), blockTextureId, default, normal);

            vertices.AddRange(v0, v2, v1, v0, v3, v2);
        }



        //for (int y = 0; y < 4; y++)
        //{
        //    for (int x = 0; x < 4; x++)
        //    {
        //        BlockFaceMask currentFace = (BlockFaceMask)(1 << (y * 4 + x));
        //        if ((remainingFace & currentFace) != 0)
        //        {
        //            float ul = x * .25f;
        //            float vl = y * .25f;
        //            float uh = (x + 1) * .25f;
        //            float vh = (y + 1) * .25f;

        //            for (int i = x + 1; i < 4; i++)
        //            {
        //                BlockFaceMask nextFace = (BlockFaceMask)(1 << (y * 4 + i));
        //                if ((remainingFace & nextFace) != 0)
        //                {
        //                    currentFace |= nextFace;
        //                    remainingFace &= ~nextFace;

        //                    uh += .25f;
        //                    x++;
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //            }

        //            for (int i = y + 1; i < 4; i++)
        //            {
        //                BlockFaceMask nextRow = (BlockFaceMask)(0xF << y * 4);
        //                if ((mask & currentFace) != 0)
        //                {
        //                    currentFace |= nextFace;
        //                    vh += .25f;
        //                    y++;
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //            }

        //            MinimizedChunkVertex v0 = new((coordinates + offset).ToVector() + ul * right + vl * up, new(ul, vl), blockTextureId, new Vector4(ao));
        //            MinimizedChunkVertex v1 = new((coordinates + offset).ToVector() + uh * right + vl * up, new(uh, vl), blockTextureId, new Vector4(ao));
        //            MinimizedChunkVertex v2 = new((coordinates + offset).ToVector() + uh * right + vh * up, new(uh, vh), blockTextureId, new Vector4(ao));
        //            MinimizedChunkVertex v3 = new((coordinates + offset).ToVector() + ul * right + vh * up, new(ul, vh), blockTextureId, new Vector4(ao));

        //            vertices.AddRange(v0, v2, v1, v0, v3, v2);
        //        }
        //    }
        //}
    }

    //public void AppendPartialBlockFace(Coordinates offset, Vector3 right, Vector3 up, Vector3 forward, uint blockTextureId, float ao)
    //{
    //    // float ao0 = CalculateAO(neighborhood, coordinates, right, up, normal);
    //    // float ao1 = CalculateAO(neighborhood, coordinates + right, right, up, normal);
    //    // float ao2 = CalculateAO(neighborhood, coordinates + right + up, right, up, normal);
    //    // float ao3 = CalculateAO(neighborhood, coordinates + up, right, up, normal);
    //    // Vector4 aov = new Vector4(ao0, ao1, ao3, ao2);

    //    for (int y = 0; y < 4; y++)
    //    {
    //        for (int x = 0; x < 4; x++)
    //        {
    //            BlockFaceMask m = (BlockFaceMask)(1 << (y * 4 + x));
    //            if ((faceMask & m) != 0)
    //            {
    //                float ul = x * .25f;
    //                float uh = (x + 1) * .25f;
    //                float vl = y * .25f;
    //                float vh = (y + 1) * .25f;

    //                MinimizedChunkVertex v0 = new((coordinates + faceOffset).ToVector() + ul * right.ToVector() + vl * up.ToVector(), new(ul, vl), blockTextureId, aov);
    //                MinimizedChunkVertex v1 = new((coordinates + faceOffset).ToVector() + uh * right.ToVector() + vl * up.ToVector(), new(uh, vl), blockTextureId, aov);
    //                MinimizedChunkVertex v2 = new((coordinates + faceOffset).ToVector() + uh * right.ToVector() + vh * up.ToVector(), new(uh, vh), blockTextureId, aov);
    //                MinimizedChunkVertex v3 = new((coordinates + faceOffset).ToVector() + ul * right.ToVector() + vh * up.ToVector(), new(ul, vh), blockTextureId, aov);

    //                vertices.AddRange(v0, v2, v1, v0, v3, v2);
    //            }
    //        }
    //    }

    //}

    public void AppendBlockFace(Coordinates offset, Coordinates right, Coordinates up, Coordinates normal, uint blockTextureId)
    {
        MinimizedChunkVertex v0 = new((coordinates + offset).ToVector(), new(0, 0), blockTextureId, default, normal.ToVector());
        MinimizedChunkVertex v1 = new((coordinates + offset + right).ToVector(), new(1, 0), blockTextureId, default, normal.ToVector());
        MinimizedChunkVertex v2 = new((coordinates + offset + right + up).ToVector(), new(1, 1), blockTextureId, default, normal.ToVector());
        MinimizedChunkVertex v3 = new((coordinates + offset + up).ToVector(), new(0, 1), blockTextureId, default, normal.ToVector());

        vertices.AddRange(v0, v2, v1, v0, v3, v2);
    }

    private float CalculateAO(ChunkNeighborhood neighborhood, Coordinates coordinates, Coordinates right, Coordinates up, Coordinates normal)
    {
        // TODO: better ao algo

        if (neighborhood.At(coordinates + normal - right, out var offset)?.GetModel(offset) is SolidBlockModel)
        {
            return 0;
        }
        if (neighborhood.At(coordinates + normal - up, out offset)?.GetModel(offset) is SolidBlockModel)
        {
            return 0;
        }
        if (neighborhood.At(coordinates + normal - right - up, out offset)?.GetModel(offset) is SolidBlockModel)
        {
            return 0;
        }
        if (neighborhood.At(coordinates + normal, out offset)?.GetModel(offset) is SolidBlockModel)
        {
            return 0;
        }

        return 1;
    }
}