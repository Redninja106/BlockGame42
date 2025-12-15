using BlockGame42.GUI;
using BlockGame42.Rendering;
using SDL.GPU;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BlockGame42;

class Font
{
    public string FontName { get; }

    public float Ascent { get; }
    public float Descent { get; }
    public float Leading { get; }

    public Texture Atlas { get; }


    private Glyph?[] glyphs;

    public Font(GraphicsContext graphics, IAssetSource assets, string fontName)
    {
        this.FontName = fontName;

        glyphs = new Glyph[256];

        JsonDocument fontJsonDoc = JsonDocument.Parse(assets.LoadText(fontName + ".font.json"));
        var fontJson = fontJsonDoc.RootElement;

        Ascent = fontJson.GetProperty("ascent").GetSingle();
        Descent = fontJson.GetProperty("descent").GetSingle();
        Leading = fontJson.GetProperty("leading").GetSingle();

        foreach (var key in fontJson.GetProperty("glyphs").EnumerateObject())
        {
            Glyph glyph = new();
            
            glyph.Advance = key.Value.GetProperty("advance").GetSingle();

            glyph.TopLeft.X = key.Value.GetProperty("left").GetSingle();
            glyph.TopLeft.Y = key.Value.GetProperty("top").GetSingle();
            glyph.BottomRight.X = key.Value.GetProperty("right").GetSingle();
            glyph.BottomRight.Y = key.Value.GetProperty("bottom").GetSingle();

            glyph.UV0.X = key.Value.GetProperty("uv_left").GetSingle();
            glyph.UV0.Y = key.Value.GetProperty("uv_top").GetSingle();
            glyph.UV1.X = key.Value.GetProperty("uv_right").GetSingle();
            glyph.UV1.Y = key.Value.GetProperty("uv_bottom").GetSingle();

            glyphs[int.Parse(key.Name, NumberStyles.HexNumber)] = glyph;
        }

        Atlas = graphics.LoadTexture(fontName);
    }

    public Glyph GetGlyph(char c)
    {
        if (c >= glyphs.Length)
        {
            return glyphs[0]!;
        }

        return glyphs[c] ?? glyphs[0]!;
    }

    public Extent Measure(ReadOnlySpan<char> text)
    {
        Vector2 position = Vector2.Zero;
        Extent result = default;
        for (int i = 0; i < text.Length; i++)
        {
            Glyph glyph = GetGlyph(text[i]);
            Extent glyphExtent = new(position + glyph.TopLeft, position + glyph.BottomRight);
            result = Extent.Union(result, glyphExtent);
            position.X += glyph.Advance;
        }
        return result;
    }

    public class Glyph
    {
        public float Advance;
        public Vector2 UV0;
        public Vector2 UV1;
        public Vector2 TopLeft;
        public Vector2 BottomRight;
    }
}
