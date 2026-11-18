// `sample`: the sample invoice (ZatcaTest's, at a fixed date) as JSON, as System.Text.Json writes it: decimals keep their
// scale, and the computed properties (AmountWithTax, ItemDetailsCount...) are written with the rest.
// `texts <frx> <json> <name>`: the JSON read into the sample's classes and registered under <name> as the server does
// (RegisterData), the data source on its first row, and every text, table cell and barcode of the report as
// CosmoReport's evaluator makes it (EvaluateObject: ProcessText(Text, Format)), one per line: name | text. Then again
// for each line item current ("# LineItems <k>"), the list made a data source as the engine makes it
// (ResolveChildCollection), and for an item with details, its first detail current too.

using System.Reflection;
using System.Text;
using FastReport.Engine;
using System.Text.Encodings.Web;
using System.Text.Json;
using FastReport;
using FastReport.Bands;
using FastReport.Expressions;
using FastReport.Model;
using FastReport.Objects;
using FastReport.ZatcaTest;

static class Texts
{
    static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static LineItem Item(int id, string itemId, string name, decimal qty, decimal price, decimal tax, decimal discount = 0m)
        => new LineItem
        {
            TransactionDetailID = id, ItemID = itemId, ItemName = name, Quantity = qty, SalesPrice = price,
            CostPrice = Math.Round(price * 0.8m, 2), TaxPercent = 0.15m, TaxAmount = tax, Discount = discount,
            ExpiryDate = new DateTime(2027, 6, 30), ItemDetails = [],
        };

    public static string SampleJson(int count = 7)
    {
        var day = new DateTime(2026, 11, 18, 14, 15, 0);
        var items = new List<LineItem>
        {
            Item(1, "ITEM-001", "غسالة سامسونج 8 كيلو", 2, 1500.00m, 450.00m),
            Item(2, "ITEM-002", "ثلاجة LG 18 قدم", 1, 2000.00m, 300.00m),
            Item(3, "ITEM-003", "مكيف هاير 24000 وحدة", 1, 900.00m, 135.00m),
            Item(4, "ITEM-004", "خلاط مولينكس", 3, 150.00m, 67.50m, 10.00m),
            Item(5, "ITEM-005", "Water 330ml x 40", 4, 11.30m, 6.78m),
            Item(6, "ITEM-006", "Laban 2L", 12, 9.2625m, 16.67m),
            Item(7, "ITEM-007", "Glass sheet", 2, 120.50m, 36.15m),
        };
        items[6].ItemDetails =
        [
            new ItemDetail { ItemID = "ITEM-007", Description = "Front", Length = 1.20m, Width = 0.80m, Height = 0.006m, TotalSize = 0.96m, Quantity = 1 },
            new ItemDetail { ItemID = "ITEM-007", Description = "Side", Length = 0.60m, Width = 0.80m, Height = 0.006m, TotalSize = 0.48m, Quantity = 1 },
        ];
        // A longer invoice: the seven items again and again, numbered on, so it runs over pages.
        for (int k = items.Count; k < count; k++)
        {
            var from = items[k % 7];
            items.Add(Item(k + 1, $"ITEM-{k + 1:000}", from.ItemName, from.Quantity + k / 7, from.SalesPrice, from.TaxAmount, from.Discount));
        }
        var invoice = new MyInvoice
        {
            TransactionID = "1-C-TEST-001",
            TransactionDate = day,
            TransactionTime = day,
            PaymentDueDate = day.AddDays(30),
            DocumentNo = "DOC-2026-001",
            BranchID = 1, BranchName = "الفرع الرئيسي",
            VoucherTypeID = 6, VoucherTypeName = "فاتورة ضريبية",
            CustomerID = "CUST-001", CustomerName = "شركة الأمل التجارية",
            UserName = "admin",
            TotalAmount = 9182.15m, Discount = 0, NetAmount = 10559.47m,
            CashAmount = 10000.00m, CashAmount2 = 559.47m, CreditAmount = 0m,
            TaxPercent = 0.15m, TaxAmount = 1377.32m, CustomerBalance = -125.5m,
            CustomerVat = "300000000000003", SpanType = "فاتورة", PaymentMethod = "QR_PLACEHOLDER_DATA",
            PrintedBy = "admin", Notes = "شكراً لتعاملكم معنا",
            CompanyInfo = new CompanyInfo
            {
                ACompanyName = "مؤسسة الغوباشي للأجهزة المنزلية",
                ALine1 = "شارع الملك فهد، حي الشفا", ALine2 = "ص.ب 1234 الرمز البريدي 12345",
                ALine3 = "الرياض، المملكة العربية السعودية", ALine4 = "الرقم الضريبي: 300753467100003",
                ECompanyName = "Al Khubashi Home Appliances",
                ELine1 = "Al Bahah, Shafi Street", ELine2 = "P.O. Box 116  Fax: 017-7251518",
                ELine3 = "Tel: 017-7251517 Fax: 017-7251518", ELine4 = "VAT: 300753467100003",
            },
            Customer = new Customer
            {
                AccountNo = "CUST-001", AccountName = "شركة الأمل التجارية", IDNo = "1234567890",
                Address = "الرياض، شارع العليا", MobileNo = "0500000000", CreditLimit = 5000m, CreditPeriod = 30,
            },
            LineItems = items,
            Installments =
            [
                new Installment { TransactionID = "1-C-TEST-000", TransactionDate = day.AddDays(-30), SalesPriceTotal = 1000.00m, TaxAmount = 150.00m },
            ],
            PaymentDetails =
            [
                new PaymentDetail { PaymentTypeId = 2, AccountNo = "1101001", AccountName = "Cash", Amount = 10000.00m },
                new PaymentDetail { PaymentTypeId = 3, AccountNo = "1102001", AccountName = "Span", Amount = 559.47m },
            ],
        };
        return JsonSerializer.Serialize(invoice, Options) + "\n";
    }

