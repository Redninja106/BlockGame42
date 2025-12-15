using BlockGame42.Blocks;
using BlockGame42.Chunks;
using BlockGame42.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal class World
{
    public ChunkMap Chunks { get; }

    private List<Entity> deletedEntities = [];

    public World()
    {
        Chunks = new();

        // Chunks.Initialize();
        // chunkManager = new(null);

        //foreach (var (chunkCoords, (chunk, mesh)) in chunks.chunkMap)
        //{
        //    Timer timer = Timer.Start();

        //    for (int y = 0; y < Chunk.Height; y++)
        //    {
        //        for (int z = 0; z < Chunk.Depth; z++)
        //        {
        //            for (int x = 0; x < Chunk.Width; x++)
        //            {
        //                UpdateSupport(GetBlockReference(chunkCoords * Chunk.Size + new Coordinates(x, y, z)));
        //            }
        //        }
        //    }

        //    Console.WriteLine($"support update of chunk {chunkCoords} took {timer.ElapsedMilliseconds()}ms");

        //}
    }

    public BlockReference GetBlockReference(Coordinates worldCoordinates)
    {
        Chunk.DecomposeCoordinates(worldCoordinates, out Coordinates chunkCoordinates, out Coordinates localCoordinates);
        Chunk? chunk = Chunks.At(chunkCoordinates);

        if (chunk == null)
        {
            return default;
        }

        return new BlockReference(
            this,
            chunk,
            chunkCoordinates,
            worldCoordinates,
            ref chunk.Blocks[localCoordinates],
            ref chunk.BlockStates[localCoordinates],
            ref chunk.Support[localCoordinates],
            ref chunk.BlockMasks[localCoordinates]
            );
    }

    public void Update()
    {
        Chunks.Update();
    }

    public bool Raycast(Ray ray, out float outT, out Coordinates outHitCoords, out Coordinates outNormal)
    {
        outNormal = default;
        ray.Direction = Vector3.Normalize(ray.Direction);

        Coordinates voxel = new((int)float.Floor(ray.Origin.X), (int)float.Floor(ray.Origin.Y), (int)float.Floor(ray.Origin.Z));
        Coordinates step = new(float.Sign(ray.Direction.X), float.Sign(ray.Direction.Y), float.Sign(ray.Direction.Z));

        if (step.X == 0 && step.Y == 0 && step.Z == 0)
        {
            outT = float.PositiveInfinity;
            outHitCoords = outNormal = default;
            return false;
        }

        //Box box = new();
        //if (!BoxRaycast(ray, Vector3.Zero, Chunk.SizeVector, out float tNear, out float tFar))
        //{
        //    outT = float.PositiveInfinity;
        //    outHitCoords = outNormal = default;
        //    return false;
        //}

        Vector3 start = ray.At(0);
        Vector3 end = ray.At(ray.Length);

        Vector3 d = end - start;
        Vector3 tDelta = step.ToVector() / d;
        Vector3 tMax = tDelta * new Vector3(Frac(start.X, step.X), Frac(start.Y, step.Y), Frac(start.Z, step.Z));

        int stepLimit = 1000;

        float t = 0;
        while (--stepLimit > 0)
        {
            //if (voxel.X < 0 || voxel.X >= Width || voxel.Y < 0 || voxel.Y >= Height || voxel.Z < 0 || voxel.Z >= Depth)
            //{
            //    outT = float.PositiveInfinity;
            //    outBlockHit = outNormal = default;
            //    return false;
            //}

            Coordinates chunkCoords = Chunk.ChunkCoordinatesFromWorldPosition(voxel.ToVector());
            if (Chunks.At(chunkCoords) is Chunk chunk)
            {
                Block block = chunk.Blocks[voxel - chunkCoords * Chunk.Size];
                BlockState blockState = chunk.BlockStates[voxel - chunkCoords * Chunk.Size];
                Ray localRay = ray;
                localRay.Origin -= voxel.ToVector();
                if (block.Raycast(blockState, localRay, ref t, ref outNormal))
                {
                    outT = t;
                    outHitCoords = voxel;
                    return true;
                }
            }

            if (step.X != 0 && tMax.X < tMax.Y)
            {
                if (step.X != 0 && tMax.X < tMax.Z)
                {
                    t = tMax.X;
                    voxel.X += step.X;
                    tMax.X += tDelta.X;
                    outNormal = new(-step.X, 0, 0);
                }
                else
                {
                    t = tMax.Z;
                    voxel.Z += step.Z;
                    tMax.Z += tDelta.Z;
                    outNormal = new(0, 0, -step.Z);
                }
            }
            else
            {
                if (step.Y != 0 && tMax.Y < tMax.Z)
                {
                    t = tMax.Y;
                    voxel.Y += step.Y;
                    tMax.Y += tDelta.Y;
                    outNormal = new(0, -step.Y, 0);
                }
                else
                {
                    t = tMax.Z;
                    voxel.Z += step.Z;
                    tMax.Z += tDelta.Z;
                    outNormal = new(0, 0, -step.Z);
                }
            }
        }

        outT = float.PositiveInfinity;
        outHitCoords = outNormal = default;
        return false;
    }

    static float Frac(float f, float s)
    {
        if (s > 0)
            return 1 - f + float.Floor(f);
        else
            return f - float.Floor(f);
    }

    public Block? GetBlock(Coordinates coordinates, out BlockState state)
    {
        Chunk.DecomposeCoordinates(coordinates, out Coordinates chunkCoords, out Coordinates localCoords);
        if (Chunks.At(chunkCoords) is Chunk chunk)
        {
            state = chunk.BlockStates[localCoords];
            return chunk.Blocks[localCoords];
        }
        else
        {
            state = default;
            return null;
        }
    }

    public bool SetBlock(Coordinates coordinates, Block block, BlockState state = default)
    {
        BlockReference blockRef = GetBlockReference(coordinates);
        
        blockRef.Prototype = block;
        blockRef.State = state;
        blockRef.Mask = block.Model.GetVolumeMask(state);

        if (blockRef.Chunk != null) 
        { 
            blockRef.Chunk.Blocks.Stale = true;
            blockRef.Chunk.BlockMasks.Stale = true;
        }

        // UpdateSupport(blockRef);
        UpdateBlock(blockRef);

        for (int i = 0; i < 6; i++)
        {
            var neighbor = blockRef.Offset(ChunkMesh.forwardDirs[i]);
            UpdateBlock(neighbor);
            if (neighbor.Chunk != null)
            {
                neighbor.Chunk.Blocks.Stale = true;
            }
        }
        return true;
    }

    public void AddEntity(Entity entity)
    {
        Coordinates chunkCoordinates = entity.GetChunkCoordinates();
        this.Chunks.At(chunkCoordinates)?.Entities.Add(entity);
    }

    public bool Intersect(Box box, Vector3 offset = default)
    {
        int minX = (int)float.Floor(box.Min.X + offset.X); 
        int minY = (int)float.Floor(box.Min.Y + offset.Y); 
        int minZ = (int)float.Floor(box.Min.Z + offset.Z);
        int maxX = (int)float.Floor(box.Max.X + offset.X);
        int maxY = (int)float.Floor(box.Max.Y + offset.Y);
        int maxZ = (int)float.Floor(box.Max.Z + offset.Z);

        for (int y = minY; y <= maxY; y++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Block? block = GetBlock(new(x, y, z), out BlockState state);
                    Box localBox = box;
                    localBox.Min = localBox.Min + offset - new Vector3(x, y, z);
                    localBox.Max = localBox.Max + offset - new Vector3(x, y, z);
                    if (block != null && block.Intersect(state, localBox))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void Tick()
    {
        Chunks.Tick();

        foreach (var entity in deletedEntities)
        {
            Chunk chunk = Chunks.At(entity.GetChunkCoordinates());
            if (!chunk.Entities.Remove(entity)) 
            {
                Debug.Assert(false);
            }
        }
        deletedEntities.Clear();
    }

    public void RemoveEntity(Entity entity)
    {
        deletedEntities.Add(entity);
    }

    public void UpdateBlock(Coordinates worldCoordinates)
    {
        UpdateBlock(GetBlockReference(worldCoordinates));
    }

    public void UpdateBlock(in BlockReference block)
    {
        if (!block.IsNull)
        {
            block.Prototype.OnUpdate(this, in block);
        }
        // UpdateSupport(block);
    }

    public void UpdateSupport(in BlockReference block, bool weak = false)
    {
        if (block.Prototype == BlockRegistry.Air)
        {
            return;
        }

        if (block.WorldCoordinates.Y == 0)
        {
            block.Support = 255;
            return;
        }

        byte neighborSupport = 0;
        int weakSideCount = 0;

        for (int i = 0; i < 6; i++)
        {
            BlockReference neighbor = block.Offset(ChunkMesh.forwardDirs[i]);
            
            byte blockStrength = i switch
            {
                4 => block.Prototype.Strength.Tension,
                5 => block.Prototype.Strength.Compression,
                _ => block.Prototype.Strength.Lateral,
            };

            byte neighborStrength = i switch
            {
                4 => neighbor.Prototype.Strength.Tension,
                5 => neighbor.Prototype.Strength.Compression,
                _ => neighbor.Prototype.Strength.Lateral,
            };

            byte transferStrength = byte.Min(blockStrength, neighbor.Support);
            neighborSupport = byte.Max(neighborSupport, transferStrength);

            if (neighborStrength < blockStrength)
            {
                weakSideCount++;
            }
        }

        int support = neighborSupport - weakSideCount;

        if (support < 0)
        {
            support = 0;
        }

        int oldSupport = block.Support;
        block.Support = (byte)int.Clamp(support, 0, 255);

        if (!weak)
        {
            for (int i = 0; i < 6; i++)
            {
                BlockReference neighbor = block.Offset(ChunkMesh.forwardDirs[i]);
                if (neighbor.Support < oldSupport)
                {
                    // UpdateSupport(in neighbor, true);
                }
            }
        }
    }

    public byte GetSupport(Coordinates blockCoordinates)
    {
        Chunk.DecomposeCoordinates(blockCoordinates, out var chunkCoords, out var localCoords);
        return Chunks.At(chunkCoords)!.Support[localCoords];
    }
}

class ChunkMap : IEnumerable<KeyValuePair<Coordinates, Chunk>>
{
    private readonly Dictionary<Coordinates, Chunk> map = [];

    public Chunk? At(Coordinates chunkCoordinates)
    {
        return map.GetValueOrDefault(chunkCoordinates);
    }

    public void Insert(Coordinates coordinates, Chunk chunk)
    {
        map.Add(coordinates, chunk);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<KeyValuePair<Coordinates, Chunk>> GetEnumerator()
    {
        return map.GetEnumerator();
    }

    public void Tick()
    {
        foreach (var (_, chunk) in map)
        {
            chunk.Tick();
        }
    }

    internal void Update()
    {
        foreach (var (_, chunk) in map)
        {
            chunk.Update();
        }
    }

}

readonly struct ChunkNeighborhood
{
    public const int Size = 3;

    public Chunk Center => storage.Get(1, 1, 1)!;

    public readonly Coordinates CenterChunkCoordinates;
    private readonly Storage storage;

    [InlineArray(Size * Size * Size)]
    public struct Storage
    {
        private Chunk? first;

        public Chunk? Get(int x, int y, int z)
        {
            return this[y * Size * Size + z * Size + x];
        }
    }

    public ChunkNeighborhood(Storage storage, Coordinates centerChunkCoordinates)
    {
        this.storage = storage;
        this.CenterChunkCoordinates = centerChunkCoordinates;
    }

    public Chunk? At(Coordinates localCoordinates, out Coordinates localOffset)
    {
        Chunk.DecomposeCoordinates(localCoordinates, out Coordinates chunkCoordinates, out localOffset);
        Coordinates localChunkCoordinates = chunkCoordinates + new Coordinates(1, 1, 1);
        return storage.Get(localChunkCoordinates.X, localChunkCoordinates.Y, localChunkCoordinates.Z);
    }

    public Coordinates WorldToLocal(Coordinates worldCoordinates)
    {
        return worldCoordinates - CenterChunkCoordinates * Chunk.Size;
    }

    public Coordinates LocalToWorld(Coordinates localCoordinates)
    {
        return Chunk.Size * CenterChunkCoordinates + localCoordinates;
    }
}