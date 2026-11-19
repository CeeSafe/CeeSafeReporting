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
    /// The time [Date] prints in every test: the port's tests give its engine the same.
    internal static readonly DateTime TestNow = new(2026, 11, 18, 14, 54, 0);

    public static string Run(string frx, string jsonPath, string name)
    {
        // No text measurer: a counter that never answers, so a growing text counts its newlines (and the bridge
        // CosmoPdf.FastReport would install is not looked for).
        TextMeasureBridge.LineCounter = (_, _, _, _, _, _) => null;
        var report = new Report { Now = () => TestNow };
        report.Load(frx);
        RegisterJson(report, jsonPath, name);
        return DumpPages(report.Prepare());
    }

    /// The report prepared as production prepares it (the font line counter and QR encoder of CosmoPdf.FastReport
    /// installed) and exported by ImageExport (monochrome, 203 dpi): `<out>.prepared.txt` and `<out>-<page>.png`.
    public static string Image(string frx, string jsonPath, string name, string outPrefix, bool noText = false)
    {
        CosmoPdf.FastReport.CoreTextMeasureBridge.Install();
        CosmoPdf.FastReport.CoreQrBridge.Install();
        var report = new Report { Now = () => TestNow };
        report.Load(frx);
        RegisterJson(report, jsonPath, name);
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

    /// The report prepared as production prepares it and exported by EscPosExport: the bytes to `out`.
    public static string EscPos(string frx, string jsonPath, string name, string outPath)
    {
        CosmoPdf.FastReport.CoreTextMeasureBridge.Install();
        CosmoPdf.FastReport.CoreQrBridge.Install();
        var report = new Report { Now = () => TestNow };
        report.Load(frx);
        RegisterJson(report, jsonPath, name);
        report.Prepare();
        using var stream = new MemoryStream();
        report.Export(new CosmoPdf.FastReport.EscPosExport(), stream);
        File.WriteAllBytes(outPath, stream.ToArray());
        return $"{stream.Length} bytes\n";
    }

    /// The report prepared as production prepares it and exported by CosmoPdfExport (its defaults): the PDF to `out`.
    public static string Pdf(string frx, string jsonPath, string name, string outPath)
    {
        CosmoPdf.FastReport.CoreTextMeasureBridge.Install();
        CosmoPdf.FastReport.CoreQrBridge.Install();
        var report = new Report { Now = () => TestNow };
        report.Load(frx);
        RegisterJson(report, jsonPath, name);
        report.Prepare();
        using var stream = new MemoryStream();
        report.Export(new CosmoPdf.FastReport.CosmoPdfExport(), stream);
        File.WriteAllBytes(outPath, stream.ToArray());
        File.WriteAllText(outPath + ".txt", PdfDump(stream.ToArray()), new UTF8Encoding(false));
        return $"{stream.Length} bytes\n";
    }

    /// The PDF as the port's writer dumps one (src/pdf_writer.cee): each object in file order, a stream's dictionary
    /// without its Length and its content inflated (a font program or an image as its size and CRC-32), then the
    /// trailer.
    static string PdfDump(byte[] pdf)
    {
        var latin = Encoding.Latin1;
        var o = new List<byte>();
        void Text(string t) => o.AddRange(Encoding.UTF8.GetBytes(t));
        int at = pdf.AsSpan().IndexOf("\n"u8) + 1;
        while (true)
        {
            var head = System.Text.RegularExpressions.Regex.Match(latin.GetString(pdf, at, Math.Min(32, pdf.Length - at)), @"^(\d+) (\d+) obj\n");
            if (!head.Success) break;
            at += head.Length;
            Text(head.Value);
            int end = pdf.AsSpan(at).IndexOf("\nendobj\n"u8);
            int stream = pdf.AsSpan(at).IndexOf("\nstream\n"u8);
            if (stream < 0 || stream > end)
            {
                o.AddRange(pdf.AsSpan(at, end).ToArray());
                Text("\nendobj\n");
                at += end + "\nendobj\n".Length;
                continue;
            }
            string dict = latin.GetString(pdf, at, stream);
            int length = int.Parse(System.Text.RegularExpressions.Regex.Match(dict, @" /Length (\d+)").Groups[1].Value);
            Text(System.Text.RegularExpressions.Regex.Replace(dict, @" /Length \d+", ""));
            Text("\nstream\n");
            int data = at + stream + "\nstream\n".Length;
            byte[] content = pdf.AsSpan(data, length).ToArray();
            if (dict.Contains("/FlateDecode"))
            {
                using var z = new System.IO.Compression.ZLibStream(new MemoryStream(content), System.IO.Compression.CompressionMode.Decompress);
                using var m = new MemoryStream();
                z.CopyTo(m);
                content = m.ToArray();
            }
            if (dict.Contains("/Length1 ") || dict.Contains("/Subtype /Image"))
                Text($"binary {content.Length} {Crc32(content):X8}");
            else
                o.AddRange(content);
            Text("\nendstream\nendobj\n");
            at = data + length + "\nendstream\nendobj\n".Length;
        }
        int trailer = pdf.AsSpan(at).IndexOf("trailer\n"u8) + at;
        int startxref = pdf.AsSpan(trailer).IndexOf("\nstartxref"u8) + trailer;
        o.AddRange(pdf.AsSpan(trailer, startxref - trailer).ToArray());
        Text("\n");
        return Encoding.UTF8.GetString(o.ToArray());
    }

    static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return ~crc;
    }

    /// The JSON registered under `name` as the ERP registers its object: a VoucherPrintModel for "Voucher", the
    /// server's MyInvoice otherwise.
    static void RegisterJson(Report report, string jsonPath, string name)
    {
        var json = File.ReadAllText(jsonPath);
        if (name == "Voucher")
            report.RegisterData(new List<VoucherPrint> { JsonSerializer.Deserialize<VoucherPrint>(json)! }, name, 5);
        else
            report.RegisterData(new List<MyInvoice> { JsonSerializer.Deserialize<MyInvoice>(json)! }, name, 5);
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
