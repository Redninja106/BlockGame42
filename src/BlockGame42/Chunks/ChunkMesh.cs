using BlockGame42.Blocks;
using BlockGame42.Blocks.Models;
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

    private readonly GraphicsContext graphics;
    public DataBuffer VertexBuffer;
    public int VertexCount;

    public ChunkMesh(GraphicsContext graphics)
    {
        this.graphics = graphics;

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

        if (neighborBlock.IsNull)
        {
            return;
        }

        BlockFaceMask visibleFaces = BlockFaceMask.Full;
        if (block.Prototype.Transparent != neighborBlock.Prototype.Transparent)
        {
            BlockFaceMask faceMask = block.Prototype.Model.GetFaceMask(block.State, direction);
            BlockFaceMask neighborMask = neighborBlock.Prototype.Model.GetFaceMask(neighborBlock.State, inverseDirection);
            if ((int)direction < 4)
            {
                neighborMask = FlipMaskHorizontal(neighborMask);
            }

            visibleFaces = faceMask & ~neighborMask;

            if (visibleFaces == 0)
            {
                return;
            }
        }

        Coordinates offset = faceOffsets[(int)direction];
        Coordinates right = rightDirs[(int)direction];
        Coordinates up = upDirs[(int)direction];
        Coordinates normal = forwardDirs[(int)direction];
        float ao = aos[(int)direction];
        if (visibleFaces == BlockFaceMask.Full)
        {
            mesh.AppendBlockFace(offset, right, up, normal, block.Prototype.Model.Material.TexID);
        }
        else
        {
            mesh.AppendPartialBlockFace(visibleFaces, offset.ToVector(), right.ToVector(), up.ToVector(), normal.ToVector(), block.Prototype.Model.Material.TexID);
            //mesh.AppendPartialBlockFace(visibleFaces, neighborhood, coords, offset, right, up, model.TextureIDs[direction], ao);
        }
    }

    public void Build(World world, Chunk chunk, Coordinates chunkCoordinates)
    {
        Timer start = Timer.Start();
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

        Console.WriteLine($"meshing chunk {chunkCoordinates} took {start.ElapsedMilliseconds()}ms");
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

    public void AppendPartialBlockFace(BlockFaceMask mask, Vector3 offset, Vector3 right, Vector3 up, Vector3 normal, uint blockTextureId)
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
    }

    public void AppendBlockFace(Coordinates offset, Coordinates right, Coordinates up, Coordinates normal, uint blockTextureId)
    {
        MinimizedChunkVertex v0 = new((coordinates + offset).ToVector(), new(0, 0), blockTextureId, default, normal.ToVector());
        MinimizedChunkVertex v1 = new((coordinates + offset + right).ToVector(), new(1, 0), blockTextureId, default, normal.ToVector());
        MinimizedChunkVertex v2 = new((coordinates + offset + right + up).ToVector(), new(1, 1), blockTextureId, default, normal.ToVector());
        MinimizedChunkVertex v3 = new((coordinates + offset + up).ToVector(), new(0, 1), blockTextureId, default, normal.ToVector());

        vertices.AddRange(v0, v2, v1, v0, v3, v2);
    }
}
