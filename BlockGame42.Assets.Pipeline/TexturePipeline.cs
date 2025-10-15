using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Assets.Pipeline;
internal class TexturePipeline : AssetPipeline
{
    public override string PrimaryInputExtension => ".png";
    public override string PrimaryOutputExtension => ".texture";

    public unsafe override void Build(AssetBuildContext context)
    {
        SKBitmap bitmap = SKBitmap.Decode(File.ReadAllBytes(context.InputFile));
        bitmap = bitmap.Copy(SKColorType.Rgba8888);

        nint pixelsPtr = bitmap.GetPixels(out nint length);
        Span<byte> pixelData = new Span<byte>((void*)pixelsPtr, (int)length);
        byte[] finalData = EncodeImage(bitmap.Width, bitmap.Height, pixelData);

        File.WriteAllBytes(context.GetOutputFilePath(".texture"), finalData);
    }

    public static byte[] EncodeImage(int width, int height, Span<byte> pixelData)
    {
        Span<byte> widthBytes = MemoryMarshal.AsBytes<int>(new(ref width));
        Span<byte> heightBytes = MemoryMarshal.AsBytes<int>(new(ref height));

        return [.. widthBytes, .. heightBytes, ..pixelData];
    }
}
