using BlockGame42.Blocks;
using BlockGame42.Chunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlockGame42;
internal class WorldGenerator
{
    List<OpenSimplexNoise> octaves;

    public WorldGenerator()
    {
        octaves = [];
        for (int i = 0; i < 5; i++)
        {
            octaves.Add(new OpenSimplexNoise());
            Thread.Sleep(1); // hack so time-based seed is different
        }
    }

    public void GenerateChunk(World world, Coordinates chunkCoordinates, Chunk chunk)
    {
        Console.WriteLine("generating chunk " + chunkCoordinates);

        //BlockReference origin = world.GetBlockReference(chunkCoordinates * Chunk.Size);
        for (int z = 0; z < Chunk.Depth; z++)
        {
            for (int x = 0; x < Chunk.Width; x++)
            {
                double h = 10 + 5 * HeightSample(.01 * (chunkCoordinates.X * Chunk.Width + x), .01 * (chunkCoordinates.Z * Chunk.Depth + z));

                for (int y = 0; y < Chunk.Height; y++)
                {
                    BlockReference block = world.GetBlockReference(chunkCoordinates * Chunk.Size + new Coordinates(x, y, z));
                    //var block = origin.Offset(new(x, y, z));
                    if (y < h)
                    {
                        BlockState state = default;
                        state.DynamicBlock.Mask = 0xFF;
                        block.Set(BlockRegistry.Stone, state);
                    }
                    else
                    {
                        block.Set(BlockRegistry.Air);
                    }
                }
            }
        }

    }

    private double HeightSample(double x, double y)
    {
        double total = 1;
        double scale = 1;
        foreach (var o in octaves)
        {
            total += (1.0 / scale) * o.Evaluate(scale * x, scale * y);
            scale *= 2;
        }
        return total;
    }
}
