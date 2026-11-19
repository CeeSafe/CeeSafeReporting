// The reference harness. `model <file.frx>` loads a report with CosmoReport and writes its object model as text, in the
// form the port's model dump writes (src/model_dump.cee), so the two compare line by line.
//
// Every length is in thousandths, rounded half away from zero from the float as a double: the port does the same
// arithmetic on the same IEEE values. Text is escaped: \\ \n \r \t.

using System.Globalization;
using System.Text;
using FastReport;
using FastReport.Bands;
using FastReport.Drawing;
using FastReport.Format;
using FastReport.Model;
using FastReport.Objects;

// The port has the invariant culture only; CosmoReport's "current culture" formats are run in it.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
if (args.Length == 1 && args[0] == "values")
{
    Console.Out.Write(Values.Table());
    return 0;
}
if (args.Length is 1 or 2 && args[0] == "sample")
{
    Console.Out.Write(Texts.SampleJson(args.Length == 2 ? int.Parse(args[1]) : 7));
    return 0;
}
if (args.Length == 4 && args[0] == "texts")
{
    Console.Out.Write(Texts.Evaluate(args[1], args[2], args[3]));
    return 0;
}
if (args.Length is 5 or 6 && args[0] == "image")
{
    Console.Out.Write(Prepared.Image(args[1], args[2], args[3], args[4], args.Length == 6 && args[5] == "notext"));
    return 0;
}
if (args.Length == 3 && args[0] == "lines")
{
    Console.Out.Write(Lines.Run(args[1], args[2]));
    return 0;
}
if (args.Length == 3 && args[0] == "shape")
{
    Console.Out.Write(Shape.Run(args[1], args[2]));
    return 0;
}
if (args.Length == 2 && args[0] == "qr")
{
    Console.Out.Write(Qr.Run(args[1]));
    return 0;
}
if (args.Length == 5 && args[0] == "escpos")
{
    Console.Out.Write(Prepared.EscPos(args[1], args[2], args[3], args[4]));
    return 0;
}
if (args.Length == 5 && args[0] == "pdf")
{
    Console.Out.Write(Prepared.Pdf(args[1], args[2], args[3], args[4]));
    return 0;
}
if (args.Length == 4 && args[0] == "prepare")
{
    Console.Out.Write(Prepared.Run(args[1], args[2], args[3]));
    return 0;
}
if (args.Length == 5 && args[0] == "exprs")
{
    Console.Out.Write(Texts.Expressions(args[1], args[2], args[3], args[4]));
    return 0;
}
if (args.Length != 2 || args[0] != "model")
{
    Console.Error.WriteLine("usage: RefDump model <file.frx> | values | sample [items] | texts <file.frx> <data.json> <name> | prepare <file.frx> <data.json> <name> | escpos <file.frx> <data.json> <name> <out> | pdf <file.frx> <data.json> <name> <out> | image <file.frx> <data.json> <name> <out> [notext] | qr <list> | shape <font.ttf> <list> | lines <font.ttf> <list> | exprs <file.frx> <data.json> <name> <list>");
    return 2;
}
var report = new Report { Now = () => Prepared.TestNow };
report.Load(args[1]);
var o = new StringBuilder();
Dump.Report(report, o);
Console.Out.Write(o.ToString());
return 0;

static class Dump
{
    public static string M(float v) => Math.Round((double)v * 1000.0, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);

