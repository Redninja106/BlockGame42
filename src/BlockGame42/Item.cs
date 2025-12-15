using BlockGame42.Blocks;
using Protor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal class Item : Prototype
{

}

class BlockItem : Item
{
    public Block Block { get; }

    public BlockItem(Block block)
    {
        this.Block = block;
    }
}
