using System.IO.Compression;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>A rendered report file. <see cref="FallbackFrom"/> is the requested language when the
/// PDF had to be rendered in English because no font covers that language's script.</summary>
public sealed record LabReportFile(byte[] Content, string ContentType, string FileName, string Lang, string? FallbackFrom);

/// <summary>
/// Renders report files: one sample report as PDF or Excel, or every report of a batch as one
/// merged PDF or a zip of PDFs (named by report code). Chooses the effective language: a PDF in a
/// language whose script no registered font covers (see <see cref="LabFonts"/>) is rendered in
/// English and reported through <see cref="LabReportFile.FallbackFrom"/>; Excel always uses the
/// requested language (the spreadsheet application supplies the fonts).
/// </summary>
public sealed class LabReportFiles
{
    public const string PdfType = "application/pdf";
    public const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string ZipType = "application/zip";

    private readonly LabReportReader _reader;
    private readonly LabTranslations _translations;
    private readonly LabFonts _fonts;
    private readonly LabReportRenderer _renderer;

    public LabReportFiles(LabReportReader reader, LabTranslations translations, LabFonts fonts, LabReportRenderer renderer)
    {
        _reader = reader;
        _translations = translations;
        _fonts = fonts;
        _renderer = renderer;
    }

    public string PdfLanguage(string lang) => _fonts.Covers(lang) ? lang : "en";

    public async Task<LabReportFile?> PdfAsync(int reportId, string lang, CancellationToken ct = default)
    {
        var model = await _reader.LoadAsync(reportId, ct);
        if (model == null) return null;
        var effective = PdfLanguage(lang);
        var t = await _translations.ForAsync(effective, ct);
        var bytes = _renderer.RenderPdf(model, t);
        return new LabReportFile(bytes, PdfType, $"{model.ReportCode}-{effective}.pdf", effective, effective == lang ? null : lang);
    }

    public async Task<LabReportFile?> XlsxAsync(int reportId, string lang, CancellationToken ct = default)
    {
        var model = await _reader.LoadAsync(reportId, ct);
        if (model == null) return null;
        var t = await _translations.ForAsync(lang, ct);
        var bytes = _renderer.RenderXlsx(model, t);
        return new LabReportFile(bytes, XlsxType, $"{model.ReportCode}-{lang}.xlsx", lang, null);
    }

    /// <summary>All reports of a batch in <paramref name="reportIds"/> order: one merged PDF or a zip of PDFs.</summary>
    public async Task<LabReportFile?> BatchAsync(IReadOnlyList<int> reportIds, string batchCode, string lang, bool zip, CancellationToken ct = default)
    {
        var models = await _reader.LoadManyAsync(reportIds, ct);
        if (models.Count == 0) return null;
        var effective = PdfLanguage(lang);
        var t = await _translations.ForAsync(effective, ct);
        var fallback = effective == lang ? null : lang;

        if (!zip)
        {
            var bytes = _renderer.RenderMergedPdf(models.Select(m => (m, t)).ToList());
            return new LabReportFile(bytes, PdfType, $"{batchCode}-reports-{effective}.pdf", effective, fallback);
        }

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var model in models)
            {
                var pdf = _renderer.RenderPdf(model, t);
                var entry = archive.CreateEntry($"{model.ReportCode}-{effective}.pdf", CompressionLevel.Fastest);
                await using var stream = entry.Open();
                await stream.WriteAsync(pdf, ct);
            }
        }
        return new LabReportFile(ms.ToArray(), ZipType, $"{batchCode}-reports-{effective}.zip", effective, fallback);
    }
}
