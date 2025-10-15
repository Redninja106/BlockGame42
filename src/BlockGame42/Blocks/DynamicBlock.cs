using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Blocks;
internal class DynamicBlock : Block
{
    public override BlockPlacementHandler PlacementHandler => BlockPlacementHandler.Dynamic;
    public override BlockModel Model { get; }

    public DynamicBlock(string texid, BlockStrength strength) : base(strength)
    {
        Model = new DynamicBlockModel(Game.Textures.Get(texid));
    }
}
