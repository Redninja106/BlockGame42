using BlockGame42.Blocks;
using BlockGame42.Chunks;
using BlockGame42.Rendering;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Blocks;
internal class BlockRegistry
{
    public readonly EmptyBlock Air = new EmptyBlock();
    public readonly EmptyBlock Unloaded = new EmptyBlock();
    public readonly SolidBlock Unknown = new SolidBlock("unknown", null, new(255, 255, 255));
    public readonly DynamicBlock Stone = new DynamicBlock("stone", new(100, 20, 20));
    public readonly SolidBlock Dirt = new SolidBlock("dirt", null, new(6, 0, 1));
    public readonly SolidBlock Glowstone = new SolidBlock("glowstone", "glowstone", new(10, 10, 10));

    // public readonly Block MechanicalMachineChassis = new MachineChassisBlock();

    public BlockRegistry()
    {
    }
}

class HalfBlock : Block
{
    public override BlockModel Model { get; }

    public override BlockPlacementHandler PlacementHandler => BlockPlacementHandler.Half;

    public HalfBlock(uint texture, BlockStrength strength) : base(strength)
    {
        Model = new HalfBlockModel(Material.CreateUniform(texture, 0));
    }
}

class HalfBlockModel : BlockModel
{
    Material material;

    BlockMesh halfMesh;
    BlockMesh fullMesh;

    public HalfBlockModel(Material material)
    {
        this.material = material;

        halfMesh = BlockMesh.CreateFromModel(Game.graphics, this, default);
        fullMesh = BlockMesh.CreateFromModel(Game.graphics, this, new() { HalfBlock = new(true, 0) });
    }

    public override BlockFaceMask GetFaceMask(BlockState state, Direction direction)
    {
        if (state.HalfBlock.Doubled)
        {
            return BlockFaceMask.Full;
        }

        if (direction == Direction.Down)
        {
            return BlockFaceMask.Full;
        }
        if (direction == Direction.Up)
        {
            return BlockFaceMask.Empty;
        }
        return BlockFaceMask.BottomHalf;
    }

    public override Material GetMaterial(BlockState state)
    {
        return material;
    }

    public override void AddInternalFaces(BlockState state, BlockMeshBuilder mesh)
    {
        if (!state.HalfBlock.Doubled)
        {
            mesh.AppendFace(
                new Vector3(0, .5f, 0),
                Vector3.UnitX,
                Vector3.UnitZ,

                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),

                this.material.Data.Transmission.Up,
                this.material.Data.Emission.Up
                );
        }
    }

    public override bool Intersect(BlockState state, Box box)
    {
        Box localBox = new(new(0, 0, 0), new(1f, state.HalfBlock.Doubled ? 1f : .5f, 1f));
        return box.Intersects(localBox);
    }

    public override bool Raycast(BlockState state, Ray ray, ref float t, ref Coordinates normal)
    {
        Box localCollider = new(Vector3.Zero, new Vector3(1, state.HalfBlock.Doubled ? 1f : .5f, 1));

        if (localCollider.Raycast(ray, out float tNear, out float tFar, out normal))
        {
            if (tNear >= 0)
            {
                t = tNear;
            }
            else
            {
                t = tFar;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public override BlockMesh GetMesh(BlockState state, out Matrix4x4 transform)
    {
        transform = Matrix4x4.Identity;
        
        if (state.HalfBlock.Doubled)
        {
            return fullMesh;
        }
        else
        {
            return halfMesh;
        }
    }

    public override ulong GetVolumeMask(BlockState state)
    {
        return state.HalfBlock.Doubled ? 0xFFFFFFFFFFFFFFFFul : 0x00000000FFFFFFFFul;
    }
}