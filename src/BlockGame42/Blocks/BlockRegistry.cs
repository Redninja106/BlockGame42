using BlockGame42.Blocks;
using BlockGame42.Chunks;
using BlockGame42.Rendering;
using Protor;
using SDL.GPU;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Blocks;
internal static class BlockRegistry
{
    public static Block Air => field ??= Registry.Get<Block>("air");// new EmptyBlock();
    public static Block Unloaded => field ??= Registry.Get<Block>("unloaded");// new EmptyBlock();
    public static Block Unknown => field ??= Registry.Get<Block>("unknown");// new SolidBlock("unknown", null, new(255, 255, 255));
    public static Block Stone => field ??= Registry.Get<Block>("stone");// new SolidBlock("stone", null, new(100, 20, 20));
    public static Block Dirt => field ??= Registry.Get<Block>("dirt");// new SolidBlock("dirt", null, new(6, 0, 1));
    public static Block Glowstone => field ??= Registry.Get<Block>("glowstone");// new SolidBlock("glowstone", "glowstone_emissive", new(10, 10, 10));
    public static Block IronBlock => field ??= Registry.Get<Block>("iron_block");// new SolidBlock("iron_block", "iron_emissive", new(10, 10, 10));
}
