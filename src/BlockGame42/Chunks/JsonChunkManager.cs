using BlockGame42.Blocks;
using Newtonsoft.Json;
using Protor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlockGame42.Chunks;

internal class JsonChunkManager : ChunkManager
{
    private GameClient client;

    public JsonChunkManager(GameClient client) : base(client)
    {
        this.client = client;
    }

    public override void Initialize()
    {
        const string worldPath = "../../../../../worldparser/out";

        HashSet<string> unknownBlocks = [];
        foreach (var filePath in Directory.GetFiles(worldPath))
        {
            JsonChunk json = JsonConvert.DeserializeObject<JsonChunk>(File.ReadAllText(filePath))!;
            Chunk chunk = new();
            chunk.Blocks.Stale = true;

            Block air = Registry.Get<Block>("air");
            Dictionary<int, Block> blockLookup = [];
            foreach (var (blockName, id) in json.Palette)
            {
                if (!Registry.TryGet(blockName, out Block? block))
                {
                    unknownBlocks.Add(blockName);
                    block = Registry.Get<Block>("stone");
                }
                blockLookup.Add(id, block);
            }

            for (int y = 0; y < 32; y++)
            {
                for (int z = 0; z < 32; z++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        int blockIdx = json.Blocks[y * 32 * 32 + z * 32 + x];
                        Block block = blockLookup[blockIdx];
                        chunk.Blocks[x, y, z] = block;
                        chunk.BlockStates[x, y, z] = block.DefaultState;
                        chunk.BlockMasks[x, y, z] = block.Model.GetVolumeMask(block.DefaultState);
                    }
                }
            }

            client.World.Chunks.Insert(new(json.X, json.Y, json.Z), chunk);
        }

        foreach (var b in unknownBlocks)
        {
            Console.WriteLine("unknown block: " + b);
        }
    }

    public override void Update()
    {
    }

    class JsonChunk
    {
        [JsonProperty(PropertyName = "x")]
        public int X { get; init; }

        [JsonProperty(PropertyName = "y")]
        public int Y { get; init; }

        [JsonProperty(PropertyName = "z")]
        public int Z { get; init; }

        [JsonProperty(PropertyName = "block_palette")]
        public Dictionary<string, int> Palette { get; init; }

        [JsonProperty(PropertyName = "blocks")]
        public int[] Blocks { get; init; }
    }
}