    public static string Evaluate(string frx, string jsonPath, string name)
    {
        var invoice = JsonSerializer.Deserialize<MyInvoice>(File.ReadAllText(jsonPath))!;
        var report = new Report { Now = () => Prepared.TestNow };
        report.Load(frx);
        report.RegisterData(new List<MyInvoice> { invoice }, name, 5);
        report.GetDataSource(name)!.First();
        var evaluator = new ExpressionEvaluator(report) { PageNo = 1, TotalPages = 1, RowNo = 1 };
        var o = new StringBuilder();
        All(report, evaluator, o);
        var engine = new ReportEngine(report);
        var resolve = typeof(ReportEngine).GetMethod("ResolveChildCollection", BindingFlags.NonPublic | BindingFlags.Instance)!;
        for (int k = 0; k < invoice.LineItems.Count; k++)
        {
            var items = (FastReport.Data.DataSourceBase)resolve.Invoke(engine, [name, "LineItems", "LineItems"])!;
            for (int j = 0; j < k; j++) items.Next();
            o.Append($"# LineItems {k}\n");
            if (invoice.LineItems[k].ItemDetails.Count > 0)
                resolve.Invoke(engine, ["LineItems", "ItemDetails", "ItemDetails"]);
            All(report, evaluator, o);
        }
        return o.ToString();
    }

    // `exprs <frx> <json> <name> <list>`: each line of <list> evaluated (ProcessText) with line item 3 current; a line
    // "fmt <format> | <text>" with a CustomFormat of <format>. One per line: the line, " => ", the text.
    public static string Expressions(string frx, string jsonPath, string name, string listPath)
    {
        var invoice = JsonSerializer.Deserialize<MyInvoice>(File.ReadAllText(jsonPath))!;
        var report = new Report { Now = () => Prepared.TestNow };
        report.Load(frx);
        report.RegisterData(new List<MyInvoice> { invoice }, name, 5);
        report.GetDataSource(name)!.First();
        var evaluator = new ExpressionEvaluator(report) { PageNo = 2, TotalPages = 3, RowNo = 4 };
        var engine = new ReportEngine(report);
        var resolve = typeof(ReportEngine).GetMethod("ResolveChildCollection", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var items = (FastReport.Data.DataSourceBase)resolve.Invoke(engine, [name, "LineItems", "LineItems"])!;
        for (int j = 0; j < 3; j++) items.Next();
        var o = new StringBuilder();
        foreach (var line in File.ReadAllLines(listPath))
        {
            string text;
            if (line.StartsWith("fmt "))
            {
                var bar = line.IndexOf(" | ");
                var format = new FastReport.Format.CustomFormat { FormatString = line[4..bar] };
                text = evaluator.ProcessText(line[(bar + 3)..], format);
            }
            else
                text = evaluator.ProcessText(line);
            o.Append($"{line} => {Escape(text)}\n");
        }
        return o.ToString();
    }

    static void All(Report report, ExpressionEvaluator evaluator, StringBuilder o)
    {
        foreach (var p in report.Pages)
            if (p is ReportPage page)
                foreach (var b in page.Bands)
                    if (b is ReportComponentBase c)
                        Walk(c, evaluator, o);
    }

    static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r");

    static void Walk(ReportComponentBase c, ExpressionEvaluator e, StringBuilder o)
    {
        if (c is TextObject t && t.AllowExpressions)
            o.Append($"{t.Name} | {Escape(e.ProcessText(t.Text, t.Format))}\n");
        if (c is BarcodeObject bc && bc.AllowExpressions)
            o.Append($"{bc.Name} | {Escape(e.ProcessText(bc.Text))}\n");
        if (c is TableObject table)
            foreach (var row in table.Rows)
                foreach (var cell in row.Cells)
                    Walk(cell, e, o);
        if (c is BandBase band)
        {
            foreach (var obj in band.Objects) Walk(obj, e, o);
            if (band is DataBand d)
            {
                if (d.Header != null) Walk(d.Header, e, o);
                foreach (var detail in d.DetailBands) Walk(detail, e, o);
                if (d.Footer != null) Walk(d.Footer, e, o);
            }
            if (band is GroupHeaderBand g)
            {
                if (g.NestedGroup != null) Walk(g.NestedGroup, e, o);
                if (g.Data != null) Walk(g.Data, e, o);
                if (g.GroupFooter != null) Walk(g.GroupFooter, e, o);
            }
            if (band.Child != null) Walk(band.Child, e, o);
        }
    }
}
