using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Core.DTOs;
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
	/// Uploads a downloaded report to SpicAPI, exactly as a person would from the
	/// Excel Upload page.
	///
	/// This used to call ExcelBulkUploadService in-process. Going over HTTP instead
	/// buys three things: the data lands in the portal's own database rather than
	/// the automation's, there is still only one import implementation so an
	/// automated import and a hand upload cannot drift apart, and this service
	/// stops needing any knowledge of the SPIC schema.
	/// </summary>
	public sealed class ReportImporter : IReportImporter
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly UploadOptions _options;
		private readonly ILogger<ReportImporter> _logger;

		private static readonly JsonSerializerOptions Json = new()
		{
			PropertyNameCaseInsensitive = true
		};

		public ReportImporter(
			IHttpClientFactory httpClientFactory,
			IOptions<UploadOptions> options,
			ILogger<ReportImporter> logger)
		{
			_httpClientFactory = httpClientFactory;
			_options = options.Value;
			_logger = logger;
		}

		public async Task<ExcelBulkUploadResult> ImportAsync(
			ReportJob job,
			DownloadedReport download,
			DateTime? reportDate,
			CancellationToken cancellationToken)
		{
			EnsureUsable(job, download);

			if (string.IsNullOrWhiteSpace(_options.ApiBaseUrl))
			{
				throw new InvalidOperationException(
					"Upload:ApiBaseUrl is not set, so there is nowhere to send the report.");
			}

			var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/{_options.Path.TrimStart('/')}";
			var attempts = Math.Max(1, _options.MaxAttempts);

			for (var attempt = 1; attempt <= attempts; attempt++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				try
				{
					return await PostAsync(url, job, download, reportDate, cancellationToken);
				}
				catch (HttpRequestException ex) when (attempt < attempts)
				{
					// Only the network is retried. A rejected file would be rejected
					// again, and re-posting a file that imported would duplicate rows.
					_logger.LogWarning(
						"Upload of {JobKey} failed to reach {Url} (attempt {Attempt}/{Max}): {Message}",
						job.Key, url, attempt, attempts, ex.Message);

					await Task.Delay(TimeSpan.FromSeconds(10 * attempt), cancellationToken);
				}
				catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < attempts)
				{
					_logger.LogWarning(
						"Upload of {JobKey} timed out (attempt {Attempt}/{Max}).",
						job.Key, attempt, attempts);
				}
			}

			throw new InvalidOperationException(
				$"Could not upload '{job.Key}' to {url} after {attempts} attempts.");
		}

		private async Task<ExcelBulkUploadResult> PostAsync(
			string url,
			ReportJob job,
			DownloadedReport download,
			DateTime? reportDate,
			CancellationToken cancellationToken)
		{
			var client = _httpClientFactory.CreateClient("spic-upload");
			client.Timeout = TimeSpan.FromSeconds(Math.Max(60, _options.TimeoutSeconds));

			using var content = new MultipartFormDataContent();

			await using var stream = new FileStream(
				download.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

			using var file = new StreamContent(stream);
			file.Headers.ContentType = new MediaTypeHeaderValue(
				download.Extension == ".csv" ? "text/csv" : "application/vnd.ms-excel");

			// The field names match what the Excel Upload page posts, because this
			// hits the very same endpoint.
			content.Add(file, "file", Path.GetFileName(download.FilePath));
			content.Add(new StringContent(job.CategoryId), "categoryId");

			if (reportDate.HasValue)
			{
				content.Add(
					new StringContent(reportDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
					"reportDate");
			}

			using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

			if (!string.IsNullOrWhiteSpace(_options.ApiKey))
				request.Headers.Add("X-Automation-Key", _options.ApiKey);

			_logger.LogInformation(
				"Uploading {File} ({Bytes:N0} bytes) as category {Category} to {Url}.",
				Path.GetFileName(download.FilePath), download.Bytes, job.CategoryId, url);

			using var response = await client.SendAsync(request, cancellationToken);
			var body = await response.Content.ReadAsStringAsync(cancellationToken);

			if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
									or System.Net.HttpStatusCode.Forbidden)
			{
				throw new InvalidOperationException(
					"SpicAPI rejected the upload key. Check that Upload:ApiKey here matches " +
					"IfmsAutomation:AutomationKey on the API.");
			}

			var result = TryParse(body);

			if (result is null)
			{
				throw new InvalidOperationException(
					$"SpicAPI returned {(int)response.StatusCode} and a body this could not read: " +
					Trim(body, 400));
			}

			if (result.Success)
			{
				_logger.LogInformation(
					"Imported {JobKey}: {Total} rows read, {Inserted} inserted, {Updated} updated, {Skipped} skipped.",
					job.Key, result.TotalRows, result.RowsInserted, result.RowsUpdated, result.RowsSkipped);
			}
			else
			{
				_logger.LogError("SpicAPI rejected {JobKey}: {Message}", job.Key, result.Message);
			}

			return result;
		}

		private static ExcelBulkUploadResult? TryParse(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return null;

			try
			{
				return JsonSerializer.Deserialize<ExcelBulkUploadResult>(body, Json);
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static string Trim(string value, int max) =>
			value.Length <= max ? value : value[..max] + "…";

		/// <summary>
		/// Catches the classic silent failure: the portal responds to an export
		/// click with an HTML "no data found" or session-expired page, which lands
		/// on disk with a report's name and would otherwise be posted to the API as
		/// if it were data.
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
					$"imported; legacy .xls is not supported.");
			}

			if (download.Extension == ".xlsx" && !LooksLikeZip(download.FilePath))
			{
				throw new InvalidDataException(
					$"'{job.Key}' has an .xlsx name but is not a real workbook — the portal probably " +
					$"returned an HTML error page. The file is kept at {download.FilePath}.");
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
