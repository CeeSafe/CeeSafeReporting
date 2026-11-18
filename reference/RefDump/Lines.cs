// `lines <font.ttf> <list>`: each line of <list> ("\n" in it a line end) broken by CosmoPdf's FontMeasure.LineBreak,
// as CoreTextMeasureBridge counts a growing text's lines: at 8, 10 and 12 points in 40, 120 and 300 report units
// (3/4 of a point each). One line per case: "<size> <width> <count>".

using System.Text;
using CosmoPdf.Pdf.Font;

static class Lines
{
    public static string Run(string fontPath, string listPath)
    {
        var ttf = TrueTypeFont.Parse(File.ReadAllBytes(fontPath));
        var o = new StringBuilder();
        foreach (var raw in File.ReadAllLines(listPath))
        {
            var text = raw.Replace("\\n", "\n").Replace("\\r", "\r");
            o.Append($"# {raw}\n");
            foreach (var size in new[] { 8f, 10f, 12f })
                foreach (var width in new[] { 40f, 120f, 300f })
                    o.Append($"{size} {width} {FontMeasure.LineBreak(ttf, text, size, width * 0.75).Count}\n");
        }
        return o.ToString();
    }
}
