// `prepare <frx> <json> <name>`: the JSON registered under <name> as the server registers its object, the report run
// by CosmoReport's engine (Report.Prepare, its script compiled and run), and the prepared pages written as text in the
// form the port's dump writes (src/prepared_dump.cee). No text measurer is installed, so a growing text counts its
// lines by its newlines, as the port's engine does until it is given one.

using System.Text;
using System.Text.Json;
using FastReport;
using FastReport.Bands;
using FastReport.Drawing;
using FastReport.Model;
using FastReport.Objects;
using FastReport.ZatcaTest;

static class Prepared
{
    public static string Run(string frx, string jsonPath, string name)
    {
        var invoice = JsonSerializer.Deserialize<MyInvoice>(File.ReadAllText(jsonPath))!;
        // No text measurer: a counter that never answers, so a growing text counts its newlines (and the bridge
        // CosmoPdf.FastReport would install is not looked for).
        TextMeasureBridge.LineCounter = (_, _, _, _, _, _) => null;
        var report = new Report();
        report.Load(frx);
        report.RegisterData(new List<MyInvoice> { invoice }, name, 5);
        return DumpPages(report.Prepare());
    }

    /// The report prepared as production prepares it (the font line counter and QR encoder of CosmoPdf.FastReport
    /// installed) and exported by ImageExport (monochrome, 203 dpi): `<out>.prepared.txt` and `<out>-<page>.png`.
    public static string Image(string frx, string jsonPath, string name, string outPrefix, bool noText = false)
    {
        CosmoPdf.FastReport.CoreTextMeasureBridge.Install();
        CosmoPdf.FastReport.CoreQrBridge.Install();
        var invoice = JsonSerializer.Deserialize<MyInvoice>(File.ReadAllText(jsonPath))!;
        var report = new Report();
        report.Load(frx);
        report.RegisterData(new List<MyInvoice> { invoice }, name, 5);
        var pages = report.Prepare();
        File.WriteAllText(outPrefix + ".prepared.txt", DumpPages(pages));
        if (noText)
        {
            // Every text and table cell emptied: the page without its text, to compare what is not text exactly.
            var set = typeof(TextObject).GetMethod("SetProcessedText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            for (int pi = 0; pi < pages.Count; pi++)
                foreach (var band in pages[pi].Bands)
                    foreach (var obj in band.Objects)
                    {
                        if (obj is TextObject t) set.Invoke(t, [""]);
                        if (obj is TableObject table)
                            foreach (var row in table.Rows)
                                foreach (var cell in row.Cells) set.Invoke(cell, [""]);
                    }
        }
        var export = new CosmoPdf.FastReport.ImageExport();
        report.Export(export, new MemoryStream());
        for (int i = 0; i < export.PageImages.Count; i++)
            File.WriteAllBytes($"{outPrefix}-{i + 1}.png", export.PageImages[i]);
        return $"{export.PageImages.Count} pages\n";
    }

    static string DumpPages(PreparedPages pages)
    {
        var o = new StringBuilder();
        for (int i = 0; i < pages.Count; i++)
        {
            var p = pages[i];
            o.Append($"Page {i + 1} {Dump.Q(p.SourcePage.Name)}\n");
            foreach (var b in p.Bands)
                Component(b, 1, o);
        }
        return o.ToString();
    }

    static void Component(ReportComponentBase c, int depth, StringBuilder o)
    {
        o.Append(' ', depth * 2);
        o.Append($"{c.GetType().Name} {Dump.Q(c.Name)} box={Dump.M(c.Left)},{Dump.M(c.Top)},{Dump.M(c.Width)},{Dump.M(c.Height)} fill={Dump.Fill(c.Fill)}");
        if (c is BandBase band) o.Append($" repeated={Dump.B(band.Repeated)}");
        if (c is TextObject t)
            o.Append($" text={Dump.Q(t.ProcessedText)} color={Dump.C(t.TextColor)} font={Dump.Q(t.FontFamily)},{Dump.M(t.FontSize)},{(int)t.FontStyle.Weight},{t.FontStyle.Slant}");
        if (c is PictureObject pic) o.Append($" image={pic.ImageData?.Length ?? 0}");
        if (c is BarcodeObject bc) o.Append($" text={Dump.Q(bc.ProcessedText)}");
        o.Append('\n');
        if (c is TableObject table)
            foreach (var row in table.Rows)
            {
                o.Append(' ', depth * 2 + 2).Append($"TableRow {Dump.Q(row.Name)} height={Dump.M(row.RowHeight)}\n");
                foreach (var cell in row.Cells) Component(cell, depth + 2, o);
            }
        if (c is BandBase b)
            foreach (var obj in b.Objects) Component(obj, depth + 1, o);
    }
}
