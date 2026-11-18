// `values`: how .NET writes the numbers and dates a report shows, for the port's tests (tests/numbers_test.cee). The
// culture is invariant (InvariantGlobalization), as CosmoReport's harness runs. Each line is a kind, its input and what
// .NET makes of it, separated by " | ". A double is given by its bits in hex, so both sides start from the same value.

using System.Globalization;
using System.Text;

static class Values
{
    static readonly double[] Doubles =
    {
        0.0, -0.0, 1.0, -1.0, 0.1, 0.2, 0.3, 0.5, 1.5, 2.5, 3.5, -2.5, 0.125, 0.375, 0.625, 2.675, 1.005, 1.115, 12.5,
        12.345, 99.995, 0.0049, 0.005, 0.015, 1234.5678, 1234567.891, -1234.565, 1e14, 99999999999999.9, 1e15,
        123456789012345.0, 999999999999999.0, 1234567890123456.0, 1e16, 1e17, 1e21, 1e-4, 1e-5, 0.0001234, 0.00001234,
        3.14159, 100.0, 0.30000000000000004, 1.7976931348623157e308, 5e-324, 2.2250738585072014e-308, 123.456e-10,
        4.35, 11.3, 52.0, 6.5, 9.75, 128.82, 0.15, 15.0, 26.3775, 202.23, 179.05, 0.8, 1.0 / 3.0, 2.0 / 3.0, 1e6, 1e7,
        9007199254740993.0, 0.1 + 0.7, 1234.5, 999.5, 0.45, 1.45, 8.75, 1.25, -0.5, -1.5,
    };

    static readonly string[] Decimals =
    {
        "0", "12.50", "-3.000", "0.1", "15.755", "1234567.8900", "100", "0.0001", "999999999999.99", "-0.50", "26.3775",
    };

    static readonly string[] DateTimes =
    {
        "2026-11-18T14:15:00", "2026-01-05T09:03:07", "2026-12-31T00:00:00", "2024-02-29T23:59:59",
    };

    static string Bits(double d) => "0x" + BitConverter.DoubleToInt64Bits(d).ToString("X16");

    public static string Table()
    {
        var o = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        foreach (var d in Doubles)
        {
            o.Append($"D {Bits(d)} | {d.ToString(inv)} | {d.ToString("N0", inv)} | {d.ToString("N2", inv)} | " +
                     $"{d.ToString("N4", inv)} | {d.ToString("C2", inv)} | ");
            string dec;
            try { dec = ((decimal)d).ToString(inv); } catch (OverflowException) { dec = "overflow"; }
            o.Append(dec).Append('\n');
        }
        foreach (var s in Decimals)
        {
            var m = decimal.Parse(s, inv);
            o.Append($"M {s} | {m.ToString(inv)} | {Bits((double)m)}\n");
        }
        // Sums as a Total makes them: decimals of doubles, added.
        double[][] sums = { new[] { 12.5, 3.25 }, new[] { 0.1, 0.2 }, new[] { 13.0, 1.95 }, new[] { 6.78, 2.93, 16.67 },
                            new[] { 1.0 / 3.0, 1.0 / 3.0 }, new[] { 45.2, 19.5, 111.15 } };
        foreach (var list in sums)
        {
            decimal total = 0m;
            foreach (var v in list) total += (decimal)v;
            o.Append("S ").Append(string.Join(" ", list.Select(Bits))).Append($" | {total.ToString(inv)} | {Bits((double)total)}\n");
        }
        foreach (var s in DateTimes)
        {
            var dt = DateTime.Parse(s, inv);
            o.Append($"T {s} | {dt.ToString(inv)} | {dt.ToString("yyyy/MM/dd", inv)} | {dt.ToString("dd/MM/yyyy", inv)} | " +
                     $"{dt.ToString("HH:mm", inv)} | {dt.ToString("T", inv)} | {dt.ToString("d", inv)} | {dt.ToString("g", inv)} | " +
                     $"{dt.ToString("t", inv)}\n");
        }
        return o.ToString();
    }
}
