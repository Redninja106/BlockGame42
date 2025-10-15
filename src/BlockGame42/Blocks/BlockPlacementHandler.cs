using BlockGame42.Chunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Blocks;
internal abstract class BlockPlacementHandler
{
    public static readonly BlockPlacementHandler Solid = new SolidBlockPlacementHandler();
    public static readonly BlockPlacementHandler Half = new HalfBlockPlacementHandler();
    public static readonly BlockPlacementHandler Dynamic = new DynamicBlockPlacementHandler();

    public BlockPlacementHandler()
    {
    }

    public abstract void OnPlaceBlock(World world, Camera camera, Block block);
}

class SolidBlockPlacementHandler : BlockPlacementHandler
{
    public override void OnPlaceBlock(World world, Camera camera, Block block)
    {
        Ray ray = new(camera.transform.Position, camera.transform.Forward, 100);
        if (world.Raycast(ray, out float t, out Coordinates hitCoords, out Coordinates normal))
        {
            // if (!placingBlock.GetModel(placingState).Intersect(this.collider.Offset(this.Transform.Position - placeCoords.ToVector())))
            // {
            // }
            
            Coordinates placeCoords = hitCoords + normal;
            if (world.GetBlock(placeCoords, out _) == Game.Blocks.Air)
            {
                world.SetBlock(placeCoords, block);
            }
        }
    }
}

class HalfBlockPlacementHandler : BlockPlacementHandler
{
    public override void OnPlaceBlock(World world, Camera camera, Block block)
    {
        Ray ray = new(camera.transform.Position, camera.transform.Forward, 100);
        if (world.Raycast(ray, out float t, out Coordinates hitCoords, out Coordinates normal))
        {
            if (world.GetBlock(hitCoords, out BlockState state) is HalfBlock hb)
            {
                if (block == hb)
                {
                    if (!state.HalfBlock.Doubled)
                    {
                        if (normal == new Coordinates(0, 1, 0))
                        {
                            state.HalfBlock = new(true, state.HalfBlock.Direction);
                            world.SetBlock(hitCoords, hb, state);
                            return;
                        }
                    }
                }
            }

            Coordinates placeCoords = hitCoords + normal;

            switch (world.GetBlock(placeCoords, out state))
            {
                case EmptyBlock:
                    world.SetBlock(placeCoords, block);
                    break;

                case HalfBlock h:
                    BlockState placingState = new()
                    {
                        HalfBlock = new(true, state.HalfBlock.Direction),
                    };
                    world.SetBlock(placeCoords, h, placingState);
                    break;

            }
            

        }
    }
}

class DynamicBlockPlacementHandler : BlockPlacementHandler
{
    public override void OnPlaceBlock(World world, Camera camera, Block block)
    {
        Ray ray = new(camera.transform.Position, camera.transform.Forward, 100);
        if (world.Raycast(ray, out float t, out Coordinates hitCoords, out Coordinates normal))
        {
            Coordinates placeCoords = hitCoords + normal;
            if (world.GetBlock(placeCoords, out _) == Game.Blocks.Air)
            {
                world.SetBlock(placeCoords, block, new() { DynamicBlock = new(0xFF) });
            }
        }
    }
}