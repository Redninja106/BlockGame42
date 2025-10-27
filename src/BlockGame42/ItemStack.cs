using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal class ItemStack
{
    public Item Item { get; private set; }
    public int Count { get; private set; }

    public ItemStack()
    { 

    }
}
