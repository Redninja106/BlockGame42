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
internal class ClientChunkManager
{
    private GameClient client;
    private WorldGenerator generator;

    public ClientChunkManager(GameClient client)
    {
        this.client = client;
        generator = new();
    }

    public void Initialize()
    {
    //    int chunks = 2;

    //    for (int x = -chunks; x < chunks; x++)
    //    {
    //        for (int z = -chunks; z < chunks; z++)
    //        {
    //            for (int y = 0; y < 1; y++)
    //            {
    //                LoadOrCreateChunk(new(x, y, z));
    //            }
    //        }
    //    }
    }

    public void LoadOrCreateChunk(Coordinates coordinates)
    {
        if (client.World.Chunks.At(coordinates) != null)
        {
            return;
        }

        Chunk chunk = new Chunk();
        client.World.Chunks.Insert(coordinates, chunk);
        generator.GenerateChunk(client.World, coordinates, chunk);

        //for (int z = 0; z < Chunk.Depth; z++)
        //{
        //    int gz = coordinates.Z * Chunk.Depth + z;
        //    for (int x = 0; x < Chunk.Width; x++)
        //    {
        //        int gx = coordinates.X * Chunk.Width + x;
        //        float worldHeight = (15 + 4 * float.Cos(gx * 1 / 16f) + 4 * float.Cos(gz * 1 / 16f));

        //        for (int y = 0; y < Chunk.Height; y++)
        //        {
        //            int gy = coordinates.Y * Chunk.Height + y;
        //            //if (Occupied(gx, gy, gz))
        //            //{
        //            //    chunk.Blocks[x, y, z] = BlockRegistry.Stone;
        //            //    chunk.BlockStates[x, y, z].Raw = 0x00000000FFFFFFFF;
        //            //}

        //            // for (int by = 0; by < 2; by++)
        //            // {
        //            //     for (int bz = 0; bz < 2; bz++)
        //            //     {
        //            //         for (int bx = 0; bx < 2; bx++)
        //            //         {
        //            //             float bitx = gx + bx * .5f + .25f;
        //            //             float bity = gy + by * .5f + .25f;
        //            //             float bitz = gz + bz * .5f + .25f;
        //            //             float h = (15 + 4 * float.Cos(bitx * 1/16f) + 4 * float.Cos(bitz * 1/16f));
        //            //             if (bity < h)
        //            //             {
        //            //                 chunk.Blocks[x, y, z] = BlockRegistry.Stone;
        //            //                 chunk.BlockStates[x, y, z].DynamicBlock[(by << 2) + (bz << 1) + bx] = true;
        //            //             }
        //            //         }
        //            //     }
        //            // }

        //            if (gy < worldHeight)
        //            {
        //                chunk.Blocks[x, y, z] = BlockRegistry.Stone;
        //                chunk.BlockMasks[x, y, z] = 0xFFFFFFFFFFFFFFFF;
        //            }
        //            else
        //            {
        //                chunk.Blocks[x, y, z] = BlockRegistry.Air;
        //            }
        //        }
        //    }
        //}

    }

    public void Update(PlayerEntity player)
    {
        int chunks = 3;
        Coordinates centerChunk = player.GetChunkCoordinates();

        for (int x = -chunks; x < chunks; x++)
        {
            for (int z = -chunks; z < chunks; z++)
            {
                for (int y = 0; y < 1; y++)
                {
                    LoadOrCreateChunk(new(centerChunk.X + x, y, centerChunk.Z + z));
                }
            }
        }

    }

    //public void Tick()
    //{
    //    int renderDistance = 3;

    //    foreach (var (pos, (chunk, mesh)) in chunkMap)
    //    {
    //        if (Vector3.Distance(pos.ToVector(), client.Interaction.Player.Transform.Position / Chunk.SizeVector) > renderDistance)
    //        {
    //            UnloadChunk(pos);
    //            continue;
    //        }

    //        chunk.Tick();
    //    }

    //    for (int x = -renderDistance; x < renderDistance; x++)
    //    {
    //        for (int y = -renderDistance; y < renderDistance; y++)
    //        {

    //        }
    //    }
    //}

    private void UnloadChunk(Coordinates coordinates)
    {

    }
}
