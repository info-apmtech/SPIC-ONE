using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Spic.Infrastructure.Services
{
	public class ExcelBulkUploadService : IExcelBulkUploadService
	{
		private readonly AppDbContext _db;
		private readonly ILogger<ExcelBulkUploadService> _logger;

		// Dealer names come from external Excel/CSV files. These values must never
		// create IFMS masters such as "0", "00", "000", "NA" or punctuation-only names.
		private static readonly HashSet<string> InvalidDealerNameTokens =
			new(StringComparer.OrdinalIgnoreCase)
			{
				"0", "00", "000", "0000",
				"NA", "N/A", "NONE", "NULL", "NIL",
				"NOT AVAILABLE", "UNKNOWN", "-", "--", "."
			};

		private static readonly char[] DealerNameEdgeCharacters =
			".,;:-_/\\|\"'`~*#".ToCharArray();

		private static readonly HashSet<string> InvalidProductNameTokens =
			new(StringComparer.OrdinalIgnoreCase)
			{
				"0", "00", "000", "NA", "N/A", "NONE", "NULL", "NIL",
				"NOT AVAILABLE", "UNKNOWN", "-", "--", "."
			};

		public ExcelBulkUploadService(AppDbContext db, ILogger<ExcelBulkUploadService> logger)
		{
			_db = db;
			_logger = logger;
		}

		public async Task<ExcelBulkUploadResult> ImportAsync(
			Stream fileStream,
			string currentUserId,
			string fileExtension,
			string categoryId,
			string fileName,
			DateTime? reportDate,
			CancellationToken cancellationToken = default)
		{
			if (fileStream is null || !fileStream.CanRead)
				return Failed("The uploaded file stream is not readable.");

			categoryId = (categoryId ?? string.Empty).Trim();
			fileExtension = (fileExtension ?? string.Empty).Trim().ToLowerInvariant();
			currentUserId = string.IsNullOrWhiteSpace(currentUserId) ? "System" : currentUserId.Trim();
			fileName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "upload" + fileExtension : fileName);

			if (!SupportedCategories.Contains(categoryId))
				return Failed($"Unsupported upload category '{categoryId}'.");

			if (fileExtension != ".xlsx" && fileExtension != ".csv")
				return Failed("Only .xlsx and .csv files are supported. Legacy .xls is not supported by ClosedXML.");

			if (RequiresReportDate(categoryId) && !reportDate.HasValue)
				return Failed("Report Date is required for this upload category.");

			var now = DateTime.UtcNow;
			var reportDateUtc = reportDate.HasValue ? ToUtcDate(reportDate.Value) : (DateTime?)null;
			var records = new List<Dictionary<string, string>>();

			var requiredCols = categoryId == "One"
				? new[] { "statename", "districtname", "retailerid", "retailername" }
				: categoryId == "Two"
				? new[] { "transactionid", "invoiceno", "dealername" }
				: categoryId == "Three"
				? new[] { "company", "plant", "product", "state", "district", "agencyname" }
				: categoryId == "Four"
				? new[] { "transactionid", "marketer", "wholesalerid", "wholesaleragencyname" }
				: categoryId == "Six"
				? new[] { "state", "openingstock", "openinggit", "production/imports", "receipt", "dispatches", "sales", "salesreturn", "stockadjustment", "closinggit", "closingstock" }
				: categoryId == "Seven"
				? new[] { "state", "district", "warehouse/location", "openingstock(atlocation)", "openingstock(git)", "imports/production", "receipt", "dispatches", "sales", "salesreturn", "stockadjustment", "closinggit", "closingstock" }
				: new[] { "state", "district", "dealerid", "agencyname", "dealertype", "dealershipnature", "company", "plant", "product", "stock", "stockdate" };

			bool IsHeaderRow(List<string> rowValues)
			{
				if (categoryId == "One") return rowValues.Contains("statename") && rowValues.Contains("retailerid");
				if (categoryId == "Two") return rowValues.Contains("transactionid") && rowValues.Contains("dealername");
				if (categoryId == "Three") return rowValues.Contains("serialnumber") && rowValues.Contains("agencyname");
				if (categoryId == "Four") return rowValues.Contains("transactionid") && rowValues.Contains("marketer");
				if (categoryId == "Six") return rowValues.Contains("state") && rowValues.Contains("openingstock");
				if (categoryId == "Seven") return rowValues.Contains("state") && rowValues.Contains("district") && rowValues.Contains("warehouse/location");
				return rowValues.Contains("state") && rowValues.Contains("dealerid");
			}

			string globalPlantStr = null;
			string globalProductStr = null;
			void ExtractCategoryTitle(string cellText)
			{
				if (!string.IsNullOrWhiteSpace(cellText))
				{
					if ((categoryId == "Six" && cellText.StartsWith("State-Wise Global Stock Reconciliation", StringComparison.OrdinalIgnoreCase)) ||
						(categoryId == "Seven" && cellText.StartsWith("District-Wise Details Global Stock Reconciliation", StringComparison.OrdinalIgnoreCase)))
					{
						var parts = cellText.Split(new[] { " for " }, StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length >= 3)
						{
							globalPlantStr = parts[1].Trim();
							if (globalPlantStr.Equals("GFL", StringComparison.OrdinalIgnoreCase))
							{
								globalPlantStr = "Green Star";
							}
							globalProductStr = parts[2].Trim();
						}
					}
				}
			}

			try
			{
				if (fileExtension == ".csv")
				{
					using var reader = new StreamReader(fileStream);
					using var csv = new CsvHelper.CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { HasHeaderRecord = false });

					var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
					bool headerFound = false;
					int sourceRowNumber = 0;

					while (csv.Read())
					{
						sourceRowNumber++;
						cancellationToken.ThrowIfCancellationRequested();
						if (!headerFound)
						{
							var rowValues = new List<string>();
							for (int i = 0; csv.TryGetField<string>(i, out var field); i++)
							{
								var text = field?.Trim() ?? "";
								ExtractCategoryTitle(text);
								rowValues.Add(text.Trim('\uFEFF', '\u200B', ' ', '"').Replace(" ", "").ToLowerInvariant());
							}

							if (IsHeaderRow(rowValues))
							{
								headerFound = true;
								for (int i = 0; i < rowValues.Count; i++)
								{
									var h = rowValues[i];
									if (categoryId == "Three" && h.StartsWith("wholesalerob")) h = "wholesalerob";
									if (!string.IsNullOrEmpty(h) && !headerMap.ContainsKey(h))
										headerMap[h] = i;
								}
							}
							continue;
						}

						var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
						{
							["__rownumber"] = sourceRowNumber.ToString(CultureInfo.InvariantCulture)
						};
						foreach (var kvp in headerMap)
						{
							dict[kvp.Key] = csv.TryGetField<string>(kvp.Value, out var fieldValue)
								? fieldValue?.Trim() ?? string.Empty
								: string.Empty;
						}

						if (dict.Any(x =>
							!x.Key.StartsWith("__", StringComparison.Ordinal) &&
							!string.IsNullOrWhiteSpace(x.Value)))
						{
							records.Add(dict);
						}
					}
				}
				else
				{
					using var workbook = new XLWorkbook(fileStream);
					var ws = workbook.Worksheets.First();

					var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
					int headerRowIndex = -1;

					var rows = ws.RowsUsed().Take(10).ToList();
					foreach (var row in rows)
					{
						var rowValues = new List<string>();
						int lastCol = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
						for (int c = 1; c <= lastCol; c++)
						{
							var text = row.Cell(c).GetString().Trim();
							ExtractCategoryTitle(text);
							rowValues.Add(text.Trim('\uFEFF', '\u200B', ' ', '"').Replace(" ", "").ToLowerInvariant());
						}

						if (IsHeaderRow(rowValues))
						{
							headerRowIndex = row.RowNumber();
							for (int c = 1; c <= lastCol; c++)
							{
								var h = rowValues[c - 1];
								if (categoryId == "Three" && h.StartsWith("wholesalerob")) h = "wholesalerob";
								if (!string.IsNullOrEmpty(h) && !headerMap.ContainsKey(h))
									headerMap[h] = c;
							}
							break;
						}
					}

					if (headerRowIndex != -1)
					{
						var dataRows = ws.RowsUsed().Where(r => r.RowNumber() > headerRowIndex).ToList();
						foreach (var row in dataRows)
						{
							cancellationToken.ThrowIfCancellationRequested();
							var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
							{
								["__rownumber"] = row.RowNumber().ToString(CultureInfo.InvariantCulture)
							};
							foreach (var kvp in headerMap)
							{
								dict[kvp.Key] = row.Cell(kvp.Value).GetString().Trim();
							}

							if (dict.Any(x =>
								!x.Key.StartsWith("__", StringComparison.Ordinal) &&
								!string.IsNullOrWhiteSpace(x.Value)))
							{
								records.Add(dict);
							}
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				return Failed("Failed to parse file format: " + ex.Message);
			}

			if (records.Count == 0)
			{
				return Failed("No data rows found. Please ensure the file contains valid headers and data.");
			}

			foreach (var rc in requiredCols)
			{
				if (!records[0].ContainsKey(rc))
				{
					var foundHeaders = string.Join(", ", records[0].Keys);
					return Failed($"Missing required column: {rc}. Found headers: {foundHeaders}");
				}
			}

			if (categoryId is "Six" or "Seven")
			{
				if (string.IsNullOrWhiteSpace(globalPlantStr) ||
					string.IsNullOrWhiteSpace(globalProductStr))
				{
					return Failed(
						"Plant and Product could not be read from the reconciliation report title. " +
						"Use the approved template title: '<Report Name> for <Plant> for <Product>'.");
				}
			}

			// Strips only leading/trailing single quotes (straight ' and smart ' ')
			// so Excel text-prefixed numbers like '3326401039 or '3326401039' become
			// clean values, while inner apostrophes (e.g. O'Brien) stay intact.
			static string CleanCell(string value)
			{
				if (string.IsNullOrEmpty(value))
					return string.Empty;

				return value.Trim().Trim('\'', '\u2018', '\u2019').Trim();
			}

			string GetCell(Dictionary<string, string> rowDict, string key) =>
				rowDict.TryGetValue(key, out var val) ? CleanCell(val) : string.Empty;

			string GetFirstCell(Dictionary<string, string> rowDict, params string[] keys)
			{
				foreach (var key in keys)
				{
					var value = GetCell(rowDict, key);
					if (!string.IsNullOrWhiteSpace(value))
						return value;
				}

				return string.Empty;
			}

			static bool IsNullToken(string? value)
			{
				if (string.IsNullOrWhiteSpace(value))
					return true;

				var normalized = value.Trim();
				return normalized.Equals("NA", StringComparison.OrdinalIgnoreCase) ||
					normalized.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
					normalized.Equals("NULL", StringComparison.OrdinalIgnoreCase) ||
					normalized.Equals("NONE", StringComparison.OrdinalIgnoreCase) ||
					normalized == "-";
			}

			string OptionalText(Dictionary<string, string> rowDict, string key)
			{
				var value = GetCell(rowDict, key);
				return IsNullToken(value) ? string.Empty : value;
			}

			string? ParseOptionalMobile(
				Dictionary<string, string> rowDict,
				int rowNumber,
				params string[] keys)
			{
				var raw = GetFirstCell(rowDict, keys);
				if (string.IsNullOrWhiteSpace(raw))
					return null;

				var trimmed = raw.Trim();
				if (IsNullToken(trimmed) || trimmed == "0")
					return null;

				// Handles Excel scientific notation such as 9.87654321E+09.
				if ((trimmed.Contains('E') || trimmed.Contains('e')) &&
					double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var scientific))
				{
					trimmed = Math.Round(scientific, 0, MidpointRounding.AwayFromZero)
						.ToString("0", CultureInfo.InvariantCulture);
				}

				var digits = new string(trimmed.Where(char.IsDigit).ToArray());
				if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
					digits = digits[2..];
				else if (digits.Length == 11 && digits.StartsWith("0", StringComparison.Ordinal))
					digits = digits[1..];

				if (digits.Length is < 10 or > 15)
				{
					throw new InvalidDataException(
						$"Row {rowNumber}: Invalid mobile number '{raw}'. Expected 10 to 15 digits.");
				}

				return digits;
			}

			decimal ParseDecimal(Dictionary<string, string> rowDict, string key, int rowNumber)
			{
				var raw = GetCell(rowDict, key);
				if (string.IsNullOrWhiteSpace(raw)) return 0m;

				var normalized = raw.Replace(",", string.Empty).Trim();
				if (decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var value))
					return value;

				throw new InvalidDataException($"Row {rowNumber}: Invalid numeric value '{raw}' in column '{key}'.");
			}

			DateTime? ParseOptionalDate(Dictionary<string, string> rowDict, string key, int rowNumber)
			{
				var raw = GetCell(rowDict, key);
				if (IsNullToken(raw)) return null;
				return ParseDate(raw, rowNumber, key);
			}

			DateTime ParseRequiredDate(Dictionary<string, string> rowDict, string key, int rowNumber)
			{
				var raw = GetCell(rowDict, key);
				if (IsNullToken(raw))
					throw new InvalidDataException($"Row {rowNumber}: Column '{key}' is required.");
				return ParseDate(raw, rowNumber, key);
			}

			await using var transaction = await _db.Database.BeginTransactionAsync(
				IsolationLevel.Serializable,
				cancellationToken);
			try
			{
				var result = new ExcelBulkUploadResult
				{
					Success = true,
					Message = "Upload completed successfully.",
					FileName = fileName,
					CategoryId = categoryId,
					ReportDate = reportDateUtc,
					TotalRows = records.Count
				};

				var ifmsDealerMobileUpdatedIds = new HashSet<int>();
				var uploadedMobileByDealerLocation =
					new Dictionary<string, string>(StringComparer.Ordinal);
				var warningKeys = new HashSet<string>(StringComparer.Ordinal);

				void AddWarningOnce(string warningKey, string message)
				{
					if (!warningKeys.Add(warningKey))
						return;

					if (result.Warnings.Count < 100)
					{
						result.Warnings.Add(message);
					}
					else if (warningKeys.Add("warnings:truncated"))
					{
						result.Warnings.Add("Additional warnings were omitted after the first 100 entries.");
					}
				}

				void TrackUploadedMobile(
					string dealerLookupKey,
					string displayDealerName,
					string? mobileNo,
					int rowNumber)
				{
					if (string.IsNullOrWhiteSpace(mobileNo))
						return;

					if (uploadedMobileByDealerLocation.TryGetValue(
						dealerLookupKey,
						out var firstMobile))
					{
						if (!string.Equals(firstMobile, mobileNo, StringComparison.Ordinal))
						{
							AddWarningOnce(
								"mobile:file:" + dealerLookupKey,
								$"Row {rowNumber}: Dealer '{displayDealerName}' has multiple mobile numbers " +
								$"('{firstMobile}', '{mobileNo}') in the file. The first valid number was retained for the IFMS master.");
						}
						return;
					}

					uploadedMobileByDealerLocation[dealerLookupKey] = mobileNo;
				}

				void ApplyIfmsDealerMobile(
					IfmsDealer dealer,
					string dealerLookupKey,
					string displayDealerName,
					string? mobileNo,
					int rowNumber)
				{
					// Transaction/report files must never erase or silently replace a verified master mobile.
					if (string.IsNullOrWhiteSpace(mobileNo))
						return;

					var existingMobile = dealer.MobileNo?.Trim();
					if (string.Equals(existingMobile, mobileNo, StringComparison.Ordinal))
						return;

					if (!string.IsNullOrWhiteSpace(existingMobile))
					{
						AddWarningOnce(
							"mobile:db:" + dealerLookupKey,
							$"Row {rowNumber}: IFMS dealer '{displayDealerName}' already has mobile '{existingMobile}', " +
							$"so uploaded mobile '{mobileNo}' was not used to overwrite the master.");
						return;
					}

					dealer.MobileNo = mobileNo;
					dealer.UpdatedAt = now;
					dealer.UpdatedBy = currentUserId;

					if (dealer.Id > 0 && ifmsDealerMobileUpdatedIds.Add(dealer.Id))
						result.IfmsDealerMobileNumbersUpdated++;
				}

				// NOTE: build every lookup with GroupBy(...).First() rather than
				// ToDictionaryAsync(keySelector). If the master tables already contain
				// two rows whose names normalize to the same key (e.g. "Puducherry" and
				// "Puducherry ", or a genuine duplicate), ToDictionaryAsync throws
				// "An item with the same key has already been added". Grouping keeps the
				// first matching row instead of crashing.
				var stateDict = (await _db.States
						.Where(s => s.StateName != null)
						.Select(s => new { s.Id, s.StateName })
						.ToListAsync(cancellationToken))
					.GroupBy(s => s.StateName.Trim().ToLowerInvariant())
					.ToDictionary(g => g.Key, g => g.First().Id);

				var districtDict = (await _db.Districts
						.Where(d => d.DistrictName != null)
						.Select(d => new { d.Id, d.DistrictName, d.StateId })
						.ToListAsync(cancellationToken))
					.GroupBy(d => $"{d.DistrictName.Trim().ToLowerInvariant()}_{d.StateId}")
					.ToDictionary(g => g.Key, g => g.First().Id);

				var subDistrictDict = (await _db.SubDistricts
						.Where(sd => sd.SubDistrictName != null)
						.Select(sd => new { sd.Id, sd.SubDistrictName, sd.DistrictId })
						.ToListAsync(cancellationToken))
					.GroupBy(sd => $"{sd.SubDistrictName.Trim().ToLowerInvariant()}_{sd.DistrictId}")
					.ToDictionary(g => g.Key, g => g.First().Id);

				var rawRegDealers = await _db.DealerRegistrations
					.Where(d => d.FirmName != null)
					.Select(d => new
					{
						d.Id,
						d.FirmName,
						d.StateId,
						d.DistrictId,
						d.DealerCode,
						d.SPICCode,
						d.GreenStarCode,
						d.TnCode,
						d.WholesalemFMSCode,
						d.RetailmFMSCode
					})
					.ToListAsync(cancellationToken);

				/*
				 * Excel Dealer/Retailer ID is not saved in IfmsDealer because the
				 * existing entity must remain unchanged. It is still used to resolve
				 * an existing DealerRegistration through its available code columns.
				 */
				var registrationIdsByExternalKey =
					new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

				void AddRegistrationExternalId(string? rawExternalId, int registrationId)
				{
					foreach (var externalKey in ExternalDealerIdKeys(rawExternalId))
					{
						if (!registrationIdsByExternalKey.TryGetValue(externalKey, out var ids))
						{
							ids = new HashSet<int>();
							registrationIdsByExternalKey[externalKey] = ids;
						}

						ids.Add(registrationId);
					}
				}

				foreach (var registeredDealer in rawRegDealers)
				{
					AddRegistrationExternalId(registeredDealer.DealerCode, registeredDealer.Id);
					AddRegistrationExternalId(registeredDealer.SPICCode, registeredDealer.Id);
					AddRegistrationExternalId(registeredDealer.GreenStarCode, registeredDealer.Id);
					AddRegistrationExternalId(registeredDealer.TnCode, registeredDealer.Id);
					AddRegistrationExternalId(registeredDealer.WholesalemFMSCode, registeredDealer.Id);
					AddRegistrationExternalId(registeredDealer.RetailmFMSCode, registeredDealer.Id);
				}

				var dealerRegByLocation = rawRegDealers
					.GroupBy(d => DealerLocationKey(d.FirmName, d.StateId, d.DistrictId), StringComparer.Ordinal)
					.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList(), StringComparer.Ordinal);

				var dealerRegByNameState = rawRegDealers
					.GroupBy(d => DealerNameStateKey(d.FirmName, d.StateId), StringComparer.Ordinal)
					.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList(), StringComparer.Ordinal);

				var rawIfmsDealers = await _db.IfmsDealers
					.Where(d => d.Name != null)
					.ToListAsync(cancellationToken);

				var ifmsDealerByLocation = rawIfmsDealers
					.GroupBy(d => DealerLocationKey(d.Name, d.StateId, d.DistrictId), StringComparer.Ordinal)
					.ToDictionary(
						g => g.Key,
						g => g.OrderByDescending(x => x.UpdatedAt)
							.ThenByDescending(x => x.Id)
							.ToList(),
						StringComparer.Ordinal);

				var ifmsDealerByNameState = rawIfmsDealers
					.GroupBy(d => DealerNameStateKey(d.Name, d.StateId), StringComparer.Ordinal)
					.ToDictionary(
						g => g.Key,
						g => g.OrderByDescending(x => x.UpdatedAt)
							.ThenByDescending(x => x.Id)
							.ToList(),
						StringComparer.Ordinal);

				var ifmsDealerByComposite = rawIfmsDealers
					.GroupBy(
						d => IfmsDealerCompositeKey(
							d.Name,
							d.StateId,
							d.DistrictId,
							d.MobileNo,
							d.DealerTypeId,
							d.DealershipNatureId),
						StringComparer.Ordinal)
					.ToDictionary(
						g => g.Key,
						g => g.OrderBy(x => x.Id).ToList(),
						StringComparer.Ordinal);

				/*
				 * Since IfmsDealer has no external-ID column, this map keeps each Excel
				 * dealer ID tied to one IFMS row during this import. The reverse map
				 * prevents two different Excel IDs from collapsing into the same IFMS row.
				 */
				var ifmsDealerByUploadedExternalKey =
					new Dictionary<string, IfmsDealer>(StringComparer.Ordinal);
				var claimedIfmsDealers =
					new Dictionary<IfmsDealer, string>(ReferenceComparer<IfmsDealer>.Instance);

				static bool SameIfmsDealer(IfmsDealer left, IfmsDealer right) =>
					ReferenceEquals(left, right) ||
					(left.Id > 0 && right.Id > 0 && left.Id == right.Id);

				void AddIfmsToIndexes(IfmsDealer dealer)
				{
					var locationKey = DealerLocationKey(
						dealer.Name,
						dealer.StateId,
						dealer.DistrictId);

					if (!ifmsDealerByLocation.TryGetValue(locationKey, out var locationRows))
					{
						locationRows = new List<IfmsDealer>();
						ifmsDealerByLocation[locationKey] = locationRows;
					}
					if (!locationRows.Any(x => SameIfmsDealer(x, dealer)))
						locationRows.Add(dealer);

					var nameStateKey = DealerNameStateKey(dealer.Name, dealer.StateId);
					if (!ifmsDealerByNameState.TryGetValue(nameStateKey, out var stateRows))
					{
						stateRows = new List<IfmsDealer>();
						ifmsDealerByNameState[nameStateKey] = stateRows;
					}
					if (!stateRows.Any(x => SameIfmsDealer(x, dealer)))
						stateRows.Add(dealer);

					var compositeKey = IfmsDealerCompositeKey(
						dealer.Name,
						dealer.StateId,
						dealer.DistrictId,
						dealer.MobileNo,
						dealer.DealerTypeId,
						dealer.DealershipNatureId);

					if (!ifmsDealerByComposite.TryGetValue(compositeKey, out var compositeRows))
					{
						compositeRows = new List<IfmsDealer>();
						ifmsDealerByComposite[compositeKey] = compositeRows;
					}
					if (!compositeRows.Any(x => SameIfmsDealer(x, dealer)))
						compositeRows.Add(dealer);
				}

				(int? RegistrationId, IfmsDealer? IfmsDealer) ResolveDealer(
					string? externalDealerId,
					string? dealerName,
					int? dealerStateId,
					int? dealerDistrictId,
					int? dealerTypeId,
					int? dealerNatureId,
					string? mobileNo,
					int rowNumber,
					string roleName)
				{
					var cleanedDealerName = CleanDealerName(dealerName);
					var externalKeys = ExternalDealerIdKeys(externalDealerId);
					var primaryExternalKey = externalKeys.FirstOrDefault() ?? string.Empty;

					if (string.IsNullOrWhiteSpace(cleanedDealerName) && externalKeys.Count == 0)
						return (null, null);

					var displayDealerName = cleanedDealerName;

					/*
					 * When Excel supplies an ID, that ID is authoritative for checking
					 * DealerRegistration. A name-only registration match is not used in
					 * this branch because different Excel IDs can share the same name.
					 */
					if (externalKeys.Count > 0)
					{
						var registrationIds = externalKeys
							.Where(registrationIdsByExternalKey.ContainsKey)
							.SelectMany(x => registrationIdsByExternalKey[x])
							.Distinct()
							.ToList();

						if (registrationIds.Count > 1)
						{
							var narrowed = rawRegDealers
								.Where(x => registrationIds.Contains(x.Id))
								.Where(x => !dealerStateId.HasValue || x.StateId == dealerStateId)
								.Where(x => !dealerDistrictId.HasValue || x.DistrictId == dealerDistrictId)
								.Where(x => string.IsNullOrWhiteSpace(cleanedDealerName) ||
									NormalizeDealerNameKey(x.FirmName) == NormalizeDealerNameKey(cleanedDealerName))
								.Select(x => x.Id)
								.Distinct()
								.ToList();

							if (narrowed.Count == 1)
								registrationIds = narrowed;
						}

						if (registrationIds.Count > 1)
						{
							throw new InvalidDataException(
								$"Row {rowNumber}: Excel {roleName} ID '{externalDealerId}' " +
								"matches more than one DealerRegistration row.");
						}

						if (registrationIds.Count == 1)
							return (registrationIds[0], null);

						var mappedIfmsDealers = externalKeys
							.Where(ifmsDealerByUploadedExternalKey.ContainsKey)
							.Select(x => ifmsDealerByUploadedExternalKey[x])
							.Distinct(ReferenceComparer<IfmsDealer>.Instance)
							.ToList();

						if (mappedIfmsDealers.Count > 1)
						{
							throw new InvalidDataException(
								$"Row {rowNumber}: Excel {roleName} ID '{externalDealerId}' " +
								"was mapped to multiple IFMS dealer rows during this upload.");
						}

						IfmsDealer? ifmsDealer;
						if (mappedIfmsDealers.Count == 1)
						{
							ifmsDealer = mappedIfmsDealers[0];
							if (!IsValidDealerName(displayDealerName))
								displayDealerName = CleanDealerName(ifmsDealer.Name);
						}
						else
						{
							// An unmatched external ID must not be used as the IFMS dealer name.
							// A valid textual dealer name is required before an IFMS master can be created.
							if (!IsValidDealerName(cleanedDealerName))
							{
								throw new InvalidDataException(
									$"Row {rowNumber}: Invalid {roleName} name '{dealerName}'. " +
									$"Excel ID '{externalDealerId}' did not match DealerRegistration, so a valid name containing letters is required.");
							}

							var compositeKey = IfmsDealerCompositeKey(
								cleanedDealerName,
								dealerStateId,
								dealerDistrictId,
								mobileNo,
								dealerTypeId,
								dealerNatureId);

							var candidates = ifmsDealerByComposite
								.TryGetValue(compositeKey, out var exactRows)
								? exactRows.OrderBy(x => x.Id == 0 ? int.MaxValue : x.Id).ToList()
								: new List<IfmsDealer>();

							if (candidates.Count == 0)
							{
								var locationKey = DealerLocationKey(
									cleanedDealerName,
									dealerStateId,
									dealerDistrictId);

								if (ifmsDealerByLocation.TryGetValue(locationKey, out var locationRows))
								{
									candidates = locationRows
										.Where(x => string.IsNullOrWhiteSpace(mobileNo) ||
											string.IsNullOrWhiteSpace(x.MobileNo) ||
											string.Equals(x.MobileNo.Trim(), mobileNo, StringComparison.Ordinal))
										.OrderBy(x => x.Id == 0 ? int.MaxValue : x.Id)
										.ToList();
								}
							}

							/*
							 * Do not assign one IFMS row to two different Excel IDs in the same
							 * file. This is what prevents same-name retailers from collapsing.
							 */
							ifmsDealer = candidates.FirstOrDefault(x =>
								!claimedIfmsDealers.TryGetValue(x, out var claimedBy) ||
								string.Equals(claimedBy, primaryExternalKey, StringComparison.Ordinal));

							if (ifmsDealer is null)
							{
								ifmsDealer = new IfmsDealer
								{
									Name = displayDealerName,
									MobileNo = mobileNo,
									StateId = dealerStateId,
									DistrictId = dealerDistrictId,
									DealerTypeId = dealerTypeId,
									DealershipNatureId = dealerNatureId,
									CreatedAt = now,
									UpdatedAt = now,
									UpdatedBy = currentUserId
								};

								_db.IfmsDealers.Add(ifmsDealer);
								AddIfmsToIndexes(ifmsDealer);
								result.NewMastersCreated.IfmsDealers++;
							}

							claimedIfmsDealers[ifmsDealer] = primaryExternalKey;
							foreach (var externalKey in externalKeys)
								ifmsDealerByUploadedExternalKey[externalKey] = ifmsDealer;
						}

						var trackingKey = "external:" + primaryExternalKey;
						TrackUploadedMobile(
							trackingKey,
							displayDealerName,
							mobileNo,
							rowNumber);
						ApplyIfmsDealerMobile(
							ifmsDealer,
							trackingKey,
							displayDealerName,
							mobileNo,
							rowNumber);

						return (null, ifmsDealer);
					}

					/*
					 * Legacy templates without Dealer ID use the existing name/location
					 * resolution. This path cannot separate two otherwise identical dealers.
					 */
					if (string.IsNullOrWhiteSpace(cleanedDealerName))
						return (null, null);

					if (!IsValidDealerName(cleanedDealerName))
					{
						throw new InvalidDataException(
							$"Row {rowNumber}: Invalid {roleName} name '{dealerName}'. " +
							"Use a real dealer/agency name containing letters.");
					}

					var dealerLookupKey = DealerLocationKey(
						cleanedDealerName,
						dealerStateId,
						dealerDistrictId);

					if (dealerRegByLocation.TryGetValue(dealerLookupKey, out var registeredMatches))
					{
						if (registeredMatches.Count > 1)
						{
							throw new InvalidDataException(
								$"Row {rowNumber}: Multiple DealerRegistration rows match {roleName} '{cleanedDealerName}' " +
								$"for StateId={dealerStateId?.ToString() ?? "null"}, DistrictId={dealerDistrictId?.ToString() ?? "null"}.");
						}

						return (registeredMatches[0].Id, null);
					}

					var dealerNameStateKey = DealerNameStateKey(cleanedDealerName, dealerStateId);
					if (dealerRegByNameState.TryGetValue(dealerNameStateKey, out var nameStateMatches) &&
						nameStateMatches.Count == 1)
					{
						AddWarningOnce(
							"dealer:fallback:" + dealerLookupKey,
							$"Row {rowNumber}: {roleName} '{cleanedDealerName}' matched DealerRegistration by name and state because the district did not match exactly.");
						return (nameStateMatches[0].Id, null);
					}

					TrackUploadedMobile(
						"location:" + dealerLookupKey,
						cleanedDealerName,
						mobileNo,
						rowNumber);

					IfmsDealer? legacyIfmsDealer = null;
					if (ifmsDealerByLocation.TryGetValue(dealerLookupKey, out var legacyMatches) &&
						legacyMatches.Count > 0)
					{
						legacyIfmsDealer = legacyMatches
							.OrderByDescending(x => x.UpdatedAt)
							.ThenByDescending(x => x.Id)
							.First();

						if (legacyMatches.Count > 1)
						{
							AddWarningOnce(
								"ifms:duplicate:" + dealerLookupKey,
								$"Multiple IFMS dealer masters exist for '{cleanedDealerName}' in the same state/district. " +
								$"The latest row (Id {legacyIfmsDealer.Id}) was used.");
						}
					}
					else
					{
						legacyIfmsDealer = new IfmsDealer
						{
							Name = cleanedDealerName,
							MobileNo = mobileNo,
							StateId = dealerStateId,
							DistrictId = dealerDistrictId,
							DealerTypeId = dealerTypeId,
							DealershipNatureId = dealerNatureId,
							CreatedAt = now,
							UpdatedAt = now,
							UpdatedBy = currentUserId
						};

						_db.IfmsDealers.Add(legacyIfmsDealer);
						AddIfmsToIndexes(legacyIfmsDealer);
						result.NewMastersCreated.IfmsDealers++;
					}

					ApplyIfmsDealerMobile(
						legacyIfmsDealer,
						"location:" + dealerLookupKey,
						cleanedDealerName,
						mobileNo,
						rowNumber);

					return (null, legacyIfmsDealer);
				}

				// Local helper: load a name-keyed master into a duplicate-safe dictionary.
				async Task<Dictionary<string, int>> LoadNameDictAsync<TEntity>(
					IQueryable<TEntity> query,
					Func<TEntity, string?> nameSelector,
					Func<TEntity, int> idSelector)
					where TEntity : class
				{
					var list = await query
						.AsNoTracking()
						.ToListAsync(cancellationToken);

					return list
						.Select(entity => new
						{
							Entity = entity,
							Name = nameSelector(entity)
						})
						.Where(item => !string.IsNullOrWhiteSpace(item.Name))
						.GroupBy(
							item => item.Name!.Trim().ToLowerInvariant(),
							StringComparer.Ordinal)
						.ToDictionary(
							group => group.Key,
							group => idSelector(group.First().Entity),
							StringComparer.Ordinal);
				}

				var dealerTypeDict = await LoadNameDictAsync(_db.DealerTypes, dt => dt.Name, dt => dt.Id);
				var natureDict = await LoadNameDictAsync(_db.DealershipNatures, n => n.Name, n => n.Id);
				var companyDict = await LoadNameDictAsync(_db.Companies, c => c.Name, c => c.Id);
				var plantDict = await LoadNameDictAsync(_db.Plants, p => p.Name, p => p.Id);
				var productDict = await LoadNameDictAsync(_db.Products, p => p.Name, p => p.Id);
				var ifmsProductDict = await LoadNameDictAsync(
					_db.Set<IfmsProduct>(),
					p => p.Name,
					p => p.Id);

				var txnTypeDict = await LoadNameDictAsync(_db.TxnTypes, t => t.Name, t => t.Id);
				var unitDict = await LoadNameDictAsync(_db.Units, u => u.Name, u => u.Id);
				var statusDict = await LoadNameDictAsync(_db.Statuses, s => s.Name, s => s.Id);
				var ackThroughDict = await LoadNameDictAsync(_db.AckThroughs, a => a.Name, a => a.Id);
				var warehouseDict = await LoadNameDictAsync(_db.Warehouses, w => w.Name, w => w.Id);

				// -----------------------------------------------------------------
				// PRELOAD AND BATCH-CREATE EVERY MASTER USED BY THIS FILE.
				//
				// This pass removes SaveChangesAsync from the row-processing hot path.
				// The transaction and all existing validation/rollback semantics remain.
				// -----------------------------------------------------------------
				var preparedMasterRows = new List<PreparedMasterRow>(records.Count);
				var preloadLastState = string.Empty;

				for (var preloadIndex = 0; preloadIndex < records.Count; preloadIndex++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var sourceRow = records[preloadIndex];
					var preloadRowNumber = int.TryParse(
						GetCell(sourceRow, "__rownumber"),
						NumberStyles.Integer,
						CultureInfo.InvariantCulture,
						out var parsedPreloadRowNumber)
						? parsedPreloadRowNumber
						: preloadIndex + 2;

					var preloadState = categoryId == "One"
						? GetCell(sourceRow, "statename")
						: GetCell(sourceRow, "state");
					var preloadDistrict = categoryId == "One"
						? GetCell(sourceRow, "districtname")
						: GetCell(sourceRow, "district");
					var preloadDealerId = categoryId == "One"
						? GetCell(sourceRow, "retailerid")
						: GetCell(sourceRow, "dealerid");
					var preloadDealerName = CleanDealerName(
						categoryId == "One"
							? GetCell(sourceRow, "retailername")
							: categoryId == "Two"
								? GetCell(sourceRow, "dealername")
								: GetCell(sourceRow, "agencyname"));

					if (categoryId == "Seven")
					{
						if (IsWarehouseTotalLabel(preloadState))
						{
							preparedMasterRows.Add(new PreparedMasterRow
							{
								Index = preloadIndex,
								RowNumber = preloadRowNumber,
								Skip = true
							});
							continue;
						}

						if (string.IsNullOrWhiteSpace(preloadState) &&
							!string.IsNullOrWhiteSpace(preloadDistrict))
						{
							preloadState = preloadLastState;
						}
						else if (!string.IsNullOrWhiteSpace(preloadState) &&
							!preloadState.Equals("plant", StringComparison.OrdinalIgnoreCase))
						{
							preloadLastState = preloadState;
						}
					}

					var preloadWarehouse = GetCell(sourceRow, "warehouse/location");
					var preloadIsBlank =
						string.IsNullOrEmpty(preloadState) &&
						string.IsNullOrEmpty(preloadDistrict) &&
						string.IsNullOrEmpty(preloadDealerId) &&
						string.IsNullOrEmpty(preloadDealerName) &&
						string.IsNullOrEmpty(preloadWarehouse);

					var preloadSkip = preloadIsBlank ||
						(categoryId == "Six" &&
						 string.Equals(preloadState, "total", StringComparison.OrdinalIgnoreCase));

					var preloadDealerMobile = ParseOptionalMobile(
						sourceRow,
						preloadRowNumber,
						"mobileno",
						"mobileno.",
						"mobilenumber",
						"mobile",
						"contactno",
						"contactnumber",
						"phoneno",
						"phonenumber");

					var wholesalerName = CleanDealerName(GetCell(sourceRow, "wholesaleragencyname"));
					var wholesalerMobile = ParseOptionalMobile(
						sourceRow,
						preloadRowNumber,
						"wholesalermobileno",
						"wholesalermobileno.",
						"wholesalermobilenumber",
						"wholesalercontactno",
						"wholesalercontactnumber",
						"wholesalerphoneno");

					if (string.IsNullOrWhiteSpace(wholesalerMobile) &&
						string.Equals(wholesalerName, preloadDealerName, StringComparison.OrdinalIgnoreCase))
					{
						wholesalerMobile = preloadDealerMobile;
					}

					if (categoryId is "One" or "Two" or "Three" or "Five")
					{
						EnsureValidDealerName(preloadDealerName, preloadRowNumber, "Dealer/Retailer/Agency");
					}

					if (categoryId == "Four")
					{
						EnsureValidDealerName(wholesalerName, preloadRowNumber, "Wholesaler agency");

						// Buyer/retailer name can be omitted only when a usable external ID resolves
						// to an existing DealerRegistration. A supplied invalid name is rejected.
						if (!string.IsNullOrWhiteSpace(preloadDealerName) &&
							!IsValidDealerName(preloadDealerName))
						{
							throw new InvalidDataException(
								$"Row {preloadRowNumber}: Invalid buyer dealer name '{preloadDealerName}'.");
						}
					}

					var preloadProductName = CleanProductName(
						categoryId is "Six" or "Seven"
							? globalProductStr ?? string.Empty
							: categoryId is "Two" or "Four"
								? GetCell(sourceRow, "companyproduct")
								: GetCell(sourceRow, "product"));

					if (!preloadSkip)
						EnsureValidProductName(preloadProductName, preloadRowNumber);

					preparedMasterRows.Add(new PreparedMasterRow
					{
						Index = preloadIndex,
						RowNumber = preloadRowNumber,
						Skip = preloadSkip,
						StateName = preloadState,
						DistrictName = preloadDistrict,
						SellerDistrictName = GetCell(sourceRow, "sellerdistrict"),
						BuyerDistrictName = GetCell(sourceRow, "buyerdistrict"),
						SubDistrictName = categoryId == "One" ? GetCell(sourceRow, "subdistrict") : string.Empty,
						DealerExternalId = preloadDealerId,
						DealerName = preloadDealerName,
						DealerMobileNo = preloadDealerMobile,
						DealerTypeName = categoryId == "One" ? string.Empty : GetCell(sourceRow, "dealertype"),
						NatureName = categoryId == "Three"
							? OptionalText(sourceRow, "dealernature")
							: GetCell(sourceRow, "dealershipnature"),
						CompanyName = categoryId is "Two" or "Four"
							? GetCell(sourceRow, "manufacturer")
							: GetCell(sourceRow, "company"),
						PlantName = categoryId is "Six" or "Seven"
							? globalPlantStr ?? string.Empty
							: GetCell(sourceRow, "plant"),
						ProductName = CleanProductName(
							categoryId is "Six" or "Seven"
								? globalProductStr ?? string.Empty
								: categoryId is "Two" or "Four"
									? GetCell(sourceRow, "companyproduct")
									: GetCell(sourceRow, "product")),
						MarketerName = categoryId is "Two" or "Four"
							? OptionalText(sourceRow, "marketer")
							: string.Empty,
						WholesalerExternalId = GetCell(sourceRow, "wholesalerid"),
						WholesalerName = wholesalerName,
						WholesalerMobileNo = wholesalerMobile,
						WholesalerNatureName = OptionalText(sourceRow, "wholesalernature"),
						DealerNatureName = OptionalText(sourceRow, "dealernature"),
						TxnTypeName = OptionalText(sourceRow, "txntype"),
						UnitName = OptionalText(sourceRow, "unit"),
						StatusName = OptionalText(sourceRow, "status"),
						AckThroughName = OptionalText(sourceRow, "ackthrough"),
						WarehouseName = preloadWarehouse
					});
				}

				var activePreparedRows = preparedMasterRows.Where(x => !x.Skip).ToList();

				// States are parent masters and must be saved before Districts.
				var newStates = activePreparedRows
					.Where(x => !string.IsNullOrWhiteSpace(x.StateName))
					.Where(x => !(categoryId is "Six" or "Seven") ||
						!x.StateName.Equals("plant", StringComparison.OrdinalIgnoreCase))
					.GroupBy(x => NormalizeKey(x.StateName), StringComparer.Ordinal)
					.Where(x => x.Key.Length > 0 && !stateDict.ContainsKey(x.Key))
					.Select(x => new State
					{
						StateName = x.First().StateName,
						ZoneId = 1,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();

				if (newStates.Count > 0)
				{
					_db.States.AddRange(newStates);
					await _db.SaveChangesAsync(cancellationToken);
					foreach (var state in newStates)
						stateDict[NormalizeKey(state.StateName)] = state.Id;
					result.NewMastersCreated.States += newStates.Count;
				}

				foreach (var prepared in activePreparedRows)
				{
					if (!string.IsNullOrWhiteSpace(prepared.StateName) &&
						!((categoryId is "Six" or "Seven") &&
						  prepared.StateName.Equals("plant", StringComparison.OrdinalIgnoreCase)))
					{
						stateDict.TryGetValue(NormalizeKey(prepared.StateName), out var preparedStateId);
						prepared.StateId = preparedStateId == 0 ? null : preparedStateId;
					}
				}

				var pendingDistricts = new Dictionary<string, (string Name, int StateId)>(StringComparer.Ordinal);
				void CollectDistrict(string? name, int? stateId)
				{
					if (string.IsNullOrWhiteSpace(name) || !stateId.HasValue)
						return;

					var key = $"{NormalizeKey(name)}_{stateId.Value}";
					if (!districtDict.ContainsKey(key) && !pendingDistricts.ContainsKey(key))
						pendingDistricts[key] = (name.Trim(), stateId.Value);
				}

				foreach (var prepared in activePreparedRows)
				{
					CollectDistrict(prepared.DistrictName, prepared.StateId);
					if (categoryId == "Four")
					{
						CollectDistrict(prepared.SellerDistrictName, prepared.StateId);
						CollectDistrict(prepared.BuyerDistrictName, prepared.StateId);
					}
				}

				var newDistricts = pendingDistricts.Values
					.Select(x => new District
					{
						DistrictName = x.Name,
						StateId = x.StateId,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();

				if (newDistricts.Count > 0)
				{
					_db.Districts.AddRange(newDistricts);
					await _db.SaveChangesAsync(cancellationToken);
					foreach (var district in newDistricts)
						districtDict[$"{NormalizeKey(district.DistrictName)}_{district.StateId}"] = district.Id;
					result.NewMastersCreated.Districts += newDistricts.Count;
				}

				foreach (var prepared in activePreparedRows)
				{
					if (prepared.StateId.HasValue)
					{
						if (!string.IsNullOrWhiteSpace(prepared.DistrictName) &&
							districtDict.TryGetValue(
								$"{NormalizeKey(prepared.DistrictName)}_{prepared.StateId.Value}",
								out var preparedDistrictId))
						{
							prepared.DistrictId = preparedDistrictId;
						}

						if (!string.IsNullOrWhiteSpace(prepared.SellerDistrictName) &&
							districtDict.TryGetValue(
								$"{NormalizeKey(prepared.SellerDistrictName)}_{prepared.StateId.Value}",
								out var sellerDistrictId))
						{
							prepared.SellerDistrictId = sellerDistrictId;
						}

						if (!string.IsNullOrWhiteSpace(prepared.BuyerDistrictName) &&
							districtDict.TryGetValue(
								$"{NormalizeKey(prepared.BuyerDistrictName)}_{prepared.StateId.Value}",
								out var buyerDistrictId))
						{
							prepared.BuyerDistrictId = buyerDistrictId;
						}
					}
				}

				var pendingSubDistricts = activePreparedRows
					.Where(x => categoryId == "One" &&
						!string.IsNullOrWhiteSpace(x.SubDistrictName) &&
						x.DistrictId.HasValue)
					.GroupBy(
						x => $"{NormalizeKey(x.SubDistrictName)}_{x.DistrictId!.Value}",
						StringComparer.Ordinal)
					.Where(x => !subDistrictDict.ContainsKey(x.Key))
					.Select(x => new SubDistrict
					{
						SubDistrictName = x.First().SubDistrictName,
						DistrictId = x.First().DistrictId!.Value,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();

				if (pendingSubDistricts.Count > 0)
				{
					_db.SubDistricts.AddRange(pendingSubDistricts);
					await _db.SaveChangesAsync(cancellationToken);
					foreach (var subDistrict in pendingSubDistricts)
						subDistrictDict[$"{NormalizeKey(subDistrict.SubDistrictName)}_{subDistrict.DistrictId}"] = subDistrict.Id;
					result.NewMastersCreated.SubDistricts += pendingSubDistricts.Count;
				}

				foreach (var prepared in activePreparedRows)
				{
					if (!string.IsNullOrWhiteSpace(prepared.SubDistrictName) && prepared.DistrictId.HasValue &&
						subDistrictDict.TryGetValue(
							$"{NormalizeKey(prepared.SubDistrictName)}_{prepared.DistrictId.Value}",
							out var preparedSubDistrictId))
					{
						prepared.SubDistrictId = preparedSubDistrictId;
					}
				}

				var dealerTypeNames = new Dictionary<string, string>(StringComparer.Ordinal);
				var natureNames = new Dictionary<string, string>(StringComparer.Ordinal);
				var companyNames = new Dictionary<string, string>(StringComparer.Ordinal);
				var plantNames = new Dictionary<string, string>(StringComparer.Ordinal);
				var productNames = new Dictionary<string, string>(StringComparer.Ordinal);
				var txnTypeNames = new Dictionary<string, string>(StringComparer.Ordinal);
				var unitNames = new Dictionary<string, string>(StringComparer.Ordinal);
				var statusNames = new Dictionary<string, string>(StringComparer.Ordinal);
				var ackThroughNames = new Dictionary<string, string>(StringComparer.Ordinal);
				var warehouseNames = new Dictionary<string, string>(StringComparer.Ordinal);

				static void CollectName(
					IDictionary<string, string> destination,
					string? value)
				{
					if (string.IsNullOrWhiteSpace(value))
						return;

					var key = NormalizeKey(value);
					if (key.Length > 0 && !destination.ContainsKey(key))
						destination[key] = value.Trim();
				}

				foreach (var prepared in activePreparedRows)
				{
					CollectName(dealerTypeNames, prepared.DealerTypeName);
					CollectName(natureNames, prepared.NatureName);
					CollectName(natureNames, prepared.WholesalerNatureName);
					CollectName(natureNames, prepared.DealerNatureName);
					CollectName(companyNames, prepared.CompanyName);
					CollectName(companyNames, prepared.MarketerName);
					CollectName(plantNames, prepared.PlantName);
					CollectName(productNames, prepared.ProductName);
					CollectName(txnTypeNames, prepared.TxnTypeName);
					CollectName(unitNames, prepared.UnitName);
					CollectName(statusNames, prepared.StatusName);
					CollectName(ackThroughNames, prepared.AckThroughName);
					CollectName(warehouseNames, prepared.WarehouseName);
				}

				var newDealerTypes = dealerTypeNames
					.Where(x => !dealerTypeDict.ContainsKey(x.Key))
					.Select(x => new DealerType
					{
						Name = x.Value,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();
				var newNatures = natureNames
					.Where(x => !natureDict.ContainsKey(x.Key))
					.Select(x => new DealershipNature
					{
						Name = x.Value,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();
				var newCompanies = companyNames
					.Where(x => !companyDict.ContainsKey(x.Key))
					.Select(x => new Company
					{
						Name = x.Value,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();
				var newPlants = plantNames
					.Where(x => !plantDict.ContainsKey(x.Key))
					.Select(x => new Plant
					{
						Name = x.Value,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();
				var newIfmsProducts = productNames
					.Where(x => !productDict.ContainsKey(x.Key))
					.Where(x => !ifmsProductDict.ContainsKey(x.Key))
					.Select(x => new IfmsProduct
					{
						Name = x.Value,
						CategoryId = 1,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();
				var newTxnTypes = txnTypeNames
					.Where(x => !txnTypeDict.ContainsKey(x.Key))
					.Select(x => new TxnType
					{
						Name = x.Value,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();
				var newUnits = unitNames
					.Where(x => !unitDict.ContainsKey(x.Key))
					.Select(x => new Unit
					{
						Name = x.Value,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();
				var newStatuses = statusNames
					.Where(x => !statusDict.ContainsKey(x.Key))
					.Select(x => new Status
					{
						Name = x.Value,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();
				var newAckThroughs = ackThroughNames
					.Where(x => !ackThroughDict.ContainsKey(x.Key))
					.Select(x => new AckThrough
					{
						Name = x.Value,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();
				var newWarehouses = warehouseNames
					.Where(x => !warehouseDict.ContainsKey(x.Key))
					.Select(x => new Warehouse
					{
						Name = x.Value,
						WarehouseCode = string.Empty,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						UpdatedBy = currentUserId
					})
					.ToList();

				if (newDealerTypes.Count > 0) _db.DealerTypes.AddRange(newDealerTypes);
				if (newNatures.Count > 0) _db.DealershipNatures.AddRange(newNatures);
				if (newCompanies.Count > 0) _db.Companies.AddRange(newCompanies);
				if (newPlants.Count > 0) _db.Plants.AddRange(newPlants);
				if (newIfmsProducts.Count > 0) _db.Set<IfmsProduct>().AddRange(newIfmsProducts);
				if (newTxnTypes.Count > 0) _db.TxnTypes.AddRange(newTxnTypes);
				if (newUnits.Count > 0) _db.Units.AddRange(newUnits);
				if (newStatuses.Count > 0) _db.Statuses.AddRange(newStatuses);
				if (newAckThroughs.Count > 0) _db.AckThroughs.AddRange(newAckThroughs);
				if (newWarehouses.Count > 0) _db.Warehouses.AddRange(newWarehouses);

				var independentMasterCount =
					newDealerTypes.Count + newNatures.Count + newCompanies.Count +
					newPlants.Count + newIfmsProducts.Count + newTxnTypes.Count +
					newUnits.Count + newStatuses.Count + newAckThroughs.Count +
					newWarehouses.Count;

				if (independentMasterCount > 0)
				{
					await _db.SaveChangesAsync(cancellationToken);
					foreach (var item in newDealerTypes) dealerTypeDict[NormalizeKey(item.Name)] = item.Id;
					foreach (var item in newNatures) natureDict[NormalizeKey(item.Name)] = item.Id;
					foreach (var item in newCompanies) companyDict[NormalizeKey(item.Name)] = item.Id;
					foreach (var item in newPlants) plantDict[NormalizeKey(item.Name)] = item.Id;
					foreach (var item in newIfmsProducts) ifmsProductDict[NormalizeKey(item.Name)] = item.Id;
					foreach (var item in newTxnTypes) txnTypeDict[NormalizeKey(item.Name)] = item.Id;
					foreach (var item in newUnits) unitDict[NormalizeKey(item.Name)] = item.Id;
					foreach (var item in newStatuses) statusDict[NormalizeKey(item.Name)] = item.Id;
					foreach (var item in newAckThroughs) ackThroughDict[NormalizeKey(item.Name)] = item.Id;
					foreach (var item in newWarehouses) warehouseDict[NormalizeKey(item.Name)] = item.Id;

					result.NewMastersCreated.DealerTypes += newDealerTypes.Count;
					result.NewMastersCreated.DealershipNatures += newNatures.Count;
					result.NewMastersCreated.Companies += newCompanies.Count;
					result.NewMastersCreated.Plants += newPlants.Count;
					result.NewMastersCreated.IfmsProducts += newIfmsProducts.Count;
					result.NewMastersCreated.TxnTypes += newTxnTypes.Count;
					result.NewMastersCreated.Units += newUnits.Count;
					result.NewMastersCreated.Statuses += newStatuses.Count;
					result.NewMastersCreated.AckThroughs += newAckThroughs.Count;
					result.NewMastersCreated.Warehouses += newWarehouses.Count;
				}

				foreach (var prepared in activePreparedRows)
				{
					if (!string.IsNullOrWhiteSpace(prepared.DealerTypeName))
						prepared.DealerTypeId = dealerTypeDict[NormalizeKey(prepared.DealerTypeName)];
					if (!string.IsNullOrWhiteSpace(prepared.NatureName))
						prepared.NatureId = natureDict[NormalizeKey(prepared.NatureName)];
					if (!string.IsNullOrWhiteSpace(prepared.WholesalerNatureName))
						prepared.WholesalerNatureId = natureDict[NormalizeKey(prepared.WholesalerNatureName)];
					if (!string.IsNullOrWhiteSpace(prepared.DealerNatureName))
						prepared.DealerNatureId = natureDict[NormalizeKey(prepared.DealerNatureName)];
					if (!string.IsNullOrWhiteSpace(prepared.ProductName))
					{
						var productKey = NormalizeKey(prepared.ProductName);
						if (productDict.TryGetValue(productKey, out var masterProductId))
						{
							prepared.ProductId = masterProductId;
							prepared.IfmsProductId = null;
						}
						else if (ifmsProductDict.TryGetValue(productKey, out var ifmsProductId))
						{
							prepared.ProductId = null;
							prepared.IfmsProductId = ifmsProductId;
						}
						else
						{
							throw new InvalidOperationException(
								$"Row {prepared.RowNumber}: Product '{prepared.ProductName}' was not resolved.");
						}
					}
				}

				var dealerResolutionByRow = new Dictionary<int, (int? RegistrationId, IfmsDealer? IfmsDealer)>();
				var buyerResolutionByRow = new Dictionary<int, (int? RegistrationId, IfmsDealer? IfmsDealer)>();
				var wholesalerResolutionByRow = new Dictionary<int, (int? RegistrationId, IfmsDealer? IfmsDealer)>();

				foreach (var prepared in activePreparedRows)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (categoryId == "Four")
					{
						buyerResolutionByRow[prepared.Index] = ResolveDealer(
							prepared.DealerExternalId,
							prepared.DealerName,
							prepared.StateId,
							prepared.BuyerDistrictId,
							prepared.DealerTypeId,
							prepared.DealerNatureId,
							prepared.DealerMobileNo,
							prepared.RowNumber,
							"buyer dealer");

						wholesalerResolutionByRow[prepared.Index] = ResolveDealer(
							prepared.WholesalerExternalId,
							prepared.WholesalerName,
							prepared.StateId,
							prepared.SellerDistrictId,
							null,
							prepared.WholesalerNatureId,
							prepared.WholesalerMobileNo,
							prepared.RowNumber,
							"wholesaler");
					}
					else if (categoryId is "One" or "Two" or "Three" or "Five")
					{
						dealerResolutionByRow[prepared.Index] = ResolveDealer(
							prepared.DealerExternalId,
							prepared.DealerName,
							prepared.StateId,
							prepared.DistrictId,
							prepared.DealerTypeId,
							prepared.NatureId,
							prepared.DealerMobileNo,
							prepared.RowNumber,
							"dealer");
					}
				}

				// A single save assigns IDs to every newly discovered IFMS dealer and
				// persists safe mobile updates to existing dealers.
				if (_db.ChangeTracker.Entries<IfmsDealer>().Any(x =>
					x.State is EntityState.Added or EntityState.Modified))
				{
					await _db.SaveChangesAsync(cancellationToken);
				}

				// Existing rows are preloaded once for the current business scope. This avoids
				// one database query per Excel row and makes repeated uploads idempotent.
				var dptByKey = new Dictionary<string, DptReport>(StringComparer.Ordinal);
				var stockByKey = new Dictionary<string, WholesalerStockAsOnToday>(StringComparer.Ordinal);
				var salesReceiptByKey = new Dictionary<string, SalesAndReceipt>(StringComparer.Ordinal);
				var wholesalerSaleByKey = new Dictionary<string, SalesWholesaler>(StringComparer.Ordinal);
				var companySaleByKey = new Dictionary<string, SalesCompanySale>(StringComparer.Ordinal);
				var stateReconByKey = new Dictionary<string, StateGlobalStockReconciliation>(StringComparer.Ordinal);
				var warehouseReconByKey = new Dictionary<string, WarehouseDistrictGlobalStockReconciliation>(StringComparer.Ordinal);

				// Exact duplicate rows in the same file are skipped. Conflicting rows with the
				// same business key fail the file because row order must not decide the result.
				var uploadedBusinessKeys = new Dictionary<string, string>(StringComparer.Ordinal);

				if (categoryId == "One")
				{
					var start = reportDateUtc!.Value;
					var end = start.AddDays(1);
					var existingRows = await _db.DptReports
						.Where(x => x.CreatedAt >= start && x.CreatedAt < end)
						.ToListAsync(cancellationToken);
					BuildLatest(existingRows, DptKey, x => x.UpdatedAt, dptByKey, result);
				}
				else if (categoryId == "Three")
				{
					var start = reportDateUtc!.Value;
					var end = start.AddDays(1);
					var existingRows = await _db.SalesAndReceipts
						.Where(x => x.CreatedAt >= start && x.CreatedAt < end)
						.ToListAsync(cancellationToken);
					BuildLatest(existingRows, SalesReceiptKey, x => x.UpdatedAt, salesReceiptByKey, result);
				}
				else if (categoryId == "Five")
				{
					var parsedDates = records
						.Select((row, index) => ParseRequiredDate(row, "stockdate", index + 2))
						.Select(ToUtcDate)
						.ToList();

					var start = parsedDates.Min();
					var end = parsedDates.Max().AddDays(1);
					var existingRows = await _db.WholesalerStockAsOnTodays
						.Where(x => x.StockDate >= start && x.StockDate < end)
						.ToListAsync(cancellationToken);
					BuildLatest(existingRows, WholesalerStockKey, x => x.UpdatedAt, stockByKey, result);
				}
				else if (categoryId == "Two")
				{
					var transactionKeys = records
						.Select(row => NormalizeKey(GetCell(row, "transactionid")))
						.Where(x => x.Length > 0)
						.Distinct(StringComparer.Ordinal)
						.ToList();

					var existingRows = new List<SalesCompanySale>();
					foreach (var chunk in transactionKeys.Chunk(500))
					{
						var keys = chunk.ToList();
						existingRows.AddRange(await _db.SalesCompanySales
							.Where(x => x.TransactionId != null && keys.Contains(x.TransactionId.ToLower()))
							.ToListAsync(cancellationToken));
					}
					BuildLatest(existingRows, CompanySaleKey, x => x.UpdatedAt, companySaleByKey, result);
				}
				else if (categoryId == "Four")
				{
					var transactionKeys = records
						.Select(row => NormalizeKey(GetCell(row, "transactionid")))
						.Where(x => x.Length > 0)
						.Distinct(StringComparer.Ordinal)
						.ToList();

					var existingRows = new List<SalesWholesaler>();
					foreach (var chunk in transactionKeys.Chunk(500))
					{
						var keys = chunk.ToList();
						existingRows.AddRange(await _db.SalesWholesalers
							.Where(x => x.TransactionId != null && keys.Contains(x.TransactionId.ToLower()))
							.ToListAsync(cancellationToken));
					}
					BuildLatest(existingRows, WholesalerSaleKey, x => x.UpdatedAt, wholesalerSaleByKey, result);
				}
				else if (categoryId == "Six")
				{
					var start = reportDateUtc!.Value;
					var end = start.AddDays(1);
					var existingRows = await _db.StateGlobalStockReconciliations
						.Where(x => x.CreatedAt >= start && x.CreatedAt < end)
						.ToListAsync(cancellationToken);
					BuildLatest(existingRows, StateReconciliationKey, x => x.UpdatedAt, stateReconByKey, result);
				}
				else if (categoryId == "Seven")
				{
					var start = reportDateUtc!.Value;
					var end = start.AddDays(1);
					var existingRows = await _db.WarehouseDistrictGlobalStockReconciliations
						.Where(x => x.CreatedAt >= start && x.CreatedAt < end)
						.ToListAsync(cancellationToken);
					BuildLatest(existingRows, WarehouseReconciliationKey, x => x.UpdatedAt, warehouseReconByKey, result);
				}

				if (result.ExistingDuplicateRowsDetected > 0)
				{
					result.Warnings.Add(
						$"{result.ExistingDuplicateRowsDetected} existing duplicate row(s) were detected. " +
						"They were not deleted automatically. Review and clean them before relying on historical totals.");
				}

				var previousAutoDetectChanges = _db.ChangeTracker.AutoDetectChangesEnabled;
				_db.ChangeTracker.AutoDetectChangesEnabled = false;

				try
				{
					string lastStateStr = string.Empty;

					for (int i = 0; i < records.Count; i++)
					{
						cancellationToken.ThrowIfCancellationRequested();
						var row = records[i];
						int rowNumber = int.TryParse(
							GetCell(row, "__rownumber"),
							NumberStyles.Integer,
							CultureInfo.InvariantCulture,
							out var parsedRowNumber)
							? parsedRowNumber
							: i + 2;

						var stateStr = categoryId == "One" ? GetCell(row, "statename") : GetCell(row, "state");
						var districtStr = categoryId == "One" ? GetCell(row, "districtname") : GetCell(row, "district");
						var dealerIdStr = categoryId == "One" ? GetCell(row, "retailerid") : GetCell(row, "dealerid");
						var agencyNameStr = CleanDealerName(
							categoryId == "One"
								? GetCell(row, "retailername")
								: categoryId == "Two"
									? GetCell(row, "dealername")
									: GetCell(row, "agencyname"));
						var dealerTypeStr = categoryId == "One" ? "" : GetCell(row, "dealertype");
						var natureStr = categoryId == "Three" ? OptionalText(row, "dealernature") : GetCell(row, "dealershipnature");
						var companyStr = (categoryId == "Four" || categoryId == "Two") ? GetCell(row, "manufacturer") : GetCell(row, "company");
						var plantStr = GetCell(row, "plant");
						var productStr = (categoryId == "Four" || categoryId == "Two") ? GetCell(row, "companyproduct") : GetCell(row, "product");
						var dealerMobileNo = ParseOptionalMobile(
							row,
							rowNumber,
							"mobileno",
							"mobileno.",
							"mobilenumber",
							"mobile",
							"contactno",
							"contactnumber",
							"phoneno",
							"phonenumber");

						if (categoryId is "One" or "Two" or "Three" or "Five")
						{
							EnsureValidDealerName(agencyNameStr, rowNumber, "Dealer/Retailer/Agency");
						}

						if (categoryId == "Four")
						{
							EnsureValidDealerName(
								CleanDealerName(GetCell(row, "wholesaleragencyname")),
								rowNumber,
								"Wholesaler agency");
						}

						if (categoryId is "One" or "Two" or "Three" or "Four" or "Five")
						{
							if (string.IsNullOrWhiteSpace(stateStr))
								throw new InvalidDataException($"Row {rowNumber}: State is required.");
							if (string.IsNullOrWhiteSpace(productStr))
								throw new InvalidDataException($"Row {rowNumber}: Product is required.");
						}

						if (categoryId == "Seven")
						{
							if (IsWarehouseTotalLabel(stateStr))
							{
								result.RowsSkipped++;
								continue;
							}

							if (string.IsNullOrWhiteSpace(stateStr) && !string.IsNullOrWhiteSpace(districtStr))
							{
								stateStr = lastStateStr;
							}
							else if (!string.IsNullOrWhiteSpace(stateStr) &&
								!stateStr.Equals("plant", StringComparison.OrdinalIgnoreCase))
							{
								lastStateStr = stateStr;
							}
						}

						if (string.IsNullOrEmpty(stateStr) && string.IsNullOrEmpty(districtStr) && string.IsNullOrEmpty(dealerIdStr) && string.IsNullOrEmpty(agencyNameStr) && string.IsNullOrEmpty(GetCell(row, "warehouse/location")))
						{
							result.RowsSkipped++;
							continue;
						}
						if (categoryId == "Six" && string.Equals(stateStr, "total", StringComparison.OrdinalIgnoreCase))
						{
							result.RowsSkipped++;
							continue;
						}

						int? stateId = null;
						if ((categoryId != "Six" && categoryId != "Seven") || !stateStr.Trim().Equals("plant", StringComparison.OrdinalIgnoreCase))
						{
							if (!string.IsNullOrEmpty(stateStr))
							{
								var key = stateStr.ToLowerInvariant();
								if (!stateDict.TryGetValue(key, out var id))
								{
									var newState = new State { StateName = stateStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId, ZoneId = 1 };
									_db.States.Add(newState);
									await _db.SaveChangesAsync(cancellationToken);
									id = newState.Id;
									stateDict[key] = id;
									result.NewMastersCreated.States++;
								}
								stateId = id;
							}
						}

						int? districtId = null;
						if (!string.IsNullOrEmpty(districtStr) && stateId.HasValue)
						{
							var key = $"{districtStr.ToLowerInvariant()}_{stateId.Value}";
							if (!districtDict.TryGetValue(key, out var id))
							{
								var newDistrict = new District { DistrictName = districtStr, StateId = stateId.Value, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
								_db.Districts.Add(newDistrict);
								await _db.SaveChangesAsync(cancellationToken);
								id = newDistrict.Id;
								districtDict[key] = id;
								result.NewMastersCreated.Districts++;
							}
							districtId = id;
						}

						int? subDistrictId = null;
						if (categoryId == "One")
						{
							var subDistrictStr = GetCell(row, "subdistrict");
							if (!string.IsNullOrEmpty(subDistrictStr) && districtId.HasValue)
							{
								var key = $"{subDistrictStr.ToLowerInvariant()}_{districtId.Value}";
								if (!subDistrictDict.TryGetValue(key, out var id))
								{
									var newSubDistrict = new SubDistrict { SubDistrictName = subDistrictStr, DistrictId = districtId.Value, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.SubDistricts.Add(newSubDistrict);
									await _db.SaveChangesAsync(cancellationToken);
									id = newSubDistrict.Id;
									subDistrictDict[key] = id;
									result.NewMastersCreated.SubDistricts++;
								}
								subDistrictId = id;
							}
						}

						int? dealerTypeId = null;
						if (!string.IsNullOrEmpty(dealerTypeStr))
						{
							var key = dealerTypeStr.ToLowerInvariant();
							if (!dealerTypeDict.TryGetValue(key, out var id))
							{
								var newType = new DealerType { Name = dealerTypeStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
								_db.DealerTypes.Add(newType);
								await _db.SaveChangesAsync(cancellationToken);
								id = newType.Id;
								dealerTypeDict[key] = id;
								result.NewMastersCreated.DealerTypes++;
							}
							dealerTypeId = id;
						}

						int? natureId = null;
						if (!string.IsNullOrEmpty(natureStr))
						{
							var key = natureStr.ToLowerInvariant();
							if (!natureDict.TryGetValue(key, out var id))
							{
								var newNat = new DealershipNature { Name = natureStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
								_db.DealershipNatures.Add(newNat);
								await _db.SaveChangesAsync(cancellationToken);
								id = newNat.Id;
								natureDict[key] = id;
								result.NewMastersCreated.DealershipNatures++;
							}
							natureId = id;
						}

						int? dealerRegistrationId = null;
						int? ifmsDealerId = null;

						// Dealers were resolved in the preload pass so no row-level database save is required.
						if (categoryId != "Four" && !string.IsNullOrWhiteSpace(agencyNameStr) &&
							dealerResolutionByRow.TryGetValue(i, out var dealerResolution))
						{
							dealerRegistrationId = dealerResolution.RegistrationId;
							ifmsDealerId = dealerResolution.IfmsDealer?.Id;
						}

						int? companyId = null;
						if (!string.IsNullOrEmpty(companyStr))
						{
							var key = companyStr.ToLowerInvariant();
							if (!companyDict.TryGetValue(key, out var id))
							{
								var newComp = new Company { Name = companyStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
								_db.Companies.Add(newComp);
								await _db.SaveChangesAsync(cancellationToken);
								id = newComp.Id;
								companyDict[key] = id;
								result.NewMastersCreated.Companies++;
							}
							companyId = id;
						}

						int? plantId = null;
						if (!string.IsNullOrEmpty(plantStr))
						{
							var key = plantStr.ToLowerInvariant();
							if (!plantDict.TryGetValue(key, out var id))
							{
								var newPlant = new Plant { Name = plantStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
								_db.Plants.Add(newPlant);
								await _db.SaveChangesAsync(cancellationToken);
								id = newPlant.Id;
								plantDict[key] = id;
								result.NewMastersCreated.Plants++;
							}
							plantId = id;
						}

						var preparedProduct = preparedMasterRows[i];
						int? productId = preparedProduct.ProductId;
						int? ifmsProductId = preparedProduct.IfmsProductId;

						if (!productId.HasValue && !ifmsProductId.HasValue)
							throw new InvalidOperationException(
								$"Row {rowNumber}: Product '{productStr}' was not resolved.");

						if (categoryId == "One")
						{
							var mobileNoStr = dealerMobileNo;
							var openBal = ParseDecimal(row, "openingbalance", rowNumber);
							var recvQty = ParseDecimal(row, "receivedquantity", rowNumber);
							var soldQty = ParseDecimal(row, "soldquantity", rowNumber);
							var avail = ParseDecimal(row, "availabilty", rowNumber);
							var closeBal = ParseDecimal(row, "closingbalance", rowNumber);

							var dptBusinessKey = DptKey(
								dealerRegistrationId, ifmsDealerId, agencyNameStr,
								productId, ifmsProductId, productStr, stateId, districtId, companyId, plantId,
								reportDateUtc!.Value);
							if (!ShouldProcessUploadedKey(uploadedBusinessKeys, dptBusinessKey, RowFingerprint(row), rowNumber, result))
								continue;

							if (!dptByKey.TryGetValue(dptBusinessKey, out var dptRecord))
							{
								dptRecord = new DptReport
								{
									CreatedAt = reportDateUtc.Value
								};
								_db.DptReports.Add(dptRecord);
								dptByKey[dptBusinessKey] = dptRecord;
								result.RowsInserted++;
							}
							else
							{
								result.RowsUpdated++;
							}

							dptRecord.StateId = stateId;
							dptRecord.DistrictId = districtId;
							dptRecord.SubDistrictId = subDistrictId;
							dptRecord.RetailerName = agencyNameStr;
							dptRecord.DealerRegistrationId = dealerRegistrationId;
							dptRecord.IfmsDealerId = ifmsDealerId;
							if (!string.IsNullOrWhiteSpace(mobileNoStr))
								dptRecord.MobileNo = mobileNoStr;
							dptRecord.DealershipNatureId = natureId;
							dptRecord.CompanyId = companyId;
							dptRecord.PlantId = plantId;
							dptRecord.ProductId = productId;
							dptRecord.IfmsProductId = ifmsProductId;
							dptRecord.OpeningBalance = openBal;
							dptRecord.ReceivedQuantity = recvQty;
							dptRecord.SoldQuantity = soldQty;
							dptRecord.Availability = avail;
							dptRecord.ClosingBalance = closeBal;
							dptRecord.UpdatedAt = now;
							dptRecord.UpdatedBy = currentUserId;
						}
						else if (categoryId == "Five")
						{
							var stockValue = ParseDecimal(row, "stock", rowNumber);
							var stockDate = ToUtcDate(ParseRequiredDate(row, "stockdate", rowNumber));
							var stockBusinessKey = WholesalerStockKey(
								dealerRegistrationId, ifmsDealerId, agencyNameStr,
								productId, ifmsProductId, productStr, stateId, districtId, companyId, plantId, stockDate);
							if (!ShouldProcessUploadedKey(uploadedBusinessKeys, stockBusinessKey, RowFingerprint(row), rowNumber, result))
								continue;

							if (!stockByKey.TryGetValue(stockBusinessKey, out var stockRecord))
							{
								stockRecord = new WholesalerStockAsOnToday
								{
									CreatedAt = now
								};
								_db.WholesalerStockAsOnTodays.Add(stockRecord);
								stockByKey[stockBusinessKey] = stockRecord;
								result.RowsInserted++;
							}
							else
							{
								result.RowsUpdated++;
							}

							stockRecord.StateId = stateId;
							stockRecord.DistrictId = districtId;
							stockRecord.DealerRegistrationId = dealerRegistrationId;
							stockRecord.IfmsDealerId = ifmsDealerId;
							stockRecord.AgencyName = agencyNameStr;
							stockRecord.DealerTypeId = dealerTypeId;
							stockRecord.DealershipNatureId = natureId;
							stockRecord.CompanyId = companyId;
							stockRecord.PlantId = plantId;
							stockRecord.ProductId = productId;
							stockRecord.IfmsProductId = ifmsProductId;
							stockRecord.Stock = stockValue;
							stockRecord.StockDate = stockDate;
							stockRecord.UpdatedAt = now;
							stockRecord.UpdatedBy = currentUserId;
						}
						else if (categoryId == "Three")
						{
							var openingBalance = ParseDecimal(row, "wholesalerob", rowNumber);
							var compWsSale = ParseDecimal(row, "comp-wssale", rowNumber);
							var compWsSaleRcpt = ParseDecimal(row, "comp-wssalercpt", rowNumber);
							var receivedFromWs = ParseDecimal(row, "receivedfromws", rowNumber);
							var receivedFromWsAck = ParseDecimal(row, "receivedfromwsack", rowNumber);
							var wsRtSale = ParseDecimal(row, "ws-rtsale", rowNumber);
							var wsRtSaleRcpt = ParseDecimal(row, "ws-rtsalercpt", rowNumber);
							var wsWsSale = ParseDecimal(row, "ws-wssale", rowNumber);
							var wsWsSaleRcpt = ParseDecimal(row, "ws-wssalercpt", rowNumber);
							var totalSalesByWs = ParseDecimal(row, "totalsalesbyws", rowNumber);
							var stockTransferWsToRetailer = ParseDecimal(row, "stocktransferfromwstoretailer", rowNumber);
							var stockTransferWsToRetailerAck = ParseDecimal(row, "stocktransferfromwstoretailerack", rowNumber);
							var balanceWithWs = ParseDecimal(row, "balancewithws", rowNumber);
							var totalAckToWs = ParseDecimal(row, "totalacktows", rowNumber);

							var salesReceiptBusinessKey = SalesReceiptKey(
								dealerRegistrationId, ifmsDealerId, agencyNameStr,
								productId, ifmsProductId, productStr, stateId, districtId, companyId, plantId,
								reportDateUtc!.Value);
							if (!ShouldProcessUploadedKey(uploadedBusinessKeys, salesReceiptBusinessKey, RowFingerprint(row), rowNumber, result))
								continue;

							if (!salesReceiptByKey.TryGetValue(salesReceiptBusinessKey, out var srRecord))
							{
								srRecord = new SalesAndReceipt { CreatedAt = reportDateUtc.Value };
								_db.SalesAndReceipts.Add(srRecord);
								salesReceiptByKey[salesReceiptBusinessKey] = srRecord;
								result.RowsInserted++;
							}
							else
							{
								result.RowsUpdated++;
							}

							srRecord.CompanyId = companyId;
							srRecord.PlantId = plantId;
							srRecord.ProductId = productId;
							srRecord.IfmsProductId = ifmsProductId;
							srRecord.StateId = stateId;
							srRecord.DistrictId = districtId;
							srRecord.DealershipNatureId = natureId;
							srRecord.AgencyName = agencyNameStr;
							srRecord.DealerRegistrationId = dealerRegistrationId;
							srRecord.IfmsDealerId = ifmsDealerId;
							srRecord.OpeningBalance = openingBalance;
							srRecord.CompWsSale = compWsSale;
							srRecord.CompWsSaleRcpt = compWsSaleRcpt;
							srRecord.ReceivedFromWs = receivedFromWs;
							srRecord.ReceivedFromWsAck = receivedFromWsAck;
							srRecord.WsRtSale = wsRtSale;
							srRecord.WsRtSaleRcpt = wsRtSaleRcpt;
							srRecord.WsWsSale = wsWsSale;
							srRecord.WsWsSaleRcpt = wsWsSaleRcpt;
							srRecord.TotalSalesByWs = totalSalesByWs;
							srRecord.StockTransferWsToRetailer = stockTransferWsToRetailer;
							srRecord.StockTransferWsToRetailerAck = stockTransferWsToRetailerAck;
							srRecord.BalanceWithWs = balanceWithWs;
							srRecord.TotalAckToWs = totalAckToWs;
							srRecord.UpdatedAt = now;
							srRecord.UpdatedBy = currentUserId;
						}
						else if (categoryId == "Four")
						{
							var marketerStr = OptionalText(row, "marketer");
							var ackThroughStr = OptionalText(row, "ackthrough");
							var wholesalerNatureStr = OptionalText(row, "wholesalernature");
							var dealerNatureStr = OptionalText(row, "dealernature");
							var unitStr = OptionalText(row, "unit");
							var statusStr = OptionalText(row, "status");
							var txnTypeStr = OptionalText(row, "txntype");

							var wholesalerExternalId = GetCell(row, "wholesalerid");
							var wholesalerAgencyStr = CleanDealerName(GetCell(row, "wholesaleragencyname"));
							var sellerDistrictStr = GetCell(row, "sellerdistrict");
							var buyerDistrictStr = GetCell(row, "buyerdistrict");
							var wholesalerMobileNo = ParseOptionalMobile(
								row,
								rowNumber,
								"wholesalermobileno",
								"wholesalermobileno.",
								"wholesalermobilenumber",
								"wholesalercontactno",
								"wholesalercontactnumber",
								"wholesalerphoneno");

							if (string.IsNullOrWhiteSpace(wholesalerMobileNo) &&
								string.Equals(wholesalerAgencyStr, agencyNameStr, StringComparison.OrdinalIgnoreCase))
							{
								wholesalerMobileNo = dealerMobileNo;
							}

							int? marketerId = null;
							if (!string.IsNullOrEmpty(marketerStr))
							{
								var key = marketerStr.ToLowerInvariant();
								if (!companyDict.TryGetValue(key, out var id))
								{
									var newComp = new Company { Name = marketerStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Companies.Add(newComp);
									await _db.SaveChangesAsync(cancellationToken);
									id = newComp.Id;
									companyDict[key] = id;
									result.NewMastersCreated.Companies++;
								}
								marketerId = id;
							}

							int? ackThroughId = null;
							if (!string.IsNullOrEmpty(ackThroughStr))
							{
								var key = ackThroughStr.ToLowerInvariant();
								if (!ackThroughDict.TryGetValue(key, out var id))
								{
									var newAck = new AckThrough { Name = ackThroughStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.AckThroughs.Add(newAck);
									await _db.SaveChangesAsync(cancellationToken);
									id = newAck.Id;
									ackThroughDict[key] = id;
									result.NewMastersCreated.AckThroughs++;
								}
								ackThroughId = id;
							}

							int? txnTypeId = null;
							if (!string.IsNullOrEmpty(txnTypeStr))
							{
								var key = txnTypeStr.ToLowerInvariant();
								if (!txnTypeDict.TryGetValue(key, out var id))
								{
									var newT = new TxnType { Name = txnTypeStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.TxnTypes.Add(newT);
									await _db.SaveChangesAsync(cancellationToken);
									id = newT.Id;
									txnTypeDict[key] = id;
									result.NewMastersCreated.TxnTypes++;
								}
								txnTypeId = id;
							}

							int? unitId = null;
							if (!string.IsNullOrEmpty(unitStr))
							{
								var key = unitStr.ToLowerInvariant();
								if (!unitDict.TryGetValue(key, out var id))
								{
									var newU = new Unit { Name = unitStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Units.Add(newU);
									await _db.SaveChangesAsync(cancellationToken);
									id = newU.Id;
									unitDict[key] = id;
									result.NewMastersCreated.Units++;
								}
								unitId = id;
							}

							int? statusId = null;
							if (!string.IsNullOrEmpty(statusStr))
							{
								var key = statusStr.ToLowerInvariant();
								if (!statusDict.TryGetValue(key, out var id))
								{
									var newS = new Status { Name = statusStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Statuses.Add(newS);
									await _db.SaveChangesAsync(cancellationToken);
									id = newS.Id;
									statusDict[key] = id;
									result.NewMastersCreated.Statuses++;
								}
								statusId = id;
							}

							int? wholesalerNatureId = null;
							if (!string.IsNullOrEmpty(wholesalerNatureStr))
							{
								var key = wholesalerNatureStr.ToLowerInvariant();
								if (!natureDict.TryGetValue(key, out var id))
								{
									var newN = new DealershipNature { Name = wholesalerNatureStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.DealershipNatures.Add(newN);
									await _db.SaveChangesAsync(cancellationToken);
									id = newN.Id;
									natureDict[key] = id;
									result.NewMastersCreated.DealershipNatures++;
								}
								wholesalerNatureId = id;
							}

							int? dealerNatureId = null;
							if (!string.IsNullOrEmpty(dealerNatureStr))
							{
								var key = dealerNatureStr.ToLowerInvariant();
								if (!natureDict.TryGetValue(key, out var id))
								{
									var newN = new DealershipNature { Name = dealerNatureStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.DealershipNatures.Add(newN);
									await _db.SaveChangesAsync(cancellationToken);
									id = newN.Id;
									natureDict[key] = id;
									result.NewMastersCreated.DealershipNatures++;
								}
								dealerNatureId = id;
							}

							int? sellerDistrictId = null;
							if (!string.IsNullOrEmpty(sellerDistrictStr) && stateId.HasValue)
							{
								var key = $"{sellerDistrictStr.ToLowerInvariant()}_{stateId.Value}";
								if (!districtDict.TryGetValue(key, out var id))
								{
									var newDistrict = new District { DistrictName = sellerDistrictStr, StateId = stateId.Value, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Districts.Add(newDistrict);
									await _db.SaveChangesAsync(cancellationToken);
									id = newDistrict.Id;
									districtDict[key] = id;
									result.NewMastersCreated.Districts++;
								}
								sellerDistrictId = id;
							}

							int? buyerDistrictId = null;
							if (!string.IsNullOrEmpty(buyerDistrictStr) && stateId.HasValue)
							{
								var key = $"{buyerDistrictStr.ToLowerInvariant()}_{stateId.Value}";
								if (!districtDict.TryGetValue(key, out var id))
								{
									var newDistrict = new District { DistrictName = buyerDistrictStr, StateId = stateId.Value, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Districts.Add(newDistrict);
									await _db.SaveChangesAsync(cancellationToken);
									id = newDistrict.Id;
									districtDict[key] = id;
									result.NewMastersCreated.Districts++;
								}
								buyerDistrictId = id;
							}

							if (!buyerResolutionByRow.TryGetValue(i, out var buyerResolution))
								throw new InvalidOperationException($"Row {rowNumber}: Buyer dealer was not pre-resolved.");
							dealerRegistrationId = buyerResolution.RegistrationId;
							ifmsDealerId = buyerResolution.IfmsDealer?.Id;

							if (!wholesalerResolutionByRow.TryGetValue(i, out var wholesalerResolution))
								throw new InvalidOperationException($"Row {rowNumber}: Wholesaler was not pre-resolved.");
							var wholesalerRegistrationId = wholesalerResolution.RegistrationId;
							var ifmsWholesalerId = wholesalerResolution.IfmsDealer?.Id;

							var transactionId = GetCell(row, "transactionid");
							var invoiceNo = GetCell(row, "invoiceno");
							if (string.IsNullOrWhiteSpace(transactionId))
								throw new InvalidDataException($"Row {rowNumber}: transactionid is required.");
							if (string.IsNullOrWhiteSpace(invoiceNo))
								throw new InvalidDataException($"Row {rowNumber}: invoiceno is required.");

							var invDate = ParseOptionalDate(row, "invoicedate", rowNumber);
							var entDate = ParseOptionalDate(row, "entrydate", rowNumber);
							var lckDate = ParseOptionalDate(row, "lockdate", rowNumber);
							var rrDate = ParseOptionalDate(row, "retailerreceiptdate", rowNumber);
							var qty = ParseDecimal(row, "quantity", rowNumber);
							var qtymt = ParseDecimal(row, "quantity(mt)", rowNumber);
							var recvQtymt = ParseDecimal(row, "receivedquantity(mt)", rowNumber);
							var m1qty = ParseDecimal(row, "month1qty", rowNumber);
							var m2qty = ParseDecimal(row, "month2qty", rowNumber);
							var lorryCap = ParseDecimal(row, "lorrycapacity", rowNumber);

							var wholesalerSaleBusinessKey = WholesalerSaleKey(
								transactionId,
								invoiceNo,
								productId, ifmsProductId, productStr);
							if (!ShouldProcessUploadedKey(uploadedBusinessKeys, wholesalerSaleBusinessKey, RowFingerprint(row), rowNumber, result))
								continue;

							if (!wholesalerSaleByKey.TryGetValue(wholesalerSaleBusinessKey, out var salesWholesaler))
							{
								salesWholesaler = new SalesWholesaler { CreatedAt = now };
								_db.SalesWholesalers.Add(salesWholesaler);
								wholesalerSaleByKey[wholesalerSaleBusinessKey] = salesWholesaler;
								result.RowsInserted++;
							}
							else
							{
								result.RowsUpdated++;
							}

							salesWholesaler.TransactionId = transactionId;
							salesWholesaler.InvoiceNo = invoiceNo;
							salesWholesaler.InvoiceDate = invDate;
							salesWholesaler.MarketerId = marketerId;
							salesWholesaler.ManufacturerId = companyId;
							salesWholesaler.PlantId = plantId;
							salesWholesaler.WholesalerId = wholesalerRegistrationId;
							salesWholesaler.IfmsWholesalerId = ifmsWholesalerId;
							salesWholesaler.WholesalerAgencyName = wholesalerAgencyStr;
							salesWholesaler.WholesalerNatureId = wholesalerNatureId;
							salesWholesaler.StateId = stateId;
							salesWholesaler.SellerDistrictId = sellerDistrictId;
							salesWholesaler.BuyerDistrictId = buyerDistrictId;
							salesWholesaler.DealerId = dealerRegistrationId;
							salesWholesaler.DealerTypeId = dealerTypeId;
							salesWholesaler.IfmsDealerId = ifmsDealerId;
							salesWholesaler.AgencyName = agencyNameStr;
							salesWholesaler.DealerNatureId = dealerNatureId;
							if (!string.IsNullOrWhiteSpace(dealerMobileNo))
								salesWholesaler.MobileNo = dealerMobileNo;
							salesWholesaler.ProductId = productId;
							salesWholesaler.IfmsProductId = ifmsProductId;
							salesWholesaler.UnitId = unitId;
							salesWholesaler.Quantity = qty;
							salesWholesaler.QuantityMT = qtymt;
							salesWholesaler.ReceivedQuantityMT = recvQtymt;
							salesWholesaler.StatusId = statusId;
							salesWholesaler.TxnTypeId = txnTypeId;
							salesWholesaler.EntryDate = entDate;
							salesWholesaler.LockDate = lckDate;
							salesWholesaler.AckThroughId = ackThroughId;
							salesWholesaler.TxnRemark = GetCell(row, "txnremark");
							salesWholesaler.SubsidyMonth1 = GetCell(row, "subsidymonth1");
							salesWholesaler.SubsidyYear1 = GetCell(row, "subsidyyear1");
							salesWholesaler.Month1Qty = m1qty;
							salesWholesaler.SubsidyMonth2 = GetCell(row, "subsidymonth2");
							salesWholesaler.SubsidyYear2 = GetCell(row, "subsidyyear2");
							salesWholesaler.Month2Qty = m2qty;
							salesWholesaler.ChallanNo = GetCell(row, "challanno");
							salesWholesaler.LorryNo = GetCell(row, "lorryno");
							salesWholesaler.LorryCapacity = lorryCap;
							salesWholesaler.DispatchNo = GetCell(row, "dispatchno");
							salesWholesaler.RetailerReceiptDate = rrDate;
							salesWholesaler.UpdatedAt = now;
							salesWholesaler.UpdatedBy = currentUserId;
						}
						else if (categoryId == "Two")
						{
							var marketerStr = OptionalText(row, "marketer");
							var ackThroughStr = OptionalText(row, "ackthrough");
							var unitStr = OptionalText(row, "unit");
							var statusStr = OptionalText(row, "status");
							var txnTypeStr = OptionalText(row, "txntype");

							int? marketerId = null;
							if (!string.IsNullOrEmpty(marketerStr))
							{
								var key = marketerStr.ToLowerInvariant();
								if (!companyDict.TryGetValue(key, out var id))
								{
									var newComp = new Company { Name = marketerStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Companies.Add(newComp);
									await _db.SaveChangesAsync(cancellationToken);
									id = newComp.Id;
									companyDict[key] = id;
									result.NewMastersCreated.Companies++;
								}
								marketerId = id;
							}

							int? ackThroughId = null;
							if (!string.IsNullOrEmpty(ackThroughStr))
							{
								var key = ackThroughStr.ToLowerInvariant();
								if (!ackThroughDict.TryGetValue(key, out var id))
								{
									var newAck = new AckThrough { Name = ackThroughStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.AckThroughs.Add(newAck);
									await _db.SaveChangesAsync(cancellationToken);
									id = newAck.Id;
									ackThroughDict[key] = id;
									result.NewMastersCreated.AckThroughs++;
								}
								ackThroughId = id;
							}

							int? unitId = null;
							if (!string.IsNullOrEmpty(unitStr))
							{
								var key = unitStr.ToLowerInvariant();
								if (!unitDict.TryGetValue(key, out var id))
								{
									var newU = new Unit { Name = unitStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Units.Add(newU);
									await _db.SaveChangesAsync(cancellationToken);
									id = newU.Id;
									unitDict[key] = id;
									result.NewMastersCreated.Units++;
								}
								unitId = id;
							}

							int? statusId = null;
							if (!string.IsNullOrEmpty(statusStr))
							{
								var key = statusStr.ToLowerInvariant();
								if (!statusDict.TryGetValue(key, out var id))
								{
									var newS = new Status { Name = statusStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Statuses.Add(newS);
									await _db.SaveChangesAsync(cancellationToken);
									id = newS.Id;
									statusDict[key] = id;
									result.NewMastersCreated.Statuses++;
								}
								statusId = id;
							}

							var transactionId = GetCell(row, "transactionid");
							var invoiceNo = GetCell(row, "invoiceno");
							if (string.IsNullOrWhiteSpace(transactionId))
								throw new InvalidDataException($"Row {rowNumber}: transactionid is required.");
							if (string.IsNullOrWhiteSpace(invoiceNo))
								throw new InvalidDataException($"Row {rowNumber}: invoiceno is required.");

							var invDate = ParseOptionalDate(row, "invoicedate", rowNumber);
							var entDate = ParseOptionalDate(row, "entrydate", rowNumber);
							var lckDate = ParseOptionalDate(row, "lockdate", rowNumber);
							var rrDate = ParseOptionalDate(row, "retailerreceiptdate", rowNumber);
							var qty = ParseDecimal(row, "quantity", rowNumber);
							var qtymt = ParseDecimal(row, "quantity(mt)", rowNumber);
							var recvQty = ParseDecimal(row, "receivedquantity", rowNumber);
							var m1qty = ParseDecimal(row, "month1qty", rowNumber);
							var m2qty = ParseDecimal(row, "month2qty", rowNumber);
							var lorryCap = ParseDecimal(row, "lorrycapacity", rowNumber);

							var companySaleBusinessKey = CompanySaleKey(
								transactionId,
								invoiceNo,
								productId, ifmsProductId, productStr);
							if (!ShouldProcessUploadedKey(uploadedBusinessKeys, companySaleBusinessKey, RowFingerprint(row), rowNumber, result))
								continue;

							if (!companySaleByKey.TryGetValue(companySaleBusinessKey, out var salesCompany))
							{
								salesCompany = new SalesCompanySale { CreatedAt = now };
								_db.SalesCompanySales.Add(salesCompany);
								companySaleByKey[companySaleBusinessKey] = salesCompany;
								result.RowsInserted++;
							}
							else
							{
								result.RowsUpdated++;
							}

							salesCompany.TransactionId = transactionId;
							salesCompany.InvoiceNo = invoiceNo;
							salesCompany.InvoiceDate = invDate;
							salesCompany.MarketerId = marketerId;
							salesCompany.ManufacturerId = companyId;
							salesCompany.PlantId = plantId;
							salesCompany.DealerName = agencyNameStr;
							salesCompany.DealerTypeId = dealerTypeId;
							salesCompany.DealershipNatureId = natureId;
							if (!string.IsNullOrWhiteSpace(dealerMobileNo))
								salesCompany.MobileNo = dealerMobileNo;
							salesCompany.DealerRegistrationId = dealerRegistrationId;
							salesCompany.IfmsDealerId = ifmsDealerId;
							salesCompany.StateId = stateId;
							salesCompany.DistrictId = districtId;
							salesCompany.ProductId = productId;
							salesCompany.IfmsProductId = ifmsProductId;
							salesCompany.UnitId = unitId;
							salesCompany.Quantity = qty;
							salesCompany.QuantityMT = qtymt;
							salesCompany.ReceivedQuantity = recvQty;
							salesCompany.StatusId = statusId;
							salesCompany.EntryDate = entDate;
							salesCompany.LockDate = lckDate;
							salesCompany.AckThroughId = ackThroughId;
							salesCompany.TxnRemark = GetCell(row, "txnremark");
							salesCompany.SubsidyMonth1 = GetCell(row, "subsidymonth1");
							salesCompany.SubsidyYear1 = GetCell(row, "subsidyyear1");
							salesCompany.Month1Qty = m1qty;
							salesCompany.SubsidyMonth2 = GetCell(row, "subsidymonth2");
							salesCompany.SubsidyYear2 = GetCell(row, "subsidyyear2");
							salesCompany.Month2Qty = m2qty;
							salesCompany.ChallanNo = GetCell(row, "challanno.");
							salesCompany.DdNo = GetCell(row, "ddno.");
							salesCompany.LorryNo = GetCell(row, "lorryno.");
							salesCompany.LorryCapacity = lorryCap;
							salesCompany.RetailerReceiptDate = rrDate;
							salesCompany.UpdatedAt = now;
							salesCompany.UpdatedBy = currentUserId;
						}
						else if (categoryId == "Six")
						{
							if (!string.IsNullOrEmpty(globalPlantStr))
							{
								var key = globalPlantStr.ToLowerInvariant();
								if (!plantDict.TryGetValue(key, out var id))
								{
									var newPlant = new Plant { Name = globalPlantStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Plants.Add(newPlant);
									await _db.SaveChangesAsync(cancellationToken);
									id = newPlant.Id;
									plantDict[key] = id;
									result.NewMastersCreated.Plants++;
								}
								plantId = id;
							}


							if (string.Equals(stateStr, "plant", StringComparison.OrdinalIgnoreCase))
							{
								// Keep stateId as null for "PLANT" row
							}
							else if (!string.IsNullOrEmpty(stateStr))
							{
								var stateKey = stateStr.ToLowerInvariant();
								if (!stateDict.TryGetValue(stateKey, out var id))
								{
									var newState = new State { StateName = stateStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.States.Add(newState);
									await _db.SaveChangesAsync(cancellationToken);
									id = newState.Id;
									stateDict[stateKey] = id;
									result.NewMastersCreated.States++;
								}
								stateId = id;
							}

							var openingStock = ParseDecimal(row, "openingstock", rowNumber);
							var openingGit = ParseDecimal(row, "openinggit", rowNumber);
							var production = ParseDecimal(row, "production/imports", rowNumber);
							var receipt = ParseDecimal(row, "receipt", rowNumber);
							var dispatches = ParseDecimal(row, "dispatches", rowNumber);
							var sales = ParseDecimal(row, "sales", rowNumber);
							var salesReturn = ParseDecimal(row, "salesreturn", rowNumber);
							var stockAdj = ParseDecimal(row, "stockadjustment", rowNumber);
							var closingGit = ParseDecimal(row, "closinggit", rowNumber);
							var closingStock = ParseDecimal(row, "closingstock", rowNumber);

							var stateReconciliationBusinessKey = StateReconciliationKey(stateId, plantId, productId, ifmsProductId, productStr, reportDateUtc!.Value);
							if (!ShouldProcessUploadedKey(uploadedBusinessKeys, stateReconciliationBusinessKey, RowFingerprint(row), rowNumber, result))
								continue;
							if (!stateReconByKey.TryGetValue(stateReconciliationBusinessKey, out var recon))
							{
								recon = new StateGlobalStockReconciliation { CreatedAt = reportDateUtc.Value };
								_db.StateGlobalStockReconciliations.Add(recon);
								stateReconByKey[stateReconciliationBusinessKey] = recon;
								result.RowsInserted++;
							}
							else
							{
								result.RowsUpdated++;
							}

							recon.PlantId = plantId;
							recon.ProductId = productId;
							recon.IfmsProductId = ifmsProductId;
							recon.StateId = stateId;
							recon.OpeningStock = openingStock;
							recon.OpeningGIT = openingGit;
							recon.ProductionImports = production;
							recon.Receipt = receipt;
							recon.Dispatches = dispatches;
							recon.Sales = sales;
							recon.SalesReturn = salesReturn;
							recon.StockAdjustment = stockAdj;
							recon.ClosingGIT = closingGit;
							recon.ClosingStock = closingStock;
							recon.UpdatedAt = now;
							recon.UpdatedBy = currentUserId;
						}
						else if (categoryId == "Seven")
						{
							if (!string.IsNullOrEmpty(globalPlantStr))
							{
								var key = globalPlantStr.ToLowerInvariant();
								if (!plantDict.TryGetValue(key, out var id))
								{
									var newPlant = new Plant { Name = globalPlantStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Plants.Add(newPlant);
									await _db.SaveChangesAsync(cancellationToken);
									id = newPlant.Id;
									plantDict[key] = id;
									result.NewMastersCreated.Plants++;
								}
								plantId = id;
							}


							if (stateStr.Trim().Equals("plant", StringComparison.OrdinalIgnoreCase))
							{
								stateId = null;
							}
							else if (!string.IsNullOrEmpty(stateStr))
							{
								var stateKey = stateStr.ToLowerInvariant();
								if (!stateDict.TryGetValue(stateKey, out var id))
								{
									var newState = new State { StateName = stateStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.States.Add(newState);
									await _db.SaveChangesAsync(cancellationToken);
									id = newState.Id;
									stateDict[stateKey] = id;
									result.NewMastersCreated.States++;
								}
								stateId = id;
							}

							int? distId = null;
							if (!string.IsNullOrEmpty(districtStr))
							{
								if (!stateId.HasValue)
									throw new InvalidDataException(
										$"Row {rowNumber}: District '{districtStr}' cannot be saved without a State.");

								var distKey = $"{districtStr.ToLowerInvariant()}_{stateId.Value}";
								if (!districtDict.TryGetValue(distKey, out var id))
								{
									var newDist = new District { DistrictName = districtStr, StateId = stateId.Value, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Districts.Add(newDist);
									await _db.SaveChangesAsync(cancellationToken);
									id = newDist.Id;
									districtDict[distKey] = id;
									result.NewMastersCreated.Districts++;
								}
								distId = id;
							}

							var warehouseStr = GetCell(row, "warehouse/location");
							int? whseId = null;
							if (!string.IsNullOrEmpty(warehouseStr))
							{
								var whKey = warehouseStr.ToLowerInvariant();
								if (!warehouseDict.TryGetValue(whKey, out var id))
								{
									var newWhse = new Warehouse { Name = warehouseStr, WarehouseCode = string.Empty, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
									_db.Warehouses.Add(newWhse);
									await _db.SaveChangesAsync(cancellationToken);
									id = newWhse.Id;
									warehouseDict[whKey] = id;
									result.NewMastersCreated.Warehouses++;
								}
								whseId = id;
							}

							var openingStockLoc = ParseDecimal(row, "openingstock(atlocation)", rowNumber);
							var openingGit = ParseDecimal(row, "openingstock(git)", rowNumber);
							var production = ParseDecimal(row, "imports/production", rowNumber);
							var receipt = ParseDecimal(row, "receipt", rowNumber);
							var dispatches = ParseDecimal(row, "dispatches", rowNumber);
							var sales = ParseDecimal(row, "sales", rowNumber);
							var salesReturn = ParseDecimal(row, "salesreturn", rowNumber);
							var stockAdj = ParseDecimal(row, "stockadjustment", rowNumber);
							var closingGit = ParseDecimal(row, "closinggit", rowNumber);
							var closingStock = ParseDecimal(row, "closingstock", rowNumber);

							var warehouseReconciliationBusinessKey = WarehouseReconciliationKey(
								whseId, stateId, distId, plantId, productId, ifmsProductId, productStr, reportDateUtc!.Value);
							if (!ShouldProcessUploadedKey(uploadedBusinessKeys, warehouseReconciliationBusinessKey, RowFingerprint(row), rowNumber, result))
								continue;

							if (!warehouseReconByKey.TryGetValue(warehouseReconciliationBusinessKey, out var recon))
							{
								recon = new WarehouseDistrictGlobalStockReconciliation { CreatedAt = reportDateUtc.Value };
								_db.WarehouseDistrictGlobalStockReconciliations.Add(recon);
								warehouseReconByKey[warehouseReconciliationBusinessKey] = recon;
								result.RowsInserted++;
							}
							else
							{
								result.RowsUpdated++;
							}

							recon.PlantId = plantId;
							recon.ProductId = productId;
							recon.IfmsProductId = ifmsProductId;
							recon.StateId = stateId;
							recon.DistrictId = distId;
							recon.WarehouseId = whseId;
							recon.OpeningStockAtLocation = openingStockLoc;
							recon.OpeningStockGIT = openingGit;
							recon.ProductionImports = production;
							recon.Receipt = receipt;
							recon.Dispatches = dispatches;
							recon.Sales = sales;
							recon.SalesReturn = salesReturn;
							recon.StockAdjustment = stockAdj;
							recon.ClosingGIT = closingGit;
							recon.ClosingStock = closingStock;
							recon.UpdatedAt = now;
							recon.UpdatedBy = currentUserId;
						}
					}
				}
				finally
				{
					_db.ChangeTracker.AutoDetectChangesEnabled = previousAutoDetectChanges;
				}

				await _db.SaveChangesAsync(cancellationToken);
				await transaction.CommitAsync(cancellationToken);

				result.Message = $"Upload completed. Inserted: {result.RowsInserted}, Updated: {result.RowsUpdated}, " +
					$"Skipped: {result.RowsSkipped}, IFMS dealer mobile numbers updated: {result.IfmsDealerMobileNumbersUpdated}, " +
					$"Existing duplicate rows detected: {result.ExistingDuplicateRowsDetected}.";
				return result;
			}
			catch (OperationCanceledException)
			{
				await transaction.RollbackAsync(CancellationToken.None);
				throw;
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync(CancellationToken.None);
				_logger.LogError(ex, "Error processing bulk upload category {CategoryId} file {FileName}", categoryId, fileName);
				var errorMsg = ex.InnerException?.Message ?? ex.Message;
				return Failed($"Upload failed: {errorMsg}");
			}
		}

		private sealed class PreparedMasterRow
		{
			public int Index { get; init; }
			public int RowNumber { get; init; }
			public bool Skip { get; init; }
			public string StateName { get; init; } = string.Empty;
			public string DistrictName { get; init; } = string.Empty;
			public string SellerDistrictName { get; init; } = string.Empty;
			public string BuyerDistrictName { get; init; } = string.Empty;
			public string SubDistrictName { get; init; } = string.Empty;
			public string DealerExternalId { get; init; } = string.Empty;
			public string DealerName { get; init; } = string.Empty;
			public string? DealerMobileNo { get; init; }
			public string DealerTypeName { get; init; } = string.Empty;
			public string NatureName { get; init; } = string.Empty;
			public string CompanyName { get; init; } = string.Empty;
			public string PlantName { get; init; } = string.Empty;
			public string ProductName { get; init; } = string.Empty;
			public string MarketerName { get; init; } = string.Empty;
			public string WholesalerExternalId { get; init; } = string.Empty;
			public string WholesalerName { get; init; } = string.Empty;
			public string? WholesalerMobileNo { get; init; }
			public string WholesalerNatureName { get; init; } = string.Empty;
			public string DealerNatureName { get; init; } = string.Empty;
			public string TxnTypeName { get; init; } = string.Empty;
			public string UnitName { get; init; } = string.Empty;
			public string StatusName { get; init; } = string.Empty;
			public string AckThroughName { get; init; } = string.Empty;
			public string WarehouseName { get; init; } = string.Empty;
			public int? StateId { get; set; }
			public int? DistrictId { get; set; }
			public int? SellerDistrictId { get; set; }
			public int? BuyerDistrictId { get; set; }
			public int? SubDistrictId { get; set; }
			public int? DealerTypeId { get; set; }
			public int? NatureId { get; set; }
			public int? WholesalerNatureId { get; set; }
			public int? DealerNatureId { get; set; }
			public int? ProductId { get; set; }
			public int? IfmsProductId { get; set; }
		}

		private sealed class ReferenceComparer<T> : IEqualityComparer<T>
			where T : class
		{
			public static ReferenceComparer<T> Instance { get; } = new();

			public bool Equals(T? left, T? right) => ReferenceEquals(left, right);

			public int GetHashCode(T value) =>
				System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
		}

		private static readonly HashSet<string> SupportedCategories = new(StringComparer.Ordinal)
		{
			"One", "Two", "Three", "Four", "Five", "Six", "Seven"
		};

		private static bool RequiresReportDate(string categoryId) =>
			categoryId is "One" or "Three" or "Six" or "Seven";

		private static ExcelBulkUploadResult Failed(string message) => new()
		{
			Success = false,
			Message = message
		};

		private static string NormalizeKey(string? value) =>
			(value ?? string.Empty).Trim().ToLowerInvariant();

		private static string CleanDealerName(string? rawName)
		{
			if (string.IsNullOrWhiteSpace(rawName))
				return string.Empty;

			var withoutControls = new string(rawName
				.Where(character => !char.IsControl(character) &&
					character != '\uFEFF' &&
					character != '\u200B')
				.ToArray());

			var normalizedSpacing = string.Join(
				" ",
				withoutControls.Split(
					new[] { ' ', '\t', '\r', '\n' },
					StringSplitOptions.RemoveEmptyEntries));

			return normalizedSpacing
				.Trim()
				.Trim(DealerNameEdgeCharacters)
				.Trim();
		}

		private static string NormalizeDealerNameKey(string? value) =>
			CleanDealerName(value).ToLowerInvariant();

		private static bool IsValidDealerName(string? dealerName)
		{
			var cleaned = CleanDealerName(dealerName);
			if (cleaned.Length < 2 || InvalidDealerNameTokens.Contains(cleaned))
				return false;

			// Unicode-aware: Tamil and other local-language letters are also accepted.
			// Numeric-only values and punctuation-only placeholders are rejected.
			return cleaned.Any(char.IsLetter);
		}

		private static void EnsureValidDealerName(
			string? dealerName,
			int rowNumber,
			string fieldLabel)
		{
			if (IsValidDealerName(dealerName))
				return;

			throw new InvalidDataException(
				$"Row {rowNumber}: {fieldLabel} name '{dealerName}' is invalid. " +
				"Use a real name containing letters; values such as 0, 00, 000, NA, NULL or punctuation-only text are not allowed.");
		}

		private static string CleanProductName(string? rawName)
		{
			if (string.IsNullOrWhiteSpace(rawName))
				return string.Empty;

			var withoutControls = new string(rawName
				.Where(character => !char.IsControl(character) &&
					character != '\uFEFF' &&
					character != '\u200B')
				.ToArray());

			return string.Join(
				" ",
				withoutControls.Split(
					new[] { ' ', '\t', '\r', '\n' },
					StringSplitOptions.RemoveEmptyEntries))
				.Trim();
		}

		private static bool IsValidProductName(string? productName)
		{
			var cleaned = CleanProductName(productName);
			return cleaned.Length > 0 && !InvalidProductNameTokens.Contains(cleaned);
		}

		private static void EnsureValidProductName(
			string? productName,
			int rowNumber)
		{
			if (IsValidProductName(productName))
				return;

			throw new InvalidDataException(
				$"Row {rowNumber}: Product name '{productName}' is invalid.");
		}

		private static string NormalizeExternalDealerId(string? rawValue)
		{
			if (string.IsNullOrWhiteSpace(rawValue))
				return string.Empty;

			var value = rawValue
				.Trim()
				.Trim('\'', '\u2018', '\u2019', '"')
				.Replace(" ", string.Empty)
				.ToUpperInvariant();

			if ((value.Contains('E') || value.Contains('e')) &&
				double.TryParse(
					value,
					NumberStyles.Float,
					CultureInfo.InvariantCulture,
					out var scientific))
			{
				value = Math.Round(scientific, 0, MidpointRounding.AwayFromZero)
					.ToString("0", CultureInfo.InvariantCulture);
			}

			if (value.Length == 0 ||
				InvalidDealerNameTokens.Contains(value) ||
				value is "N/A" or "NA" or "NONE" or "NULL" or "NIL")
			{
				return string.Empty;
			}

			var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
			var containsLetter = value.Any(char.IsLetter);
			if (!containsLetter &&
				digitsOnly.Length > 0 &&
				digitsOnly.All(character => character == '0'))
			{
				return string.Empty;
			}

			return value;
		}

		private static IReadOnlyList<string> ExternalDealerIdKeys(string? rawValue)
		{
			var normalized = NormalizeExternalDealerId(rawValue);
			if (normalized.Length == 0)
				return Array.Empty<string>();

			var keys = new HashSet<string>(StringComparer.Ordinal)
			{
				normalized
			};

			var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());
			if (digitsOnly.Length > 0)
			{
				keys.Add(digitsOnly);

				var withoutLeadingZeroes = digitsOnly.TrimStart('0');
				if (withoutLeadingZeroes.Length > 0)
					keys.Add(withoutLeadingZeroes);
			}

			return keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
		}

		private static string IfmsDealerCompositeKey(
			string? dealerName,
			int? stateId,
			int? districtId,
			string? mobileNo,
			int? dealerTypeId,
			int? dealerNatureId) => string.Join('|',
			NormalizeDealerNameKey(dealerName),
			IdPart(stateId),
			IdPart(districtId),
			NormalizeKey(mobileNo),
			IdPart(dealerTypeId),
			IdPart(dealerNatureId));

		private static string DealerNameStateKey(
			string? dealerName,
			int? stateId) => string.Join('|',
			NormalizeDealerNameKey(dealerName),
			IdPart(stateId));

		private static string DealerLocationKey(
			string? dealerName,
			int? stateId,
			int? districtId) => string.Join('|',
			NormalizeDealerNameKey(dealerName),
			IdPart(stateId),
			IdPart(districtId));

		private static bool IsWarehouseTotalLabel(string? value)
		{
			var normalized = NormalizeKey(value);
			return normalized == "total" ||
				normalized == "grand total" ||
				normalized.EndsWith("-total", StringComparison.Ordinal) ||
				normalized.EndsWith(" total", StringComparison.Ordinal);
		}

		private static DateTime ToUtcDate(DateTime value) =>
			DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

		private static DateTime ParseDate(string raw, int rowNumber, string column)
		{
			var value = raw.Trim();
			var formats = new[]
			{
				"dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy",
				"yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "M/d/yyyy"
			};

			if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
				DateTimeStyles.AllowWhiteSpaces, out var parsed) ||
				DateTime.TryParse(value, CultureInfo.InvariantCulture,
					DateTimeStyles.AllowWhiteSpaces, out parsed) ||
				DateTime.TryParse(value, CultureInfo.GetCultureInfo("en-IN"),
					DateTimeStyles.AllowWhiteSpaces, out parsed))
			{
				return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
			}

			if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var oaDate) &&
				oaDate > 0 && oaDate < 2958466)
			{
				return DateTime.SpecifyKind(DateTime.FromOADate(oaDate), DateTimeKind.Utc);
			}

			throw new InvalidDataException(
				$"Row {rowNumber}: Invalid date '{raw}' in column '{column}'.");
		}

		private static string DealerIdentity(int? registrationId, int? ifmsId, string? name)
		{
			if (registrationId.HasValue) return "R:" + registrationId.Value;
			if (ifmsId.HasValue) return "I:" + ifmsId.Value;
			return "N:" + NormalizeDealerNameKey(name);
		}

		private static string DayKey(DateTime value) => ToUtcDate(value).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
		private static string IdPart(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "0";

		private static string ProductIdentity(
			int? productId,
			int? ifmsProductId,
			string? productName)
		{
			if (productId.HasValue) return "P:" + productId.Value;
			if (ifmsProductId.HasValue) return "I:" + ifmsProductId.Value;
			return "N:" + NormalizeKey(productName);
		}

		private static string DptKey(DptReport x) => DptKey(
			x.DealerRegistrationId, x.IfmsDealerId, x.RetailerName,
			x.ProductId, x.IfmsProductId, null,
			x.StateId, x.DistrictId, x.CompanyId, x.PlantId, x.CreatedAt);

		private static string DptKey(
			int? registrationId, int? ifmsId, string? dealerName,
			int? productId, int? ifmsProductId, string? productName,
			int? stateId, int? districtId, int? companyId, int? plantId,
			DateTime reportDate) => string.Join('|',
			DealerIdentity(registrationId, ifmsId, dealerName),
			IdPart(stateId), IdPart(districtId),
			ProductIdentity(productId, ifmsProductId, productName),
			IdPart(companyId), IdPart(plantId), DayKey(reportDate));

		private static string WholesalerStockKey(WholesalerStockAsOnToday x) => WholesalerStockKey(
			x.DealerRegistrationId, x.IfmsDealerId, x.AgencyName,
			x.ProductId, x.IfmsProductId, null,
			x.StateId, x.DistrictId, x.CompanyId, x.PlantId, x.StockDate);

		private static string WholesalerStockKey(
			int? registrationId, int? ifmsId, string? dealerName,
			int? productId, int? ifmsProductId, string? productName,
			int? stateId, int? districtId, int? companyId, int? plantId,
			DateTime stockDate) => string.Join('|',
			DealerIdentity(registrationId, ifmsId, dealerName),
			IdPart(stateId), IdPart(districtId),
			ProductIdentity(productId, ifmsProductId, productName),
			IdPart(companyId), IdPart(plantId), DayKey(stockDate));

		private static string SalesReceiptKey(SalesAndReceipt x) => SalesReceiptKey(
			x.DealerRegistrationId, x.IfmsDealerId, x.AgencyName,
			x.ProductId, x.IfmsProductId, null,
			x.StateId, x.DistrictId, x.CompanyId, x.PlantId, x.CreatedAt);

		private static string SalesReceiptKey(
			int? registrationId, int? ifmsId, string? dealerName,
			int? productId, int? ifmsProductId, string? productName,
			int? stateId, int? districtId, int? companyId, int? plantId,
			DateTime reportDate) => string.Join('|',
			DealerIdentity(registrationId, ifmsId, dealerName),
			IdPart(stateId), IdPart(districtId),
			ProductIdentity(productId, ifmsProductId, productName),
			IdPart(companyId), IdPart(plantId), DayKey(reportDate));

		private static string CompanySaleKey(SalesCompanySale x) =>
			CompanySaleKey(x.TransactionId, x.InvoiceNo, x.ProductId, x.IfmsProductId, null);

		private static string CompanySaleKey(
			string? transactionId,
			string? invoiceNo,
			int? productId,
			int? ifmsProductId,
			string? productName) => string.Join('|',
			NormalizeKey(transactionId),
			NormalizeKey(invoiceNo),
			ProductIdentity(productId, ifmsProductId, productName));

		private static string WholesalerSaleKey(SalesWholesaler x) =>
			WholesalerSaleKey(x.TransactionId, x.InvoiceNo, x.ProductId, x.IfmsProductId, null);

		private static string WholesalerSaleKey(
			string? transactionId,
			string? invoiceNo,
			int? productId,
			int? ifmsProductId,
			string? productName) => string.Join('|',
			NormalizeKey(transactionId),
			NormalizeKey(invoiceNo),
			ProductIdentity(productId, ifmsProductId, productName));

		private static string StateReconciliationKey(StateGlobalStockReconciliation x) =>
			StateReconciliationKey(
				x.StateId, x.PlantId, x.ProductId, x.IfmsProductId, null, x.CreatedAt);

		private static string StateReconciliationKey(
			int? stateId, int? plantId, int? productId, int? ifmsProductId,
			string? productName, DateTime reportDate) => string.Join('|',
			IdPart(stateId), IdPart(plantId),
			ProductIdentity(productId, ifmsProductId, productName), DayKey(reportDate));

		private static string WarehouseReconciliationKey(WarehouseDistrictGlobalStockReconciliation x) =>
			WarehouseReconciliationKey(
				x.WarehouseId, x.StateId, x.DistrictId, x.PlantId,
				x.ProductId, x.IfmsProductId, null, x.CreatedAt);

		private static string WarehouseReconciliationKey(
			int? warehouseId, int? stateId, int? districtId, int? plantId,
			int? productId, int? ifmsProductId, string? productName,
			DateTime reportDate) => string.Join('|',
			IdPart(warehouseId), IdPart(stateId), IdPart(districtId), IdPart(plantId),
			ProductIdentity(productId, ifmsProductId, productName), DayKey(reportDate));

		private static void BuildLatest<TEntity>(
			IEnumerable<TEntity> rows,
			Func<TEntity, string> keySelector,
			Func<TEntity, DateTime> updatedAtSelector,
			IDictionary<string, TEntity> destination,
			ExcelBulkUploadResult result)
			where TEntity : class
		{
			foreach (var group in rows.GroupBy(keySelector, StringComparer.Ordinal))
			{
				var ordered = group.OrderByDescending(updatedAtSelector).ToList();
				destination[group.Key] = ordered[0];

				if (ordered.Count > 1)
				{
					// Production-safe behavior: detect existing duplicates but never delete
					// rows automatically because other tables may reference them.
					result.ExistingDuplicateRowsDetected += ordered.Count - 1;
				}
			}
		}

		private static string RowFingerprint(IReadOnlyDictionary<string, string> row) =>
			string.Join("|", row
				.Where(x => !x.Key.StartsWith("__", StringComparison.Ordinal))
				.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
				.Select(x => $"{NormalizeKey(x.Key)}={NormalizeKey(x.Value)}"));

		private static bool ShouldProcessUploadedKey(
			IDictionary<string, string> uploadedKeys,
			string businessKey,
			string rowFingerprint,
			int rowNumber,
			ExcelBulkUploadResult result)
		{
			if (!uploadedKeys.TryGetValue(businessKey, out var firstFingerprint))
			{
				uploadedKeys[businessKey] = rowFingerprint;
				return true;
			}

			if (string.Equals(firstFingerprint, rowFingerprint, StringComparison.Ordinal))
			{
				result.RowsSkipped++;
				if (result.Warnings.Count < 100)
					result.Warnings.Add($"Row {rowNumber}: Exact duplicate row skipped.");
				return false;
			}

			throw new InvalidDataException(
				$"Row {rowNumber}: The file contains conflicting values for the same business record. " +
				"Correct the duplicate rows and upload again.");
		}

	}
}