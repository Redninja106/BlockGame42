using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal interface IAssetSource
{
    byte[] Load(string assetName);
    string LoadText(string assetName);
}
