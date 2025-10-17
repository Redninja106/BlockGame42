using BlockGame42.Blocks;
using BlockGame42.Rendering;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Chunks;
internal class ChunkManager
{
    private GraphicsManager graphics;
    public Dictionary<Coordinates, (Chunk, ChunkMesh)> chunkMap = [];


    public ChunkManager(GraphicsManager graphics)
    {
        this.graphics = graphics;

        int chunks = 2;

        for (int x = -chunks; x < chunks; x++)
        {
            for (int z = -chunks; z < chunks; z++)
            {
                for (int y = 0; y < chunks; y++)
                {
                    LoadOrCreateChunk(new(x, y, z));
                }
            }
        }
    }

    public void Update(Vector3 playerPosition)
    {
        foreach (var (pos, (chunk, mesh)) in chunkMap)
        {
            chunk.Update();
        }
    }

    public void BuildStaleChunks()
    {
        foreach (var (coords, (chunk, mesh)) in chunkMap)
        {
            if (chunk.Blocks.Stale)
            {
                mesh.Build(GetNeighborhood(coords));
                chunk.Blocks.Stale = false;
            }
        }
    }

    public ChunkNeighborhood GetNeighborhood(Coordinates chunkCoordinates)
    {
        ChunkNeighborhood.Storage storage = default;
        for (int y = 0; y < ChunkNeighborhood.Size; y++)
        {
            for (int z = 0; z < ChunkNeighborhood.Size; z++)
            {
                for (int x = 0; x < ChunkNeighborhood.Size; x++)
                {
                    storage[y * ChunkNeighborhood.Size * ChunkNeighborhood.Size + z * ChunkNeighborhood.Size + x] = chunkMap.GetValueOrDefault(chunkCoordinates + new Coordinates(x - 1, y - 1, z - 1)).Item1;
                }
            }
        }
        return new ChunkNeighborhood(storage, chunkCoordinates);
    }

    public void LoadOrCreateChunk(Coordinates coordinates)
    {
        Chunk chunk = new Chunk();
        
        for (int z = 0; z < Chunk.Depth; z++)
        {
            int gz = coordinates.Z * Chunk.Depth + z;
            for (int x = 0; x < Chunk.Width; x++)
            {
                int gx = coordinates.X * Chunk.Width + x;

                for (int y = 0; y < Chunk.Height; y++)
                {
                    int gy = coordinates.Y * Chunk.Height + y;
                    chunk.Blocks[x, y, z] = Game.Blocks.Air;
                    //if (Occupied(gx, gy, gz))
                    //{
                    //    chunk.Blocks[x, y, z] = Game.Blocks.Stone;
                    //    chunk.BlockStates[x, y, z].Raw = 0x00000000FFFFFFFF;
                    //}

                    for (int by = 0; by < 2; by++)
                    {
                        for (int bz = 0; bz < 2; bz++)
                        {
                            for (int bx = 0; bx < 2; bx++)
                            {
                                float bitx = gx + bx * .5f + .25f;
                                float bity = gy + by * .5f + .25f;
                                float bitz = gz + bz * .5f + .25f;
                                float h = (15 + 4 * float.Cos(bitx * 1/16f) + 4 * float.Cos(bitz * 1/16f));
                                if (bity < h)
                                {
                                    chunk.Blocks[x, y, z] = Game.Blocks.Stone;
                                    chunk.BlockStates[x, y, z].DynamicBlock[(by << 2) + (bz << 1) + bx] = true;
                                }
                            }
                        }
                    }

                    if (chunk.Blocks[x, y, z] == Game.Blocks.Stone)
                    {
                        chunk.BlockMasks[x, y, z] = chunk.BlockStates[x, y, z].DynamicBlock.Mask;
                    }
                }
            }
        }


        ChunkMesh mesh = new(graphics);
        chunkMap[coordinates] = (chunk, mesh);
    }

    public void Tick()
    {
        foreach (var (pos, (chunk, mesh)) in chunkMap)
        {
            chunk.Tick();
        }
    }
}
