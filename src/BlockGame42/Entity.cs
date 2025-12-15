using BlockGame42.Chunks;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal abstract class Entity
{
    public ref Transform Transform => ref transform;
    public ref Transform InterpolatedTransform => ref interpolatedTransform;
    public ref Transform PreviousTransform => ref previousTransform;
    
    private Transform previousTransform;
    private Transform interpolatedTransform;
    private Transform transform;

    private Coordinates chunkCoordinates;

    protected World World { get; }

    public Entity(World world)
    {
        this.World = world;
        this.transform = new();
    }

    public Coordinates GetChunkCoordinates()
    {
        return Coordinates.Floor(transform.Position / Chunk.SizeVector);
    }

    public void Teleport(Transform transform)
    {
        this.transform = transform;
        this.previousTransform = transform;
        this.interpolatedTransform = transform;
    }

    public virtual void Tick()
    {
        Coordinates newChunkCoordinates = GetChunkCoordinates();
        if (newChunkCoordinates != this.chunkCoordinates)
        {
            World.Chunks.At(chunkCoordinates)!.Entities.Remove(this);
            World.Chunks.At(newChunkCoordinates)!.Entities.Add(this);
            this.chunkCoordinates = newChunkCoordinates;
        }
        previousTransform = transform;
    }

    public virtual void Update()
    {
        interpolatedTransform = Transform.Lerp(this.previousTransform, this.transform, GameClient.TickProgress);
    }

    public virtual void Draw(CommandBuffer commandBuffer, RenderPass pass)
    {
    }
}
