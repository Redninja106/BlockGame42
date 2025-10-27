using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BlockGame42.Assets.Pipeline;
internal class FontPipeline : AssetPipeline
{
    public override string PrimaryInputExtension => ".ttf";
    public override string PrimaryOutputExtension => ".font.json";

    const int spacing = 2;
    const int fontsize = 32;
    const float shadow = 1;
    const int atlasWidth = 256;
    const int atlasHeight = 256;

    int[] codepoints;
    string[] includedChars = [
        "\0 ",
        "abcdefghijklmnopqrstuvwxyz",
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        "0123456789",
        "!@#$%^&*",
        "`~_+-=",
        "()[]{}",
        ",<.>;:/?'\"",
        ];
    
    public FontPipeline()
    {
        codepoints = includedChars.SelectMany(s => s.ToCharArray()).Select(c => (int)c).ToArray();
    }

    public override void Build(AssetBuildContext context)
    {
        SKTypeface typeface = SKTypeface.FromFile(context.InputFile);
        SKFont font = new(typeface, fontsize);

        JsonObject fontDict = new();
        JsonObject glyphInfoDict = new();

        fontDict.Add("ascent", font.Metrics.Ascent);
        fontDict.Add("descent", font.Metrics.Descent);
        fontDict.Add("leading", font.Metrics.Leading);
        fontDict.Add("glyphs", glyphInfoDict);

        SKBitmap fontAtlas = new(atlasWidth, atlasHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        SKCanvas canvas = new(fontAtlas);

        SKPaint paint = new();
        paint.ColorF = new SKColorF(1, 1, 1, 1);
        paint.Style = SKPaintStyle.Fill;
        paint.IsAntialias = true;

        canvas.Clear(new SKColor(0, 0, 0, 0));

        Vector2 position = new(0, -font.Metrics.Ascent);
        for (int i = 0; i < codepoints.Length; i++)
        {
            // int x = cellSize * (i % glyphsPerRow);
            // int y = (fontsize + 2 * spacing) * (i / glyphsPerRow);

            ushort glyph = font.GetGlyph(codepoints[i]);
            
            float advance = font.MeasureText([glyph], out SKRect bounds);
            bounds.Right += shadow;
            bounds.Bottom += shadow;

            if (position.X + bounds.Width >= atlasWidth)
            {
                position.X = 0;
                position.Y += -font.Metrics.Ascent + spacing;
                i--;
                continue;
            }

            SKRect atlasBounds = bounds;
            atlasBounds.Location += new SKPoint(position.X, position.Y);

            glyphInfoDict.Add(codepoints[i].ToString("x4"), new JsonObject()
            {
                ["advance"] = advance,
                ["left"] =   bounds.Left,
                ["top"] =    bounds.Top,
                ["right"] =  bounds.Right,
                ["bottom"] = bounds.Bottom,
                ["uv_left"] = atlasBounds.Left / atlasWidth,
                ["uv_top"] = atlasBounds.Top / atlasHeight,
                ["uv_right"] = atlasBounds.Right / atlasWidth,
                ["uv_bottom"] = atlasBounds.Bottom / atlasHeight,
            });

            paint.ColorF = new(0, 0, 0, .75f);
            canvas.SetMatrix(SKMatrix.CreateTranslation(position.X + shadow, position.Y + shadow));
            canvas.DrawPath(font.GetGlyphPath(glyph), paint);

            paint.ColorF = new(1, 1, 1, 1);
            canvas.SetMatrix(SKMatrix.CreateTranslation(position.X, position.Y));
            canvas.DrawPath(font.GetGlyphPath(glyph), paint);

            //paint.Style = SKPaintStyle.Stroke;
            //canvas.DrawRect(bounds, paint);
            //paint.Style = SKPaintStyle.Fill;

            position.X += bounds.Width + spacing;
        }

        File.WriteAllText(context.GetOutputFilePath(PrimaryOutputExtension), fontDict.ToJsonString());

        Span<byte> encodedImage = TexturePipeline.EncodeImage(fontAtlas.Width, fontAtlas.Height, fontAtlas.GetPixelSpan());
        File.WriteAllBytes(context.GetOutputFilePath(".texture"), encodedImage);

        // using SKData data = fontAtlas.Encode(SKEncodedImageFormat.Png, 0);
        // File.WriteAllBytes(context.GetOutputFilePath(".debug.png"), data.AsSpan());
    }
}
