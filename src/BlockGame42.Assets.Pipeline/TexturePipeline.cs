using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame42.Assets.Pipeline;
internal class TexturePipeline : AssetPipeline
{
    public override string PrimaryInputExtension => ".png";
    public override string PrimaryOutputExtension => ".texture";

    public unsafe override void Build(AssetBuildContext context)
    {
        Image<Rgba32> image = Image.Load<Rgba32>(File.ReadAllBytes(context.InputFile));

        byte[] pixelData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixelData);
        byte[] finalData = EncodeImage(image.Width, image.Height, pixelData);
        Console.WriteLine(string.Join(",", pixelData.ToArray()));

        File.WriteAllBytes(context.GetOutputFilePath(".texture"), finalData);
    }

    public static byte[] EncodeImage(int width, int height, Span<byte> pixelData)
    {
        Span<byte> widthBytes = MemoryMarshal.AsBytes<int>(new Span<int>(ref width));
        Span<byte> heightBytes = MemoryMarshal.AsBytes<int>(new Span<int>(ref height));

        return [.. widthBytes, .. heightBytes, ..pixelData];
    }
}
