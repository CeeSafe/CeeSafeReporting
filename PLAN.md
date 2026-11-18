# CeeSafeReporting: the FastReport port plan

**Source:** `~/dev/CosmoDesign/CosmoPdf/FastReport/src/FastReport.Core` (CosmoReport.Core: about 16k lines of C#), and the
exporters it is used with, `~/dev/CosmoDesign/CosmoPdf/src/CosmoPdf.FastReport` (about 18k). **Target:** CeeSafe packages
in this directory. **First use:** printing IPos's invoices and receipts from the ClayWind port (`../iMurshisoft`).

## Decisions (the owner, 2026-11-18)

- **Invoice first.** Port what `PosInvoice.frx` (the thermal receipt) and then `MyInvoice.frx` (the A4 ZATCA invoice) and
  `VoucherView.frx` need, each checked against CosmoReport's output for the same data; then widen to the rest of
  FastReport.Core step by step.
- **Outputs:** a screen preview (ClayWind), PNG, ESC/POS bytes, and PDF. Bluetooth itself is not this package's: CeeSafe
  has no Bluetooth API yet.
- **Scripts:** an FRX's ScriptText (C#, compiled with Roslyn by CosmoReport) is not run. The program registers CeeSafe
  handlers by event name (`"Data1_BeforePrint"`), which the engine calls with the band as printed, as CosmoReport calls
  the script's.
- **Text and pixels come from ClayWind:** its font reader, OpenType shaping, bidi, rasterizer and image decoding. The
  report engine itself does not need a window, so it runs headless (a server, a command line) as well.

## What CosmoReport does (as read, 2026-11-18)

- **Load:** `FRReader` reads the XML into a generic tree and makes an object per element from a type registry;
  `Report.Deserialize` reads the Dictionary, the styles, the ScriptText and the pages. Attribute syntax is
  `Utils/FrxFormatParser.cs` (fonts `"Arial, 12pt, style=Bold"`, colours, border lines, alignments). Positions are
  pixels at 96 dpi; paper sizes and margins are millimetres.
- **Data:** the Dictionary's data sources are only a schema. The rows come from the program (`RegisterData`, or JSON),
  nested objects as dotted paths; a band whose DataSource is a child collection (`LineItems`) gets it from its parent's
  current row. `Row#` is the band's row number. A Total accumulates on the rows of its Evaluator band and resets after
  its PrintOn band.
- **Expressions:** no arithmetic; text is literal and `[..]` segments, each resolved in order: system variables (Page,
  TotalPages#, Row#, Date), variables, parameters, totals, aggregates, the functions FormatNumber, FormatPercent,
  FormatCurrency, FormatDateTime, IIf and Choose, then data paths. Comparisons only. A text's Format formats its value.
- **Engine:** per page, the page header, then the groups and data bands (`ShowBand`: evaluate, grow or shrink, break the
  page if it overflows, the BeforePrint handler, add at its place), subreports inline, child bands, footers; TotalPages
  is filled in after the run. The result is prepared pages: cloned bands at absolute positions with their text evaluated.
- **Export:** an exporter walks the prepared pages band by band. ImageExport draws at a resolution (203 dpi for a thermal
  printer); EscPosExport is ImageExport with its own page encoding; the PDF exporter writes through CosmoPdf.

## Steps

Each step is built, tested and committed before the next. "Parity" means: the same FRX and the same data (the
ZatcaTest sample, as JSON) through CosmoReport and through this port give the same pages, compared as prepared-page
dumps (bands, positions, texts) and as pixels.

1. **Load.** An XML reader (UTF-8, attributes, entities, CDATA, comments). The FRX object model for the elements the
   two invoices use, its attribute parsers and units. Test: both FRX files load, and a dump of the model matches
   CosmoReport's for the same file.
2. **Data, expressions and formats.** A data source over a JSON value (dotted paths, child arrays, the current row);
   the expression evaluator's resolution order and functions; Totals; the Number, Percent, Currency, Custom, Date and
   Time formats as .NET writes them. Tests against values CosmoReport gives.
   *Done.* The data is the server's object as JSON, typed by the report's Dictionary (a number it does not type is a
   decimal with a point, an integer without). Every text of PosInvoice and the server's MyInvoice, at the header and at
   each line item, and a list of functions, comparisons and custom formats, are CosmoReport's. Known differences: a
   whole object printed ("[Header.CompanyInfo]") is the C# class name there and empty here; a custom numeric format
   with sections (";") or an exponent is written as the value's ToString. Totals and aggregates accumulate as
   CosmoReport's do; they are compared with it in step 3, where the engine feeds them rows.
3. **The engine.** Pages, page header and footer, groups, data bands with header and footer, child bands, subreports,
   CanGrow and CanShrink (text measured with ClayWind), page breaks, TotalPages, and the host's event handlers. Test:
   PosInvoice.frx's prepared pages match CosmoReport's.
4. **Pixels.** Rendering prepared pages with ClayWind: text (shaped, right to left), borders, fills, tables with spans,
   lines, pictures, and QR codes (a QR encoder, written here). PNG output (a PNG encoder). Test: PosInvoice.frx at 203 dpi,
   monochrome, against CosmoReport's ImageExport.
5. **ESC/POS.** The page as raster commands, as EscPosExport writes them. Test: the same bytes as CosmoReport's.
6. **Preview and print in IPos.** A ClayWind view of the pages; the receipt's ESC/POS for the printer once there is a way
   to send it.
7. **MyInvoice.frx and VoucherView.frx.** A4: what they add over the receipt (watermark, PrintOnBottom, the subreport's
   page, more formats, Arabic company lines).
8. **PDF.** A PDF writer (pages, text with embedded font subsets, images, vector lines), enough for the invoices; the PDF/A-3
   parts ZATCA needs after that.
9. **The rest of FastReport.Core,** by what reports in use need: other objects, styles, aggregates, relations, columns,
   KeepTogether, and the other exporters.

## A reference harness

`reference/` holds a small .NET program (not part of the CeeSafe packages) that runs CosmoReport on the same FRX and data
and writes what the tests compare: the data as JSON, prepared-page dumps, PNGs and ESC/POS bytes.
