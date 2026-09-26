using System.Globalization;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// QuestPDF layouts of the sample report, copied from the product owner's reference report
/// (report (12).pdf): SOIL (green header band with the SPIC and GreenStar logos, title, farmer
/// block, Parameters / Result / Response / Optimum Range table, Recommendations, fertilizer
/// schedule "Recommendations (Kg/acre)" with Basal Application and Top Dressing tables, the
/// "Healthy Soil. Wealthy Farmer." line and the officer's signature caption) and WATER (navy
/// header, farmer name and address with Date and Lab No, S.No / Parameters / Result / Remarks
/// table, Recommendations, Note, authorised signatory). A4 portrait, body text 9-11 pt.
/// The Excel version (ClosedXML) holds the same sections on one sheet.
///
/// Header lines come from Sas:Lab:ReportHeader (English) or the header.* translations; the logos
/// from Assets/Lab (spic-logo.png, greenstar-logo.png) next to the API.
/// </summary>
public sealed class LabReportRenderer
{
    private const string Green = "#1B5E20";
    private const string GreenText = "#2E7D32";
    private const string GreenBand = "#E4EFDD";
    private const string Navy = "#1F3A68";
    private const string Ink = "#111111";
    private const string Muted = "#6B7280";
    private const float Line = 0.75f;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly LabFonts _fonts;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment? _env;
    private readonly ILogger<LabReportRenderer> _logger;
    private byte[]? _spicLogo;
    private byte[]? _greenStarLogo;
    private bool _logosLoaded;

    static LabReportRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public LabReportRenderer(LabFonts fonts, IConfiguration config, ILogger<LabReportRenderer> logger, IHostEnvironment? env = null)
    {
        _fonts = fonts;
        _config = config;
        _logger = logger;
        _env = env;
    }

    // ================================================================== public API

    public byte[] RenderPdf(LabReportModel model, LabTranslator t) => BuildDocument(model, t).GeneratePdf();

    public byte[] RenderMergedPdf(IReadOnlyList<(LabReportModel Model, LabTranslator T)> reports)
    {
        if (reports.Count == 1) return RenderPdf(reports[0].Model, reports[0].T);
        var docs = reports.Select(r => (IDocument)BuildDocument(r.Model, r.T)).ToList();
        return Document.Merge(docs).UseContinuousPageNumbers().GeneratePdf();
    }

    public IDocument BuildDocument(LabReportModel model, LabTranslator t)
    {
        LoadLogos();
        var families = Families(t.Lang);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily(families).FontColor(Ink).LineHeight(1.15f));

