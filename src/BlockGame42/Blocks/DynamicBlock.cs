using BlockGame42.Chunks;
using BlockGame42.Rendering;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Blocks;
internal class DynamicBlock : Block
{
    public override BlockPlacementHandler PlacementHandler => BlockPlacementHandler.Dynamic;
    // public override BlockModel Model { get; }

    public override BlockState DefaultState => new BlockState { DynamicBlock = new(0xFF) };

    public DynamicBlock() : base()
    {
        // Model = new DynamicBlockModel(Material.CreateUniform(Game.Textures.Get(texid), Game.Textures.Empty));
    }

    public override bool Intersect(BlockState state, Box box)
    {
        foreach (var localColliders in GetLocalColliders(state))
        {
            if (localColliders.Intersects(box))
            {
                return true;
            }
        }

        return false;
    }

    private List<Box> GetLocalColliders(BlockState state)
    {
        if (state.DynamicBlock.Mask == 0xFF)
            return [new Box(Vector3.Zero, Vector3.One)];

        List<Box> result = [];
        // 0bYZX
        if (state.DynamicBlock[0b000]) result.Add(new Box(new(0.0f, 0.0f, 0.0f), new(0.5f, 0.5f, 0.5f)));
        if (state.DynamicBlock[0b001]) result.Add(new Box(new(0.5f, 0.0f, 0.0f), new(1.0f, 0.5f, 0.5f)));
        if (state.DynamicBlock[0b010]) result.Add(new Box(new(0.0f, 0.0f, 0.5f), new(0.5f, 0.5f, 1.0f)));
        if (state.DynamicBlock[0b011]) result.Add(new Box(new(0.5f, 0.0f, 0.5f), new(1.0f, 0.5f, 1.0f)));
        if (state.DynamicBlock[0b100]) result.Add(new Box(new(0.0f, 0.5f, 0.0f), new(0.5f, 1.0f, 0.5f)));
        if (state.DynamicBlock[0b101]) result.Add(new Box(new(0.5f, 0.5f, 0.0f), new(1.0f, 1.0f, 0.5f)));
        if (state.DynamicBlock[0b110]) result.Add(new Box(new(0.0f, 0.5f, 0.5f), new(0.5f, 1.0f, 1.0f)));
        if (state.DynamicBlock[0b111]) result.Add(new Box(new(0.5f, 0.5f, 0.5f), new(1.0f, 1.0f, 1.0f)));
        return result;
    }

    public override bool Raycast(BlockState state, Ray ray, ref float t, ref Coordinates normal)
    {
        var localColliders = GetLocalColliders(state);

        t = float.PositiveInfinity;
        foreach (var collider in localColliders)
        {
            if (collider.Raycast(ray, out float tNear, out float tFar, out Coordinates hitNormal))
            {
                float hitT = tNear > 0 ? tNear : tFar;
                if (hitT > 0 && hitT < t)
                {
                    t = hitT;
                    normal = hitNormal;
                }
            }
        }
        return t != float.PositiveInfinity;
    }

}