    public static string Q(string? s)
    {
        var b = new StringBuilder("\"");
        foreach (var c in s ?? "")
            b.Append(c switch { '\\' => "\\\\", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", '"' => "\\\"", _ => c.ToString() });
        return b.Append('"').ToString();
    }

    public static string C(FrColor c) => $"#{c.Alpha:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    public static string B(bool v) => v ? "1" : "0";

    static string Line(BorderLine l) => $"{C(l.Color)}/{l.Style}/{M(l.Width)}";

    public static string Fill(FillBase f) => f is SolidFill s ? C(s.Color) : f.GetType().Name;

    static string Format(FormatBase? f) => f switch
    {
        null => "none",
        NumberFormat n => $"Number({n.DecimalDigits},{Q(n.DecimalSeparator)},{Q(n.GroupSeparator)},{n.NegativePattern},{B(n.UseLocale)})",
        CurrencyFormat c => $"Currency({c.DecimalDigits},{Q(c.DecimalSeparator)},{Q(c.GroupSeparator)},{Q(c.CurrencySymbol)},{c.PositivePattern},{c.NegativePattern},{B(c.UseLocale)})",
        PercentFormat p => $"Percent({p.DecimalDigits})",
        DateFormat d => $"Date({Q(d.FormatString)})",
        TimeFormat t => $"Time({Q(t.FormatString)})",
        CustomFormat c => $"Custom({Q(c.FormatString)})",
        BooleanFormat b => $"Boolean({Q(b.TrueText)},{Q(b.FalseText)})",
        _ => f.GetType().Name,
    };

    public static void Report(Report r, StringBuilder o)
    {
        foreach (var t in r.Totals.Values.OrderBy(t => t.Name, StringComparer.Ordinal))
            o.Append($"Total {Q(t.Name)} expr={Q(t.Expression)} evaluator={Q(t.Evaluator)} printOn={Q(t.PrintOn)} type={t.TotalType} resetAfterPrint={B(t.ResetAfterPrint)} resetOnReprint={B(t.ResetOnReprint)}\n");
        o.Append($"Script {B(!string.IsNullOrEmpty(r.ScriptText))}\n");
        foreach (var p in r.Pages)
            if (p is ReportPage page)
                Page(page, o);
    }

    static void Page(ReportPage p, StringBuilder o)
    {
        o.Append($"ReportPage {Q(p.Name)} paper={M(p.PaperWidth)},{M(p.PaperHeight)} margins={M(p.LeftMargin)},{M(p.TopMargin)},{M(p.RightMargin)},{M(p.BottomMargin)} border={Border(p.Border)}\n");
        foreach (var b in p.Bands)
            if (b is ReportComponentBase c)
                Component(c, 1, "", o);
    }

    static string Border(Border b) => b.Lines == BorderLines.None
        ? "none"
        : $"{(int)b.Lines}:{Line(b.LeftLine)}:{Line(b.RightLine)}:{Line(b.TopLine)}:{Line(b.BottomLine)}";

    static void Component(ReportComponentBase c, int depth, string role, StringBuilder o)
    {
        o.Append(' ', depth * 2);
        if (role.Length > 0) o.Append(role).Append(':');
        o.Append($"{c.GetType().Name} {Q(c.Name)} box={M(c.Left)},{M(c.Top)},{M(c.Width)},{M(c.Height)} visible={B(c.Visible)} grow={B(c.CanGrow)} shrink={B(c.CanShrink)} printOn={(int)c.PrintOn} border={Border(c.Border)} fill={Fill(c.Fill)} beforePrint={Q(c.BeforePrintEvent)}");
        if (c is BandBase band)
        {
            o.Append($" startNewPage={B(band.StartNewPage)} printOnBottom={B(band.PrintOnBottom)}");
            if (band is HeaderFooterBandBase hf) o.Append($" repeat={B(hf.RepeatOnEveryPage)} keepWithData={B(hf.KeepWithData)}");
            if (band is DataBand db) o.Append($" dataSource={Q(db.DataSourceName)} filter={Q(db.Filter)} maxRows={db.MaxRows} rowCount={db.RowCount}");
            if (band is GroupHeaderBand gh) o.Append($" condition={Q(gh.Condition)} keepTogether={B(gh.KeepTogether)}");
        }
        if (c is TextObject t)
        {
            o.Append($" text={Q(t.Text)} font={Q(t.FontFamily)},{M(t.FontSize)},{(int)t.FontStyle.Weight},{t.FontStyle.Slant} color={C(t.TextColor)} align={t.HorzAlign},{t.VertAlign} wrap={B(t.WordWrap)} expr={B(t.AllowExpressions)} shrinkText={B(t.AutoShrink)} angle={M(t.Angle)} rtl={B(t.RightToLeft)} trimming={Q(t.Trimming)} pad={M(t.PadLeft)},{M(t.PadTop)},{M(t.PadRight)},{M(t.PadBottom)} format={Format(t.Format)}");
            if (t is TableCell cell) o.Append($" span={cell.ColSpan},{cell.RowSpan}");
        }
        if (c is PictureObject pic) o.Append($" sizeMode={pic.SizeMode} image={pic.ImageData?.Length ?? 0} location={Q(pic.ImageLocation)} dataColumn={Q(pic.DataColumn)}");
        if (c is LineObject line) o.Append($" diagonal={B(line.Diagonal)}");
        if (c is BarcodeObject bc) o.Append($" barcode={Q(bc.BarcodeType)} text={Q(bc.Text)} ecc={Q(bc.ErrorCorrection)} showText={B(bc.ShowText)} autoSize={B(bc.AutoSize)}");
        if (c is SubreportObject sr) o.Append($" page={Q(sr.ReportPage)} printOnParent={B(sr.PrintOnParent)}");
        o.Append('\n');

        if (c is TableObject table)
        {
            foreach (var col in table.Columns) o.Append(' ', depth * 2 + 2).Append($"TableColumn {Q(col.Name)} width={M(col.ColumnWidth)}\n");
            foreach (var row in table.Rows)
            {
                o.Append(' ', depth * 2 + 2).Append($"TableRow {Q(row.Name)} height={M(row.RowHeight)}\n");
                foreach (var cell in row.Cells) Component(cell, depth + 2, "", o);
            }
        }
        if (c is TableCell tc)
            foreach (var inner in tc.Objects) Component(inner, depth + 1, "", o);
        if (c is BandBase b)
        {
            foreach (var obj in b.Objects) Component(obj, depth + 1, "", o);
            if (b is DataBand d)
            {
                if (d.Header != null) Component(d.Header, depth + 1, "header", o);
                foreach (var detail in d.DetailBands) Component(detail, depth + 1, "detail", o);
                if (d.Footer != null) Component(d.Footer, depth + 1, "footer", o);
            }
            if (b is GroupHeaderBand g)
            {
                if (g.NestedGroup != null) Component(g.NestedGroup, depth + 1, "nested", o);
                if (g.Data != null) Component(g.Data, depth + 1, "data", o);
                if (g.GroupFooter != null) Component(g.GroupFooter, depth + 1, "groupFooter", o);
            }
            if (b.Child != null) Component(b.Child, depth + 1, "child", o);
        }
    }
}