                if (model.Layout == SampleType.Water)
                {
                    page.Header().Element(c => WaterHeader(c, t));
                    page.Content().PaddingHorizontal(34).Element(c => WaterBody(c, model, t));
                    page.Footer().Element(c => WaterFooter(c, model, t));
                }
                else
                {
                    page.Header().Element(c => SoilHeader(c, t));
                    page.Content().PaddingHorizontal(30).Element(c => SoilBody(c, model, t));
                    page.Footer().Element(c => SoilFooter(c, model, t));
                }
            });
        }).WithMetadata(new DocumentMetadata
        {
            Title = $"{model.ReportCode} {(model.Layout == SampleType.Water ? "Irrigation Water" : "Soil Sample")} Analytical Report",
            Author = HeaderValue("Title", "SPIC AGRICULTURE SERVICES"),
            Subject = model.LabNumber,
            Language = t.Lang
        });
    }

    private string[] Families(string lang)
    {
        var latin = _fonts.LatinFamily;
        var script = _fonts.FamilyFor(lang);
        return script == null || script == latin ? new[] { latin } : new[] { latin, script };
    }

    // ================================================================== header values

    private string HeaderValue(string name, string fallback)
    {
        var v = _config[$"Sas:Lab:ReportHeader:{name}"];
        return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
    }

    /// <summary>English from configuration; another language from its header.* row, else English.</summary>
    private string Head(LabTranslator t, string key, string configName, string fallback) =>
        t.TryOwn(key) ?? HeaderValue(configName, fallback);

    private (string Role, string Org) Signatory(LabTranslator t, bool water)
    {
        var configured = HeaderValue(water ? "WaterSignatory" : "SoilSignatory",
            water ? "AUTHORIZED SIGNATORY, SPIC SOIL TESTING LAB" : "OFFICER, SPIC AGRICULTURE SERVICES");
        var comma = configured.IndexOf(',');
        var role = comma > 0 ? configured[..comma].Trim() : configured;
        var org = comma > 0 ? configured[(comma + 1)..].Trim() : "";
        var prefix = water ? "signature.water" : "signature.soil";
        return (t.TryOwn($"{prefix}.role") ?? role, t.TryOwn($"{prefix}.org") ?? org);
    }

    // ================================================================== SOIL

    private void SoilHeader(IContainer c, LabTranslator t)
    {
        c.Background(GreenBand).PaddingVertical(10).PaddingHorizontal(26).Row(row =>
        {
            row.ConstantItem(64).AlignMiddle().Element(x => Logo(x, _spicLogo, 60));
            row.RelativeItem().PaddingHorizontal(6).AlignMiddle().Column(col =>
            {
                col.Item().AlignCenter().Text(Head(t, "header.title", "Title", "SPIC AGRICULTURE SERVICES"))
                    .FontSize(19).Bold().FontColor(Green);
                col.Item().PaddingTop(2).AlignCenter().Text(Head(t, "header.address", "AddressLine", ""))
                    .FontSize(11).Bold().FontColor(GreenText);
                col.Item().AlignCenter().Text(Head(t, "header.phone", "PhoneLine", ""))
                    .FontSize(11).Bold().FontColor(GreenText);
            });
            row.ConstantItem(96).AlignMiddle().Element(x => Logo(x, _greenStarLogo, 92));
        });
    }

    private void SoilBody(IContainer c, LabReportModel m, LabTranslator t)
    {
        // One page like the reference: long recommendation lists or taller scripts scale the body down.
        c.ScaleToFit().Column(col =>
        {
            col.Item().PaddingTop(10).AlignCenter()
                .Text(t.T("report.title.soil", "SOIL SAMPLE ANALYTICAL REPORT")).FontSize(13).Bold();

            col.Item().PaddingTop(5).Element(x => SoilFarmerBlock(x, m, t));
            col.Item().PaddingTop(9).Element(x => SoilParameterTable(x, m, t));

            // Recommendations (grouped lines) and the crop note.
            col.Item().PaddingTop(5).Text(t.T("label.recommendations", "Recommendations") + ":").FontSize(10).Bold();
            if (m.Recommendations.Count == 0)
            {
                col.Item().PaddingLeft(8).Text("- " + t.T("text.nil", "Nil"));
            }
            foreach (var group in m.Recommendations)
            {
                col.Item().PaddingTop(2).PaddingLeft(8).Text(t.Group(group.Group)).FontSize(9).Bold().FontColor(GreenText);
                foreach (var line in group.Lines)
                    col.Item().PaddingLeft(8).Text("- " + t.Hint(line));
            }
            col.Item().PaddingTop(2).PaddingLeft(8).Text(text =>
            {
                text.Span(t.T("label.cropNote", "Crop Suitability Note") + ": ").Bold();
                text.Span(t.CropNote(m));
            });

            // Fertilizer schedule.
            if (m.Schedule.Count > 0 && m.Schedule.Any(s => s.Rows.Count > 0))
            {
                col.Item().PaddingTop(10).AlignCenter()
                    .Text(t.T("label.fertilizerSchedule", "Recommendations (Kg/acre)")).FontSize(11).Bold();
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Element(x => BasalTable(x, m, t));
                    row.ConstantItem(6);
                    row.RelativeItem().Element(x => TopDressingTable(x, m, t));
                });
                var generalNote = GeneralScheduleNote(m, t);
                if (generalNote != null)
                    col.Item().PaddingTop(3).Text(generalNote).FontSize(8).Italic().FontColor(Muted);
            }

            col.Item().PaddingTop(14).AlignCenter()
                .Text(t.T("footer.slogan", "!! Healthy Soil. Wealthy Farmer. !!")).FontSize(14).Bold().FontColor(GreenText);
        });
    }

    private static void SoilFarmerBlock(IContainer c, LabReportModel m, LabTranslator t)
    {
        var address = m.AddressLines.Count == 0 ? "" : string.Join("\n", m.AddressLines);
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.25f);
                cols.RelativeColumn(2.6f);
                cols.RelativeColumn(1.3f);
                cols.RelativeColumn(1.45f);
            });

            table.Cell().Row(1).Column(1).Element(Box).Text(t.T("label.farmerName", "Farmer Name")).Bold();
            table.Cell().Row(1).Column(2).Element(Box).Text(m.FarmerName);
            table.Cell().Row(1).Column(3).Element(Box).Text(t.T("label.surveyNumber", "Survey Number")).Bold();
            table.Cell().Row(1).Column(4).Element(Box).Text(m.SurveyNumber ?? "");

            table.Cell().Row(2).Column(1).RowSpan(2).Element(Box).AlignMiddle().Text(t.T("label.address", "Address")).Bold();
            table.Cell().Row(2).Column(2).RowSpan(2).Element(Box).AlignMiddle().Text(address);
            table.Cell().Row(2).Column(3).Element(Box).Text(t.T("label.sampleNumber", "Sample Number")).Bold();
            table.Cell().Row(2).Column(4).Element(Box).Text(m.SampleNumber);
            table.Cell().Row(3).Column(3).Element(Box).Text(t.T("label.labNumber", "Lab Number")).Bold();
            table.Cell().Row(3).Column(4).Element(Box).Text(m.LabNumber);

            table.Cell().Row(4).Column(1).Element(Box).Text(t.T("label.mobile", "Mobile Number")).Bold();
            table.Cell().Row(4).Column(2).Element(Box).Text(m.Mobile ?? "");
            table.Cell().Row(4).Column(3).Element(Box).Text(t.T("label.batchNumber", "Batch Number")).Bold();
            table.Cell().Row(4).Column(4).Element(Box).Text(m.BatchCode);
        });
    }

    private static void SoilParameterTable(IContainer c, LabReportModel m, LabTranslator t)
    {
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2.3f);
                cols.RelativeColumn(1.6f);
                cols.RelativeColumn(1.6f);
                cols.RelativeColumn(2.2f);
            });

            table.Header(h =>
            {
                h.Cell().Element(Box).AlignCenter().Text(t.T("table.parameters", "Parameters")).Bold().FontSize(10);
                h.Cell().Element(Box).AlignCenter().Text(t.T("table.result", "Result")).Bold().FontSize(10);
                h.Cell().Element(Box).AlignCenter().Text(t.T("table.response", "Response")).Bold().FontSize(10);
                h.Cell().Element(Box).AlignCenter().Text(t.T("table.optimumRange", "Optimum Range")).Bold().FontSize(10);
            });

            foreach (var row in m.Rows)
            {
                table.Cell().Element(Box).Text(t.Param(row.Code, row.Name)).Bold();
                if (row.IsText)
                {
                    table.Cell().ColumnSpan(3).Element(Box).AlignCenter().Text(t.Texture(row.Value));
                    continue;
                }
                table.Cell().Element(Box).AlignCenter().Text(LabReportRules.FormatValue(row.Value));
                table.Cell().Element(Box).AlignCenter().Text(row.HasValue ? t.Response(row.ResultLabel) : "");
                table.Cell().Element(Box).AlignCenter().Text(LabReportRules.RangeWithUnit(row.NormalRange, row.Unit));
            }
        });
    }

    private static void BasalTable(IContainer c, LabReportModel m, LabTranslator t)
    {
        var columns = m.Schedule;
        var products = ProductsFor(columns, LabCropStage.Basal);
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2.1f);
                foreach (var _ in columns) cols.RelativeColumn(1.25f);
            });
            table.Header(h =>
            {
                h.Cell().Element(Box).AlignCenter().Text(t.T("schedule.basal", "Basal Application")).Bold();
                foreach (var col in columns) h.Cell().Element(Box).AlignCenter().Text(CropHeader(col, t)).Bold();
            });
            foreach (var product in products)
            {
                table.Cell().Element(Box).Text(t.Product(product)).Bold();
                foreach (var col in columns)
                    table.Cell().Element(Box).AlignRight().Text(Kg(col, LabCropStage.Basal, product));
            }
        });
    }

    private static void TopDressingTable(IContainer c, LabReportModel m, LabTranslator t)
    {
        var columns = m.Schedule;
        var stages = new[]
        {
            (Stage: LabCropStage.TopDressing1, Key: "schedule.app1", En: "1st Application"),
            (Stage: LabCropStage.TopDressing2, Key: "schedule.app2", En: "2nd Application"),
            (Stage: LabCropStage.TopDressing3, Key: "schedule.app3", En: "3rd Application")
        };
        var thDay = t.T("schedule.thDay", "th day");

        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2.1f);
                foreach (var _ in columns) cols.RelativeColumn(1.25f);
            });
            table.Header(h =>
            {
                h.Cell().Element(Box).AlignCenter().Text(t.T("schedule.topDressing", "Top Dressing")).Bold();
                foreach (var col in columns) h.Cell().Element(Box).AlignCenter().Text(CropHeader(col, t)).Bold();
            });
            foreach (var stage in stages)
            {
                var products = ProductsFor(columns, stage.Stage);
                if (products.Count == 0) continue;

                table.Cell().Element(Box).AlignCenter().Text(t.T(stage.Key, stage.En)).Bold();
                foreach (var col in columns)
                {
                    var day = col.Rows.Where(r => r.Stage == stage.Stage && r.DayNumber.HasValue).Select(r => r.DayNumber).FirstOrDefault();
                    table.Cell().Element(Box).AlignCenter().Text(day.HasValue ? $"{day} {thDay}" : "-").FontSize(8.5f);
                }
                foreach (var product in products)
                {
                    table.Cell().Element(Box).Text(t.Product(product)).Bold();
                    foreach (var col in columns)
                        table.Cell().Element(Box).AlignRight().Text(Kg(col, stage.Stage, product));
                }
            }
        });
    }

    private void SoilFooter(IContainer c, LabReportModel m, LabTranslator t)
    {
        var (role, org) = Signatory(t, water: false);
        c.Background(GreenBand).PaddingHorizontal(26).PaddingTop(6).PaddingBottom(12).Row(row =>
        {
            row.RelativeItem().AlignBottom().Text($"{t.T("text.reportNo", "Report No")}: {m.ReportCode}   ·   {t.T("text.generatedOn", "Generated on")}: {m.GeneratedAt:dd-MM-yyyy}")
                .FontSize(7).FontColor(Muted);
            row.ConstantItem(250).Column(col =>
            {
                col.Item().Height(30);
                col.Item().AlignCenter().Text(role).FontSize(12).Bold().FontColor(Green);
                if (!string.IsNullOrWhiteSpace(org)) col.Item().AlignCenter().Text(org).FontSize(12).Bold().FontColor(Green);
            });
        });
    }

    // ================================================================== WATER

    private void WaterHeader(IContainer c, LabTranslator t)
    {
        c.PaddingTop(16).PaddingHorizontal(34).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(58).AlignMiddle().Element(x => Logo(x, _spicLogo, 54));
                row.RelativeItem().AlignMiddle().AlignCenter()
                    .Text(Head(t, "header.title", "Title", "SPIC AGRICULTURE SERVICES")).FontSize(21).Bold().FontColor(Navy);
                row.ConstantItem(96).AlignMiddle().Element(x => Logo(x, _greenStarLogo, 92));
            });
            col.Item().PaddingTop(4).AlignCenter().Text(Head(t, "header.address", "AddressLine", "")).FontSize(10).FontColor(Navy);
            col.Item().AlignCenter().Text(Head(t, "header.phone", "PhoneLine", "") + "   " + Head(t, "header.customerCare", "CustomerCare", ""))
                .FontSize(10).FontColor(Navy);
        });
    }

    private static void WaterBody(IContainer c, LabReportModel m, LabTranslator t)
    {
        c.ScaleToFit().Column(col =>
        {
            col.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(t.T("label.farmerNameAddress", "Farmer's Name & Address")).FontSize(11).Bold().FontColor(Navy);
                    left.Item().PaddingTop(3).PaddingLeft(4).Text(m.FarmerName);
                    foreach (var line in m.AddressLines) left.Item().PaddingLeft(4).Text(line);
                    if (!string.IsNullOrWhiteSpace(m.Mobile)) left.Item().PaddingLeft(4).Text(m.Mobile);
                });
                row.ConstantItem(180).PaddingLeft(20).Column(right =>
                {
                    right.Item().Text(text =>
                    {
                        text.Span(t.T("label.date", "Date") + ": ").FontSize(11).Bold().FontColor(Navy);
                        text.Span(m.GeneratedAt.ToString("dd-MM-yy", Inv));
                    });
                    right.Item().PaddingTop(12).Text(text =>
                    {
                        text.Span(t.T("label.labNo", "Lab No") + ": ").FontSize(11).Bold().FontColor(Navy);
                        text.Span(m.LabNumber);
                    });
                });
            });

            col.Item().PaddingTop(14).AlignCenter()
                .Text(t.T("report.title.water", "IRRIGATION WATER ANALYTICAL REPORT")).FontSize(14).Bold().FontColor(Navy);

            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(0.8f);
                    cols.RelativeColumn(3.1f);
                    cols.RelativeColumn(1.7f);
                    cols.RelativeColumn(2.6f);
                });
                table.Header(h =>
                {
                    h.Cell().Element(WaterBox).AlignCenter().Text(t.T("table.sno", "S. No")).FontSize(11).Bold().FontColor(Navy);
                    h.Cell().Element(WaterBox).AlignCenter().Text(t.T("table.parameters", "Parameters")).FontSize(11).Bold().FontColor(Navy);
                    h.Cell().Element(WaterBox).AlignCenter().Text(t.T("table.result.water", "Result")).FontSize(11).Bold().FontColor(Navy);
                    h.Cell().Element(WaterBox).AlignCenter().Text(t.T("table.remarks", "Remarks")).FontSize(11).Bold().FontColor(Navy);
                });

                var n = 0;
                foreach (var row in m.Rows)
                {
                    n++;
                    var unit = LabReportRules.CleanUnit(row.Unit);
                    var name = t.Param(row.Code, row.Name) + (unit == null ? "" : $"  ({unit})");
                    table.Cell().Element(WaterBox).AlignCenter().Text(n.ToString(Inv)).FontSize(10).Bold().FontColor(Navy);
                    table.Cell().Element(WaterBox).Text(name).FontSize(9).Bold().FontColor(Navy);
                    table.Cell().Element(WaterBox).AlignRight().Text(LabReportRules.FormatValue(row.Value));
                    var remark = row.HasValue ? t.Response(row.ResultLabel) : "";
                    table.Cell().Element(WaterBox).AlignCenter().Text(string.IsNullOrWhiteSpace(remark) ? "-" : remark);
                }
            });

            col.Item().PaddingTop(10).Text(t.T("label.recommendations", "Recommendations")).FontSize(12).Bold().FontColor(Navy);
            if (m.Recommendations.Count == 0)
            {
                col.Item().PaddingLeft(6).Text(t.T("text.nil", "Nil"));
            }
            foreach (var group in m.Recommendations)
                foreach (var line in group.Lines)
                    col.Item().PaddingLeft(6).Text("- " + t.Hint(line));

            col.Item().PaddingTop(10).Text(t.T("label.note", "Note")).FontSize(12).Bold().FontColor(Navy);
            col.Item().PaddingLeft(6).Text(t.CropNote(m));
        });
    }

    private void WaterFooter(IContainer c, LabReportModel m, LabTranslator t)
    {
        var (role, org) = Signatory(t, water: true);
        c.Column(col =>
        {
            col.Item().PaddingHorizontal(34).AlignRight().Width(250).Column(sig =>
            {
                sig.Item().Height(30);
                sig.Item().AlignCenter().Text(role).FontSize(11).Bold().FontColor(Navy);
                if (!string.IsNullOrWhiteSpace(org)) sig.Item().AlignCenter().Text(org).FontSize(11).Bold().FontColor(Navy);
            });
            col.Item().PaddingTop(10).Background(Navy).PaddingHorizontal(34).PaddingVertical(9)
                .Text($"{t.T("text.reportNo", "Report No")}: {m.ReportCode}   ·   {t.T("text.generatedOn", "Generated on")}: {m.GeneratedAt:dd-MM-yyyy}")
                .FontSize(7).FontColor(Colors.White);
        });
    }

    // ================================================================== shared pieces

    private static IContainer Box(IContainer c) =>
        c.Border(Line).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingVertical(1.5f);

    private static IContainer WaterBox(IContainer c) =>
        c.Border(1).BorderColor(Colors.Black).MinHeight(22).PaddingHorizontal(5).PaddingVertical(4).AlignMiddle();

    private static void Logo(IContainer c, byte[]? image, float width)
    {
        if (image == null) return;
        c.Width(width).Image(image).FitWidth();
    }

    /// <summary>"No fertilizer schedule is configured for Paddy, Groundnut; ..." or null when every column has its own rows.</summary>
    private static string? GeneralScheduleNote(LabReportModel m, LabTranslator t)
    {
        var crops = m.Schedule.Where(s => s.IsGeneral)
            .Select(s => string.IsNullOrWhiteSpace(s.Crop) ? t.T("text.proposedCrop", "the proposed crop") : t.Crop(s.Crop))
            .Distinct()
            .ToList();
        if (crops.Count == 0) return null;
        return t.T("schedule.generalNote", "No fertilizer schedule is configured for {crop}; the general schedule is shown.")
            .Replace("{crop}", string.Join(", ", crops));
    }

    private static string CropHeader(LabScheduleColumn col, LabTranslator t) =>
        col.IsGeneral ? t.T("schedule.general", "General") : t.Crop(col.Crop);

    private static List<string> ProductsFor(List<LabScheduleColumn> columns, LabCropStage stage) =>
        columns.SelectMany(c => c.Rows.Where(r => r.Stage == stage))
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .Select(r => r.Product)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string Kg(LabScheduleColumn col, LabCropStage stage, string product)
    {
        var row = col.Rows.FirstOrDefault(r => r.Stage == stage && string.Equals(r.Product, product, StringComparison.OrdinalIgnoreCase));
        return row == null ? "-" : row.KgPerAcre.ToString("0.00", Inv);
    }

    private void LoadLogos()
    {
        if (_logosLoaded) return;
        _spicLogo = ReadAsset("spic-logo.png");
        _greenStarLogo = ReadAsset("greenstar-logo.png");
        _logosLoaded = true;
    }

    private byte[]? ReadAsset(string name)
    {
        var candidates = new List<string>();
        if (_env != null) candidates.Add(Path.Combine(_env.ContentRootPath, "Assets", "Lab", name));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "Lab", name));
        var path = candidates.FirstOrDefault(File.Exists);
        if (path == null)
        {
            _logger.LogWarning("LabReportRenderer: logo {Name} not found under Assets/Lab; the header prints without it.", name);
            return null;
        }
        return File.ReadAllBytes(path);
    }

    // ================================================================== Excel

    public byte[] RenderXlsx(LabReportModel m, LabTranslator t)
    {
        using var wb = new XLWorkbook();
        var water = m.Layout == SampleType.Water;
        var ws = wb.Worksheets.Add(water ? "Water Report" : "Soil Report");
        ws.Style.Font.FontName = "Arial";
        ws.Style.Font.FontSize = 10;
        var r = 1;

        void Merged(string text, double size, bool bold, string? color = null, XLAlignmentHorizontalValues align = XLAlignmentHorizontalValues.Center)
        {
            var range = ws.Range(r, 1, r, 4).Merge();
            range.Value = text;
            range.Style.Font.FontSize = size;
            range.Style.Font.Bold = bold;
            range.Style.Alignment.Horizontal = align;
            range.Style.Alignment.WrapText = true;
            if (color != null) range.Style.Font.FontColor = XLColor.FromHtml(color);
            r++;
        }

        void Row4(string? a, string? b, string? c, string? d, bool header = false, bool boldFirst = false)
        {
            ws.Cell(r, 1).Value = a ?? "";
            ws.Cell(r, 2).Value = b ?? "";
            ws.Cell(r, 3).Value = c ?? "";
            ws.Cell(r, 4).Value = d ?? "";
            var range = ws.Range(r, 1, r, 4);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Alignment.WrapText = true;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (header)
            {
                range.Style.Font.Bold = true;
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                range.Style.Fill.BackgroundColor = XLColor.FromHtml(water ? "#E8EDF5" : GreenBand);
            }
            else if (boldFirst)
            {
                ws.Cell(r, 1).Style.Font.Bold = true;
            }
            r++;
        }

        var accent = water ? Navy : Green;
        Merged(Head(t, "header.title", "Title", "SPIC AGRICULTURE SERVICES"), 16, true, accent);
        Merged(Head(t, "header.address", "AddressLine", ""), 10, false, accent);
        Merged(Head(t, "header.phone", "PhoneLine", "") + (water ? "   " + Head(t, "header.customerCare", "CustomerCare", "") : ""), 10, false, accent);
        r++;
        Merged(water ? t.T("report.title.water", "IRRIGATION WATER ANALYTICAL REPORT") : t.T("report.title.soil", "SOIL SAMPLE ANALYTICAL REPORT"), 13, true, water ? Navy : null);
        r++;

        var address = string.Join(", ", m.AddressLines);
        if (water)
        {
            Row4(t.T("label.farmerNameAddress", "Farmer's Name & Address"), $"{m.FarmerName}\n{string.Join("\n", m.AddressLines)}\n{m.Mobile}".Trim(),
                t.T("label.date", "Date"), m.GeneratedAt.ToString("dd-MM-yy", Inv), boldFirst: true);
            ws.Cell(r - 1, 3).Style.Font.Bold = true;
            Row4("", "", t.T("label.labNo", "Lab No"), m.LabNumber);
            ws.Cell(r - 1, 3).Style.Font.Bold = true;
        }
        else
        {
            Row4(t.T("label.farmerName", "Farmer Name"), m.FarmerName, t.T("label.surveyNumber", "Survey Number"), m.SurveyNumber, boldFirst: true);
            ws.Cell(r - 1, 3).Style.Font.Bold = true;
            Row4(t.T("label.address", "Address"), address, t.T("label.sampleNumber", "Sample Number"), m.SampleNumber, boldFirst: true);
            ws.Cell(r - 1, 3).Style.Font.Bold = true;
            Row4("", "", t.T("label.labNumber", "Lab Number"), m.LabNumber);
            ws.Cell(r - 1, 3).Style.Font.Bold = true;
            Row4(t.T("label.mobile", "Mobile Number"), m.Mobile, t.T("label.batchNumber", "Batch Number"), m.BatchCode, boldFirst: true);
            ws.Cell(r - 1, 3).Style.Font.Bold = true;
        }
        r++;

        if (water)
        {
            Row4(t.T("table.sno", "S. No"), t.T("table.parameters", "Parameters"), t.T("table.result.water", "Result"), t.T("table.remarks", "Remarks"), header: true);
            var n = 0;
            foreach (var row in m.Rows)
            {
                n++;
                var unit = LabReportRules.CleanUnit(row.Unit);
                var remark = row.HasValue ? t.Response(row.ResultLabel) : "";
                Row4(n.ToString(Inv), t.Param(row.Code, row.Name) + (unit == null ? "" : $" ({unit})"),
                    LabReportRules.FormatValue(row.Value), string.IsNullOrWhiteSpace(remark) ? "-" : remark);
            }
        }
        else
        {
            Row4(t.T("table.parameters", "Parameters"), t.T("table.result", "Result"), t.T("table.response", "Response"), t.T("table.optimumRange", "Optimum Range"), header: true);
            foreach (var row in m.Rows)
            {
                if (row.IsText)
                    Row4(t.Param(row.Code, row.Name), t.Texture(row.Value), "", "", boldFirst: true);
                else
                    Row4(t.Param(row.Code, row.Name), LabReportRules.FormatValue(row.Value),
                        row.HasValue ? t.Response(row.ResultLabel) : "", LabReportRules.RangeWithUnit(row.NormalRange, row.Unit), boldFirst: true);
            }
        }
        r++;

        Merged(t.T("label.recommendations", "Recommendations"), 11, true, accent, XLAlignmentHorizontalValues.Left);
        if (m.Recommendations.Count == 0) Merged(t.T("text.nil", "Nil"), 10, false, null, XLAlignmentHorizontalValues.Left);
        foreach (var group in m.Recommendations)
        {
            if (!water) Merged(t.Group(group.Group), 10, true, GreenText, XLAlignmentHorizontalValues.Left);
            foreach (var line in group.Lines) Merged("- " + t.Hint(line), 10, false, null, XLAlignmentHorizontalValues.Left);
        }
        r++;
        Merged(water ? t.T("label.note", "Note") : t.T("label.cropNote", "Crop Suitability Note"), 11, true, accent, XLAlignmentHorizontalValues.Left);
        Merged(t.CropNote(m), 10, false, null, XLAlignmentHorizontalValues.Left);

        if (!water && m.Schedule.Any(s => s.Rows.Count > 0))
        {
            r++;
            Merged(t.T("label.fertilizerSchedule", "Recommendations (Kg/acre)"), 11, true);
            var cols = m.Schedule;
            string Header(LabScheduleColumn col) => CropHeader(col, t);

            Row4(t.T("schedule.basal", "Basal Application"), cols.ElementAtOrDefault(0) is { } c0 ? Header(c0) : "", cols.ElementAtOrDefault(1) is { } c1 ? Header(c1) : "", "", header: true);
            foreach (var product in ProductsFor(cols, LabCropStage.Basal))
                Row4(t.Product(product), Kg(cols[0], LabCropStage.Basal, product), cols.Count > 1 ? Kg(cols[1], LabCropStage.Basal, product) : "", "", boldFirst: true);
            r++;

            var thDay = t.T("schedule.thDay", "th day");
            Row4(t.T("schedule.topDressing", "Top Dressing"), Header(cols[0]), cols.Count > 1 ? Header(cols[1]) : "", "", header: true);
            foreach (var (stage, key, en) in new[]
                     {
                         (LabCropStage.TopDressing1, "schedule.app1", "1st Application"),
                         (LabCropStage.TopDressing2, "schedule.app2", "2nd Application"),
                         (LabCropStage.TopDressing3, "schedule.app3", "3rd Application")
                     })
            {
                var products = ProductsFor(cols, stage);
                if (products.Count == 0) continue;
                string Day(LabScheduleColumn col) =>
                    col.Rows.Where(x => x.Stage == stage && x.DayNumber.HasValue).Select(x => x.DayNumber).FirstOrDefault() is int d ? $"{d} {thDay}" : "-";
                Row4(t.T(key, en), Day(cols[0]), cols.Count > 1 ? Day(cols[1]) : "", "", boldFirst: true);
                foreach (var product in products)
                    Row4(t.Product(product), Kg(cols[0], stage, product), cols.Count > 1 ? Kg(cols[1], stage, product) : "", "", boldFirst: true);
            }
            var generalNote = GeneralScheduleNote(m, t);
            if (generalNote != null) Merged(generalNote, 9, false, Muted, XLAlignmentHorizontalValues.Left);
        }

        r++;
        if (!water) Merged(t.T("footer.slogan", "!! Healthy Soil. Wealthy Farmer. !!"), 12, true, GreenText);
        r++;
        var (role, org) = Signatory(t, water);
        Merged(role, 11, true, accent, XLAlignmentHorizontalValues.Right);
        if (!string.IsNullOrWhiteSpace(org)) Merged(org, 11, true, accent, XLAlignmentHorizontalValues.Right);
        r++;
        Merged($"{t.T("text.reportNo", "Report No")}: {m.ReportCode}   {t.T("text.generatedOn", "Generated on")}: {m.GeneratedAt:dd-MM-yyyy}", 8, false, Muted, XLAlignmentHorizontalValues.Left);

        ws.Column(1).Width = water ? 30 : 26;
        ws.Column(2).Width = water ? 38 : 30;
        ws.Column(3).Width = 22;
        ws.Column(4).Width = 26;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.FitToPages(1, 0);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
