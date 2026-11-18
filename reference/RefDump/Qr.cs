// `qr <list>`: each line of <list> encoded by CosmoPdf's QR encoder at level L (as CoreQrBridge encodes a barcode):
// "<line> => version <v> mask <m>", then its rows as 0 and 1.

using System.Text;
using CosmoPdf.QrCode;

static class Qr
{
    public static string Run(string listPath)
    {
        var o = new StringBuilder();
        foreach (var line in File.ReadAllLines(listPath))
        {
            if (line.Length == 0) continue;
            var q = QrCode.Encode(line, ErrorCorrectionLevel.L);
            o.Append($"{line} => version {q.Version} mask {q.Mask}\n");
            foreach (var row in q.Matrix)
            {
                foreach (var m in row) o.Append(m ? '1' : '0');
                o.Append('\n');
            }
        }
        return o.ToString();
    }
}
