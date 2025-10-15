using BlockGame42.Assets.Pipeline;

// debugging args
if (args.Length == 0)
{
}

Console.WriteLine("building assets...");
Console.WriteLine("input directory: " + args[0]);
Console.WriteLine("output directory: " + args[1]);

AssetPipelineManager pipelines = new();
pipelines.BuildDirectory(args[0], args[1]);
Console.WriteLine($"Finished building {pipelines.AssetsBuilt} assets");
