using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Assets.Pipeline;
internal class AssetBuildContext
{
    public readonly string outDirectory;

    public string InputFile { get; }

    public AssetBuildContext(string inputFile, string outDirectory)
    {
        InputFile = inputFile;
        this.outDirectory = outDirectory;
    }

    public string GetOutputFilePath(string extension, bool silent = false)
    {
        if (!silent)
        {
            Console.WriteLine($"{Path.GetFileName(InputFile)} > {Path.GetFileNameWithoutExtension(InputFile)}{extension}");
        }
        return Path.Combine(outDirectory, Path.GetFileNameWithoutExtension(InputFile)) + extension;
    }
}
