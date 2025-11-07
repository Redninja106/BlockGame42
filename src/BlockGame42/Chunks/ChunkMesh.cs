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
    private const int BufferSizeIncrement = 2 * 1024;

    private readonly GraphicsManager graphics;
    public DataBuffer VertexBuffer;
    public int VertexCount;
    private World world;

    public ChunkMesh(GraphicsManager graphics, World world)
    {
        this.graphics = graphics;
        this.world = world;

        VertexBuffer = this.graphics.device.CreateDataBuffer(DataBufferUsageFlags.Vertex, BufferSizeIncrement);
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

    static void EmitBlockFace(BlockMeshBuilder mesh, BlockReference block, Direction direction)
    {
        // Block block = neighborhood.Center.Blocks[coords];
        // BlockModel model = block.Model;

        Direction inverseDirection = directionInverses[(int)direction];

        if (block.Prototype.Model.GetFaceMask(block.State, direction) == BlockFaceMask.Empty)
        {
            return; 
        }

        // Coordinates coordsOffset = block. + forwardDirs[(int)direction];
        // Chunk? chunk = neighborhood.At(coordsOffset, out var blockOffset);

        // if (chunk == null)
        // {
        //     return;
        // }

        BlockReference neighborBlock = block.Offset(forwardDirs[(int)direction]);// chunk.Blocks[blockOffset];

        BlockFaceMask faceMask = block.Prototype.Model.GetFaceMask(block.State, direction);
        BlockFaceMask neighborMask = neighborBlock.Prototype.Model.GetFaceMask(neighborBlock.State, inverseDirection);
        if ((int)direction < 4)
        {
            neighborMask = FlipMaskHorizontal(neighborMask);
        }

        BlockFaceMask visibleFaces = faceMask & ~neighborMask;

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
            mesh.AppendBlockFace(offset, right, up, normal, block.Prototype.Model.GetMaterial(block.State).Data.Transmission[direction], block.Prototype.Model.GetMaterial(block.State).Data.Emission[direction]);
        }
        else
        {
            mesh.AppendPartialBlockFace(visibleFaces, offset.ToVector(), right.ToVector(), up.ToVector(), normal.ToVector(), block.Prototype.Model.GetMaterial(block.State).Data.Transmission[direction], block.Prototype.Model.GetMaterial(block.State).Data.Emission[direction]);
            //mesh.AppendPartialBlockFace(visibleFaces, neighborhood, coords, offset, right, up, model.TextureIDs[direction], ao);
        }
    }

    public void Build(Chunk chunk, Coordinates chunkCoordinates)
    {
        // Timer start = Timer.Start();
        scoped BlockMeshBuilder mesh = new();

        for (int y = 0; y < Chunk.Height; y++)
        {
            for (int z = 0; z < Chunk.Depth; z++)
            {
                for (int x = 0; x < Chunk.Width; x++)
                {
                    Coordinates localCoordinates = new(x, y, z);
                    mesh.coordinates = localCoordinates;

                    BlockReference block = world.GetBlockReference(chunkCoordinates * Chunk.Size + localCoordinates);

                    EmitBlockFace(mesh, block, Direction.East);
                    EmitBlockFace(mesh, block, Direction.South);
                    EmitBlockFace(mesh, block, Direction.West);
                    EmitBlockFace(mesh, block, Direction.North);
                    EmitBlockFace(mesh, block, Direction.Up);
                    EmitBlockFace(mesh, block, Direction.Down);

                    block.Prototype.Model.AddInternalFaces(block.State, mesh);
                }
            }
        }

        UploadVertices(CollectionsMarshal.AsSpan(mesh.vertices));
        VertexCount = mesh.vertices.Count;

        //Console.WriteLine($"meshing chunk {chunkCoordinates} took {start.ElapsedMilliseconds()}ms");
        // return start.ElapsedMilliseconds();
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

        using TransferBuffer transferBuffer = graphics.device.CreateTransferBuffer(TransferBufferUsage.Upload, VertexBuffer.Size);
        
        Span<MinimizedChunkVertex> mappedBuffer = transferBuffer.Map<MinimizedChunkVertex>(false);
        vertices.CopyTo(mappedBuffer);
        transferBuffer.Unmap();

        CopyPass pass = graphics.CommandBuffer.BeginCopyPass();
        pass.UploadToDataBuffer(transferBuffer, 0, VertexBuffer, 0, (uint)newVerticesSize, false);
        pass.End();
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
    // public ref readonly ChunkNeighborhood neighborhood;
    public Coordinates coordinates;

    public void AppendFace(Vector3 offset, Vector3 right, Vector3 up, Vector2 uvOffset, Vector2 uvRight, Vector2 uvUp, uint textureId, uint emissionId)
    {
        MinimizedChunkVertex v0 = new(coordinates.ToVector() + offset,              uvOffset,                  textureId, Vector4.One, Vector3.Cross(right, up), emissionId);
        MinimizedChunkVertex v1 = new(coordinates.ToVector() + offset + right,      uvOffset + uvRight,        textureId, Vector4.One, Vector3.Cross(right, up), emissionId);
        MinimizedChunkVertex v2 = new(coordinates.ToVector() + offset + right + up, uvOffset + uvRight + uvUp, textureId, Vector4.One, Vector3.Cross(right, up), emissionId);
        MinimizedChunkVertex v3 = new(coordinates.ToVector() + offset + up,         uvOffset + uvUp,           textureId, Vector4.One, Vector3.Cross(right, up), emissionId);

        vertices.AddRange(v0, v2, v1, v0, v3, v2);
    }

    public BlockMeshBuilder()
    {
        vertices = new();
    }

    public void AppendPartialBlockFace(BlockFaceMask mask, Vector3 offset, Vector3 right, Vector3 up, Vector3 normal, uint blockTextureId, uint emissionId)
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

            MinimizedChunkVertex v0 = new(coordinates.ToVector() + offset + ul * right + vl * up, new(ul, vl), blockTextureId, default, normal, emissionId);
            MinimizedChunkVertex v1 = new(coordinates.ToVector() + offset + uh * right + vl * up, new(uh, vl), blockTextureId, default, normal, emissionId);
            MinimizedChunkVertex v2 = new(coordinates.ToVector() + offset + uh * right + vh * up, new(uh, vh), blockTextureId, default, normal, emissionId);
            MinimizedChunkVertex v3 = new(coordinates.ToVector() + offset + ul * right + vh * up, new(ul, vh), blockTextureId, default, normal, emissionId);

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

    public void AppendBlockFace(Coordinates offset, Coordinates right, Coordinates up, Coordinates normal, uint blockTextureId, uint emissionId)
    {
        MinimizedChunkVertex v0 = new((coordinates + offset).ToVector(), new(0, 0), blockTextureId, default, normal.ToVector(), emissionId);
        MinimizedChunkVertex v1 = new((coordinates + offset + right).ToVector(), new(1, 0), blockTextureId, default, normal.ToVector(), emissionId);
        MinimizedChunkVertex v2 = new((coordinates + offset + right + up).ToVector(), new(1, 1), blockTextureId, default, normal.ToVector(), emissionId);
        MinimizedChunkVertex v3 = new((coordinates + offset + up).ToVector(), new(0, 1), blockTextureId, default, normal.ToVector(), emissionId);

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

static class DirectionExtensions
{
    public static Direction Inverse(this Direction direction)
    {
        return direction switch
        {
            Direction.East => Direction.West,
            Direction.South => Direction.North,
            Direction.West => Direction.East,
            Direction.North => Direction.South,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            _ => default
        };
    }
}