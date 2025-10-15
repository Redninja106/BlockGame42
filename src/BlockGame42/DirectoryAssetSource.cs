using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;
internal class DirectoryAssetSource : IAssetSource
{
    readonly string rootDirectory;

    public DirectoryAssetSource(string rootDirectory)
    {
        this.rootDirectory = rootDirectory;
    }

    public byte[] Load(string assetName)
    {

        return File.ReadAllBytes(Path.Combine(rootDirectory, assetName));
    }

    public string LoadText(string assetName)
    {
        return File.ReadAllText(Path.Combine(rootDirectory, assetName));
    }
}
