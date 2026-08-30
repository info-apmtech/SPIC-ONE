using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;
using SPIC.Ifms.Automation.Options;
using SPIC.Ifms.Automation.Portal;

namespace SPIC.Ifms.Automation.Reports
{
	public interface IReportImporter
	{
		Task<ExcelBulkUploadResult> ImportAsync(
			ReportJob job,
			DownloadedReport download,
			DateTime? reportDate,
			CancellationToken cancellationToken);
	}

	/// <summary>
	/// Hands a downloaded workbook to the same ExcelBulkUploadService the manual
	/// upload page uses. Nothing about the parsing, master creation or duplicate
	/// handling is reimplemented here — an automated import and a hand upload go
	/// down exactly the same path, so they cannot drift apart.
	/// </summary>
	public sealed class ReportImporter : IReportImporter
	{
		private const string AutomationUserId = "IFMS-Automation";

		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<ReportImporter> _logger;

		public ReportImporter(IServiceScopeFactory scopeFactory, ILogger<ReportImporter> logger)
		{
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		public async Task<ExcelBulkUploadResult> ImportAsync(
			ReportJob job,
			DownloadedReport download,
			DateTime? reportDate,
			CancellationToken cancellationToken)
		{
			EnsureUsable(job, download);

			await using var scope = _scopeFactory.CreateAsyncScope();
			var uploader = scope.ServiceProvider.GetRequiredService<IExcelBulkUploadService>();

			await using var stream = new FileStream(
				download.FilePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read);

			var result = await uploader.ImportAsync(
				stream,
				AutomationUserId,
				download.Extension,
				job.CategoryId,
				download.FileName,
				reportDate,
				cancellationToken);

			if (result.Success)
			{
				_logger.LogInformation(
					"Imported {JobKey}: {Total} rows read, {Inserted} inserted, {Updated} updated, {Skipped} skipped.",
					job.Key, result.TotalRows, result.RowsInserted, result.RowsUpdated, result.RowsSkipped);
			}
			else
			{
				_logger.LogError("Import of {JobKey} was rejected: {Message}", job.Key, result.Message);
			}

			return result;
		}

		/// <summary>
		/// Catches the classic silent failure: the portal responds to an export
		/// click with an HTML "no data found" or session-expired page, which lands
		/// on disk with an .xlsx name and would otherwise reach the parser as a
		/// baffling error.
		/// </summary>
		private void EnsureUsable(ReportJob job, DownloadedReport download)
		{
			if (!File.Exists(download.FilePath))
				throw new FileNotFoundException($"The download for '{job.Key}' is missing.", download.FilePath);

			if (download.Bytes < job.MinimumBytes)
			{
				throw new InvalidDataException(
					$"The file downloaded for '{job.Key}' is only {download.Bytes:N0} bytes, " +
					$"below the {job.MinimumBytes:N0} byte minimum. The portal most likely returned " +
					$"an error page instead of the report.");
			}

			if (download.Extension is not (".xlsx" or ".csv"))
			{
				throw new InvalidDataException(
					$"'{job.Key}' downloaded as '{download.Extension}'. Only .xlsx and .csv can be " +
					$"imported; legacy .xls is not supported. If the portal only offers .xls, add a " +
					$"conversion step or change the export format on the portal.");
			}

			if (download.Extension == ".xlsx" && !LooksLikeZip(download.FilePath))
			{
				throw new InvalidDataException(
					$"'{job.Key}' has an .xlsx name but is not a real workbook — the portal probably " +
					$"returned an HTML error page. The file is kept for inspection at {download.FilePath}.");
			}
		}

		/// <summary>An .xlsx is a zip, so it must start with "PK".</summary>
		private static bool LooksLikeZip(string path)
		{
			using var stream = File.OpenRead(path);
			return stream.ReadByte() == 'P' && stream.ReadByte() == 'K';
		}
	}
}
