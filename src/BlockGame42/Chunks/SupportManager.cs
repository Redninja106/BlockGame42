using BlockGame42.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Chunks;

internal class SupportManager
{
    ClientChunkManager chunks;
    List<Coordinates> chunkLocations;
    int nextUpdate;

    public SupportManager(ClientChunkManager chunks)
    {
        this.chunks = chunks;
        // chunkLocations = chunks.chunkMap.Keys.ToList();
    }


    //public void UpdateChunkSupport(ChunkNeighborhood neighborhood)
    //{
    //    for (int y = 0; y < Chunk.Height; y++)
    //    {
    //        for (int z = 0; z < Chunk.Depth; z++)
    //        {
    //            for (int x = 0; x < Chunk.Width; x++)
    //            {
    //                Coordinates localCoordinates = new(x, y, z);

    //                Chunk? chunk = neighborhood.At(localCoordinates, out Coordinates offset);
    //                if (chunk == null)
    //                {
    //                    continue;
    //                }

    //                if (chunk.Blocks[offset] is AirBlock)
    //                {
    //                    continue;
    //                }

    //                byte maxSupport = chunk.Blocks[offset].Strength;

    //                byte support = chunk.Support[offset];
    //                if (y == 0 && neighborhood.CenterChunkCoordinates.Y == 0)
    //                {
    //                    support = 255;
    //                }
    //                else
    //                {
    //                    byte maxNeighborSupport = 0;
    //                    for (int i = 0; i < 6; i++)
    //                    {
    //                        Coordinates neighborCoords = localCoordinates + ChunkMesh.forwardDirs[i];
    //                        if (neighborhood.At(neighborCoords, out var nOff) is Chunk c)
    //                        {
    //                            maxNeighborSupport = byte.Max(maxNeighborSupport, c.Support[nOff]);
    //                        }
    //                    }
    //                    support = (byte)int.Clamp(maxNeighborSupport - 1, 0, 255);
    //                }
    //                support = byte.Min(support, maxSupport);
    //                chunk.Support[offset] = support;
    //            }
    //        }
    //    }
    //}

    internal void IncrementalUpdate()
    {
        // UpdateChunkSupport(chunks.GetNeighborhood(this.chunkLocations[nextUpdate]));
        // nextUpdate = (nextUpdate + 1) % chunkLocations.Count;
    }
}
