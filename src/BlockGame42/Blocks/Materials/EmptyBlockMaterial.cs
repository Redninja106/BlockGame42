using System;
using System.Collections.Generic;
using System.Text;

namespace BlockGame42.Blocks.Materials;

internal class EmptyBlockMaterial : BlockMaterial
{
    public EmptyBlockMaterial()
    {
        this.TexID = uint.MaxValue;
    }
}
