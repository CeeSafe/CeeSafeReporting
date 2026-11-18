// `shape <font.ttf> <list>`: each line of <list> shaped by CosmoFonts as ImageExport shapes a text line (TextShaping.
// ShapeLine), one line per glyph: "gid advance xOffset yOffset" in font units, then the legacy kern the renderer adds
// before it (0 when the face's GPOS kerns).

using System.Reflection;
using System.Text;
using CosmoFonts.Loader;
using CosmoFonts.Shaping;

static class Shape
{
    public static string Run(string fontPath, string listPath)
    {
        var face = Face.Load(File.ReadAllBytes(fontPath));
        var shapeLine = typeof(TextMeasurer).Assembly.GetType("CosmoFonts.Shaping.TextShaping")!
            .GetMethod("ShapeLine", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var legacy = (bool)typeof(TextMeasurer).GetMethod("LegacyKernAppliesForFace",
            BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [face])!;
        var options = new TextOptions(face, 40f);
        var o = new StringBuilder();
        foreach (var line in File.ReadAllLines(listPath))
        {
            o.Append($"# {line}\n");
            var glyphs = (List<Glyph>)shapeLine.Invoke(null, [line, options])!;
            uint prev = 0;
            bool first = true;
            foreach (var g in glyphs)
            {
                int kern = !first && legacy && face.Kern is { } k ? (int)k.Lookup(prev, g.GlyphId) : 0;
                o.Append($"{g.GlyphId} {g.XAdvance} {g.XOffset} {g.YOffset} {kern}\n");
                prev = g.GlyphId;
                first = false;
            }
        }
        return o.ToString();
    }
}
