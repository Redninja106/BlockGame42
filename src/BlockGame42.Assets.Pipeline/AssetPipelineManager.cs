using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Assets.Pipeline;
internal class AssetPipelineManager
{
    private Dictionary<string, AssetPipeline> pipelines = new()
    {
        [".slang"] = new ShaderPipeline(),
        [".png"] = new TexturePipeline(),
        [".ttf"] = new FontPipeline(),
        // ["blend"] = new BlendPipeline(),
    };

    public int AssetsBuilt { get; private set; } = 0;

    public void BuildDirectory(string directory, string outDirectory, string baseName = "")
    {
        Directory.CreateDirectory(outDirectory);

        foreach (var file in Directory.GetFiles(directory))
        {
            if (!pipelines.ContainsKey(Path.GetExtension(file)))
            {
                Console.WriteLine("Skipping file with unknown extension: " + file);
                continue;
            }

            var pipeline = pipelines[Path.GetExtension(file)];
            
            AssetBuildContext context = new(file, outDirectory);

            DateTime assetWriteTime = File.GetLastWriteTime(file);
            DateTime outputWriteTime = File.GetLastWriteTime(context.GetOutputFilePath(pipeline.PrimaryOutputExtension, silent: true));
            if (assetWriteTime < outputWriteTime)
            {
                continue;
            }

            pipeline.Build(context);
            AssetsBuilt++;
        }

        foreach (var subdir in Directory.GetDirectories(directory))
        {
            BuildDirectory(subdir, outDirectory);
        }
    }
}
