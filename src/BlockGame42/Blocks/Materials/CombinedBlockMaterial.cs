using System;
using System.Collections.Generic;
using System.Text;

namespace BlockGame42.Blocks.Materials;

internal class CombinedBlockMaterial : BlockMaterial
{
    public string Albedo { get; init; }
    public string Normal { get; init; }
    public string Specular { get; init; }
}
