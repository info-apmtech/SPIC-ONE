using ClosedXML.Excel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Lab;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// SAS Lab portal, version 2 (docs/sas-lab-portal-plan.md): lab coordinator, lab analyst and
	/// admin tracking. Contract: SPIC.Core/DTOs/LabDtos.cs (route list in its header). The report
	/// routes (api/Lab/reports*, api/Lab/batches/{id}/report*) live in LabReportsController.
	///
	/// Access (resolved once per request by LabAccess from the designation RoleAccess):
	///   coordinator pages (LabDashboard / LabConsignments / LabAnalysis / LabReports) or
	///   Admin / CorporateAdmin : read everything, write consignments / batches / documents
	///   LabTestEntry (analyst) : read and enter values only on the batches assigned to them
	///   LabTracking            : read everything
	///   anyone else            : 403 (me and languages answer every signed-in user)
	/// Conventions follow SasController: AsNoTracking projections, { Success = false, Message }
	/// errors, v1 SasStatusEvent rows on every v1 status change, uploads under Uploads/Sas/lab.
	/// </summary>
	[Authorize]
	[ApiController]
	[Route("api/Lab")]
	public class LabController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly IWebHostEnvironment _env;
		private readonly IConfiguration _config;
		private readonly LabAccess _access;
		private readonly LabActivityWriter _activities;
		private readonly LabAutoResultEngine _engine;
		private readonly ILabReportService _reports;
		private readonly ILogger<LabController> _logger;

		public LabController(AppDbContext db, IWebHostEnvironment env, IConfiguration config, LabAccess access,
			LabActivityWriter activities, LabAutoResultEngine engine, ILabReportService reports, ILogger<LabController> logger)
		{
			_db = db;
			_env = env;
			_config = config;
			_access = access;
			_activities = activities;
			_engine = engine;
			_reports = reports;
			_logger = logger;
		}

		// ---------------------------------------------------------------- constants

		private const int DefaultPageSize = 16;
		private const int MaxPageSize = 50;
		private const long MaxDocumentBytes = 20L * 1024 * 1024;
		private const int MaxDocumentsPerUpload = 10;
		private const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

		private static readonly Dictionary<string, string> DocumentTypes = new(StringComparer.OrdinalIgnoreCase)
		{
			[".pdf"] = "application/pdf",
			[".jpg"] = "image/jpeg",
			[".jpeg"] = "image/jpeg",
			[".png"] = "image/png",
			[".webp"] = "image/webp",
			[".xlsx"] = XlsxType,
			[".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
		};

		private static readonly Dictionary<string, (string Name, string Native)> LanguageNames = new(StringComparer.OrdinalIgnoreCase)
		{
			["en"] = ("English", "English"),
			["ta"] = ("Tamil", "தமிழ்"),
			["te"] = ("Telugu", "తెలుగు"),
			["mr"] = ("Marathi", "मराठी"),
			["hi"] = ("Hindi", "हिन्दी"),
			["kn"] = ("Kannada", "ಕನ್ನಡ"),
			["ml"] = ("Malayalam", "മലയാളം")
		};

		// Activity kinds that make up the batch Timeline (status transitions only, screen 5).
		private static readonly LabActivityKind[] TimelineKinds =
		{
			LabActivityKind.BatchCreated, LabActivityKind.StatusUpdated, LabActivityKind.AnalysisStarted, LabActivityKind.ReportGenerated
		};

		private int DelayedAfterDays => _config.GetValue<int?>("Sas:Lab:DelayedAfterDays") ?? 5;

		// ================================================================ me / users / lookups

		// GET api/Lab/me   (every signed-in user; drives the dashboard variant)
		[HttpGet("me")]
		public async Task<IActionResult> GetMe()
		{
			await _access.LoadAsync(User);
			return Ok(new LabMeDto
			{
				IsCoordinator = _access.IsCoordinator,
				IsAnalyst = _access.IsAnalyst,
				CanWrite = _access.CanWrite,
				UserId = _access.UserId,
				Name = _access.Name,
				DesignationName = _access.DesignationName
			});
		}

		// GET api/Lab/analysts
		[HttpGet("analysts")]
		public async Task<IActionResult> GetAnalysts()
		{
			if (await RequireReadAsync() is { } denied) return denied;
			return Ok(await LoadAnalystsAsync());
		}

		// GET api/Lab/languages   (every signed-in user; farmers get the farmer subset)
		[HttpGet("languages")]
		public async Task<IActionResult> GetLanguages()
		{
			await _access.LoadAsync(User);

			var farmer = _access.IsFarmer;
			var key = farmer ? "Sas:Lab:FarmerReportLanguages" : "Sas:Lab:ReportLanguages";
			var codes = _config.GetSection(key).GetChildren()
				.Select(c => c.Value ?? c["Code"])
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.Select(c => c!.Trim().ToLowerInvariant())
				.Distinct()
				.ToList();

			if (codes.Count == 0)
				codes = farmer ? new List<string> { "en", "ta" } : new List<string> { "en", "ta", "te", "mr" };

			return Ok(codes.Select(code => LanguageNames.TryGetValue(code, out var names)
					? new LabLanguageDto { Code = code, Name = names.Name, NativeName = names.Native }
					: new LabLanguageDto { Code = code, Name = code, NativeName = code })
				.ToList());
		}

		// GET api/Lab/financial-years   (start years, April to March, that have batches; newest first)
		[HttpGet("financial-years")]
		public async Task<IActionResult> GetFinancialYears()
		{
			if (await RequireReadAsync() is { } denied) return denied;

			var dates = await ScopedBatches().Select(b => b.BatchDate).ToListAsync();
			return Ok(dates.Select(FinancialYearStart).Distinct().OrderByDescending(y => y).ToList());
		}

		// ================================================================ dashboards

		// GET api/Lab/dashboard   (coordinator, screen 1)
		[HttpGet("dashboard")]
		public async Task<IActionResult> GetDashboard()
		{
			if (await RequireReadAsync() is { } denied) return denied;

			var consignments = ScopedConsignments();
			var received = consignments.Where(c => c.Status == ConsignmentStatus.Delivered || c.Status == ConsignmentStatus.Completed || c.BatchId != null);

			var batches = await ScopedBatches()
				.Select(b => new { b.Status, b.ReportCode, b.ReportGeneratedAt, HasReports = b.Reports.Any() })
				.ToListAsync();

			var recentConsignmentIds = await received
				.OrderByDescending(c => c.DeliveredAt ?? c.DispatchedAt).ThenByDescending(c => c.Id)
				.Take(5).Select(c => c.Id).ToListAsync();

			var recentBatchIds = await ScopedBatches()
				.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id)
				.Take(5).Select(b => b.Id).ToListAsync();

			return Ok(new LabDashboardDto
			{
				TotalConsignmentsReceived = await received.CountAsync(),
				PendingBatchCreation = await consignments.CountAsync(c => c.Status == ConsignmentStatus.Delivered && c.BatchId == null),
				TotalBatchesCreated = batches.Count,
				TakenForAnalysis = batches.Count(b => b.Status == SampleBatchStatus.TakenForAnalysis || b.Status == SampleBatchStatus.InProgress),
				AnalysisCompleted = batches.Count(b => b.Status == SampleBatchStatus.AnalysisCompleted),
				CompletedBatches = batches.Count(b => b.Status == SampleBatchStatus.Completed),
				ReportsGenerated = batches.Count(b => b.ReportCode != null || b.ReportGeneratedAt != null || b.HasReports),
				RecentConsignments = await BuildConsignmentRowsAsync(recentConsignmentIds),
				RecentBatches = await BuildBatchRowsAsync(recentBatchIds)
			});
		}

		// GET api/Lab/analyst/dashboard   (screen 13; an analyst's own batches, everyone else all)
		[HttpGet("analyst/dashboard")]
		public async Task<IActionResult> GetAnalystDashboard()
		{
			if (await RequireReadAsync() is { } denied) return denied;

			var scope = ScopedBatches();
			if (_access.IsAnalyst)
			{
				var me = _access.UserId;
				scope = scope.Where(b => b.AssignedToUserId == me);
			}

			var batches = await scope.Select(b => new { b.Id, b.Status, b.UpdatedAt }).ToListAsync();
			var batchIds = batches.Select(b => b.Id).ToList();
			var open = batches.Where(b => b.Status != SampleBatchStatus.Completed).Select(b => b.Id).ToHashSet();

			var items = await ItemsOfBatches(batchIds)
				.Select(i => new { BatchId = i.Collection!.Consignment!.BatchId!.Value, i.AnalysisStatus })
				.ToListAsync();

			var reportYears = await _db.LabReports.AsNoTracking()
				.Where(r => batchIds.Contains(r.BatchId))
				.Select(r => r.FinancialYearStart)
				.ToListAsync();

			var currentFy = FinancialYearStart(DateTime.Today);

			return Ok(new LabAnalystDashboardDto
			{
				AssignedBatches = batches.Count,
				PendingValueEntry = items.Count(i => open.Contains(i.BatchId) && i.AnalysisStatus == SampleAnalysisStatus.NotStarted),
				AutoResultReady = items.Count(i => open.Contains(i.BatchId) && i.AnalysisStatus == SampleAnalysisStatus.Completed),
				ReportsGeneratedThisFy = reportYears.Count(y => y == currentFy),
				PreviousFyReports = reportYears.Count(y => y < currentFy),
				PendingEntryBatches = batches.Count(b => b.Status == SampleBatchStatus.TakenForAnalysis || b.Status == SampleBatchStatus.InProgress),
				AutoResultReadyBatches = batches.Count(b => b.Status == SampleBatchStatus.AnalysisCompleted),
				ReportsToDownload = batches.Count(b => b.Status == SampleBatchStatus.Completed),
				ContinueBatchId = batches.Where(b => b.Status == SampleBatchStatus.InProgress)
					.OrderByDescending(b => b.UpdatedAt).ThenByDescending(b => b.Id)
					.Select(b => (int?)b.Id).FirstOrDefault()
			});
		}

		// ================================================================ consignments

		// GET api/Lab/consignments/stats   (screen 2)
		[HttpGet("consignments/stats")]
		public async Task<IActionResult> GetConsignmentStats()
		{
			if (await RequireReadAsync() is { } denied) return denied;

			var consignments = ScopedConsignments();
			var ids = await consignments.Select(c => c.Id).ToListAsync();

			var types = await _db.SampleItems.AsNoTracking()
				.Where(i => !i.IsDeleted && !i.Collection!.IsDeleted && i.Collection.ConsignmentId != null && ids.Contains(i.Collection.ConsignmentId.Value))
				.Select(i => i.SampleType)
				.ToListAsync();

			return Ok(new LabConsignmentStatsDto
			{
				TotalConsignments = ids.Count,
				PendingBatchCreation = await consignments.CountAsync(c => c.Status == ConsignmentStatus.Delivered && c.BatchId == null),
				BatchedConsignments = await consignments.CountAsync(c => c.BatchId != null),
				SoilSamples = types.Count(t => t != SampleType.Water),
				WaterSamples = types.Count(t => t != SampleType.Soil)
			});
		}

		// GET api/Lab/consignments?status=&stateId=&q=&from=&to=&page=&pageSize=
		[HttpGet("consignments")]
		public async Task<IActionResult> GetConsignments(
			[FromQuery] string? status, [FromQuery] int? stateId, [FromQuery] string? q,
			[FromQuery] DateTime? from, [FromQuery] DateTime? to,
			[FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
		{
			if (await RequireReadAsync() is { } denied) return denied;
			(page, pageSize) = Paging(page, pageSize);

			var query = ScopedConsignments();

			var statuses = ParseEnums<LabConsignmentStatus>(status);
			if (statuses == null)
				return BadRequest(new { Success = false, Message = "Status must be InTransit, BatchPending, BatchCreated or Completed." });
			if (statuses.Count > 0)
				query = WhereLabStatus(query, statuses);

			if (stateId is > 0)
				query = query.Where(ConsignmentInState(stateId.Value));

			if (from.HasValue) query = query.Where(c => c.DispatchedAt >= from.Value.Date);
			if (to.HasValue)
			{
				var end = to.Value.Date.AddDays(1);
				query = query.Where(c => c.DispatchedAt < end);
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(c =>
					c.Code.ToLower().Contains(term) ||
					c.TrackingNumber.ToLower().Contains(term) ||
					c.CourierService.ToLower().Contains(term) ||
					c.DispatchedByName.ToLower().Contains(term) ||
					(c.Batch != null && c.Batch.Code.ToLower().Contains(term)) ||
					c.Collections.Any(x => !x.IsDeleted && (x.Code.ToLower().Contains(term) ||
						x.Items.Any(i => !i.IsDeleted && (i.Code.ToLower().Contains(term) || i.Farmer!.Name.ToLower().Contains(term))))));
			}

			var total = await query.CountAsync();
			var ids = await query
				.OrderByDescending(c => c.DispatchedAt).ThenByDescending(c => c.Id)
				.Skip((page - 1) * pageSize).Take(pageSize)
				.Select(c => c.Id)
				.ToListAsync();

			return Ok(new PageResult<LabConsignmentRowDto>
			{
				Items = await BuildConsignmentRowsAsync(ids),
				Total = total,
				Page = page,
				PageSize = pageSize
			});
		}

		// GET api/Lab/consignments/{id}
		[HttpGet("consignments/{id:int}")]
		public async Task<IActionResult> GetConsignment(int id)
		{
			if (await RequireReadAsync() is { } denied) return denied;

			if (!await ScopedConsignments().AnyAsync(c => c.Id == id))
				return NotFound(new { Success = false, Message = "Consignment not found." });

			var summary = (await BuildConsignmentRowsAsync(new List<int> { id })).First();
			var consignment = await LabV1Sync.BuildConsignmentDetailAsync(_db, id, _access.IsAdmin);

			LabBatchRowDto? batch = null;
			if (summary.BatchId.HasValue && await ScopedBatches().AnyAsync(b => b.Id == summary.BatchId.Value))
				batch = (await BuildBatchRowsAsync(new List<int> { summary.BatchId.Value })).FirstOrDefault();

			return Ok(new LabConsignmentDetailDto
			{
				Summary = summary,
				Consignment = consignment ?? new ConsignmentDetailDto { Id = id, Code = summary.Code },
				Batch = batch
			});
		}

		// POST api/Lab/consignments/{id}/receive   (Dispatched / InTransit -> Delivered)
		[HttpPost("consignments/{id:int}/receive")]
		public async Task<IActionResult> ReceiveConsignment(int id)
		{
			if (await RequireWriteAsync("Only the lab coordinator can receive consignments.") is { } denied) return denied;

			var consignment = await _db.SampleConsignments
				.Include(c => c.Collections)
				.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

			if (consignment == null)
				return NotFound(new { Success = false, Message = "Consignment not found." });

			if (consignment.Status != ConsignmentStatus.Dispatched &&
				consignment.Status != ConsignmentStatus.InTransit &&
				consignment.Status != ConsignmentStatus.PendingPickup)
			{
				return BadRequest(new { Success = false, Message = $"{consignment.Code} has already been received at the lab." });
			}

			var now = DateTime.Now;
			LabV1Sync.MarkDelivered(_db, consignment, consignment.Collections, _access.Name, User.Identity?.Name, now);

			if (consignment.BatchId.HasValue)
			{
				var batch = await _db.SampleBatches.FirstOrDefaultAsync(b => b.Id == consignment.BatchId.Value);
				if (batch != null)
				{
					var samples = await _db.SampleItems.CountAsync(i => !i.IsDeleted && !i.Collection!.IsDeleted && i.Collection.ConsignmentId == id);
					_activities.SampleReceived(batch, consignment.Code, samples, now);
					batch.UpdatedAt = now;
				}
			}

			await _db.SaveChangesAsync();
			return Ok((await BuildConsignmentRowsAsync(new List<int> { id })).First());
		}

		// ================================================================ batches

		// GET api/Lab/batches/stats   (Analysis Tracking KPIs, screen 11; admin Lab Tracking, screen 21)
		[HttpGet("batches/stats")]
		public async Task<IActionResult> GetBatchStats()
		{
			if (await RequireReadAsync() is { } denied) return denied;

			var rows = await ScopedBatches()
				.Select(b => new { b.Status, b.TakenForAnalysisAt, b.AnalysisCompletedAt })
				.ToListAsync();

			var now = DateTime.Now;
			return Ok(new LabBatchStatsDto
			{
				TotalBatches = rows.Count,
				Created = rows.Count(b => b.Status == SampleBatchStatus.Created),
				TakenForAnalysis = rows.Count(b => b.Status == SampleBatchStatus.TakenForAnalysis || b.Status == SampleBatchStatus.InProgress),
				AnalysisCompleted = rows.Count(b => b.Status == SampleBatchStatus.AnalysisCompleted),
				Completed = rows.Count(b => b.Status == SampleBatchStatus.Completed),
				DelayedAnalysis = rows.Count(b => IsDelayed(b.Status, AnalysisDays(b.TakenForAnalysisAt, b.AnalysisCompletedAt, now)))
			});
		}

		// GET api/Lab/batches?status=&priority=&assignedTo=&sampleType=&financialYear=&q=&from=&to=
		//                    &batchId=&consignmentId=&stateId=&regionId=&stage=&page=&pageSize=
		[HttpGet("batches")]
		public async Task<IActionResult> GetBatches(
			[FromQuery] string? status, [FromQuery] string? priority, [FromQuery] string? assignedTo,
			[FromQuery] string? sampleType, [FromQuery] int? financialYear, [FromQuery] string? q,
			[FromQuery] DateTime? from, [FromQuery] DateTime? to,
			[FromQuery] int? batchId, [FromQuery] int? consignmentId, [FromQuery] int? stateId, [FromQuery] int? regionId,
			[FromQuery] string? stage, [FromQuery] string? assignedBy,
			[FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
		{
			if (await RequireReadAsync() is { } denied) return denied;
			(page, pageSize) = Paging(page, pageSize);

			var query = ScopedBatches();
			var now = DateTime.Now;

			// status: SampleBatchStatus name(s), comma separated; "Delayed" = in analysis too long.
			if (!string.IsNullOrWhiteSpace(status))
			{
				var tokens = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				var delayed = tokens.Any(t => t.Equals("Delayed", StringComparison.OrdinalIgnoreCase));
				var parsed = ParseEnums<SampleBatchStatus>(string.Join(',', tokens.Where(t => !t.Equals("Delayed", StringComparison.OrdinalIgnoreCase))));
				if (parsed == null)
					return BadRequest(new { Success = false, Message = "Unknown batch status." });

				if (delayed)
				{
					var threshold = DelayedAfterDays + 1;
					query = query.Where(b => b.Status != SampleBatchStatus.Completed && b.TakenForAnalysisAt != null &&
						b.TakenForAnalysisAt.Value.AddDays(threshold) <= (b.AnalysisCompletedAt ?? now));
				}
				if (parsed.Count > 0)
					query = query.Where(b => parsed.Contains(b.Status));
			}

			// stage (admin Lab Tracking tabs): BatchCreated, TakenForAnalysis (includes In Progress), AnalysisCompleted, Completed.
			if (!string.IsNullOrWhiteSpace(stage))
			{
				var key = stage.Trim();
				if (key.Equals("BatchCreated", StringComparison.OrdinalIgnoreCase) || key.Equals("Created", StringComparison.OrdinalIgnoreCase))
					query = query.Where(b => b.Status == SampleBatchStatus.Created);
				else if (TryParseEnum<SampleBatchStatus>(key, out var stageStatus))
				{
					query = stageStatus == SampleBatchStatus.TakenForAnalysis || stageStatus == SampleBatchStatus.InProgress
						? query.Where(b => b.Status == SampleBatchStatus.TakenForAnalysis || b.Status == SampleBatchStatus.InProgress)
						: query.Where(b => b.Status == stageStatus);
				}
				else
					return BadRequest(new { Success = false, Message = "Unknown stage." });
			}

			if (!string.IsNullOrWhiteSpace(priority))
			{
				var priorities = ParseEnums<SampleBatchPriority>(priority);
				if (priorities == null)
					return BadRequest(new { Success = false, Message = "Priority must be Low, Medium or High." });
				if (priorities.Count > 0)
					query = query.Where(b => priorities.Contains(b.Priority));
			}

			if (!string.IsNullOrWhiteSpace(assignedTo))
			{
				var who = assignedTo.Trim();
				query = query.Where(b => b.AssignedToUserId == who);
			}

			if (!string.IsNullOrWhiteSpace(assignedBy))
			{
				var by = assignedBy.Trim().ToLower();
				query = query.Where(b => b.AssignedByName != null && b.AssignedByName.ToLower().Contains(by));
			}

			if (!string.IsNullOrWhiteSpace(sampleType))
			{
				if (TryParseEnum<SamplePaymentType>(sampleType, out var paymentType))
				{
					query = paymentType == SamplePaymentType.Paid
						? query.Where(b => b.Consignments.Any(c => c.Collections.Any(x => !x.IsDeleted && x.PaymentType == SamplePaymentType.Paid)))
						: query.Where(b => !b.Consignments.Any(c => c.Collections.Any(x => !x.IsDeleted && x.PaymentType == SamplePaymentType.Paid)));
				}
				else if (TryParseEnum<SampleType>(sampleType, out var type))
				{
					query = query.Where(b => b.Consignments.Any(c => c.Collections.Any(x => !x.IsDeleted &&
						x.Items.Any(i => !i.IsDeleted && i.SampleType == type))));
				}
				else
					return BadRequest(new { Success = false, Message = "Sample type must be Free, Paid, Soil, Water or SoilAndWater." });
			}

			if (financialYear is > 0)
			{
				var start = new DateTime(financialYear.Value, 4, 1);
				var end = start.AddYears(1);
				query = query.Where(b => b.BatchDate >= start && b.BatchDate < end);
			}

			if (batchId is > 0) query = query.Where(b => b.Id == batchId.Value);
			if (consignmentId is > 0) query = query.Where(b => b.Consignments.Any(c => c.Id == consignmentId.Value));

			if (stateId is > 0)
			{
				var inState = ConsignmentInState(stateId.Value);
				query = query.Where(b => b.Consignments.AsQueryable().Any(inState));
			}

			if (regionId is > 0)
			{
				var region = regionId.Value;
				query = query.Where(b => b.Consignments.Any(c => c.Collections.Any(x => !x.IsDeleted && x.HeadquarterId != null &&
					_db.Headquarters.Any(h => h.Id == x.HeadquarterId && h.RegionId == region))));
			}

			if (from.HasValue) query = query.Where(b => b.BatchDate >= from.Value.Date);
			if (to.HasValue)
			{
				var end = to.Value.Date.AddDays(1);
				query = query.Where(b => b.BatchDate < end);
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(b =>
					b.Code.ToLower().Contains(term) ||
					(b.AssignedToName != null && b.AssignedToName.ToLower().Contains(term)) ||
					(b.ReportCode != null && b.ReportCode.ToLower().Contains(term)) ||
					b.Consignments.Any(c => c.Code.ToLower().Contains(term) ||
						c.Collections.Any(x => !x.IsDeleted && x.Code.ToLower().Contains(term))));
			}

			var total = await query.CountAsync();
			var ids = await query
				.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id)
				.Skip((page - 1) * pageSize).Take(pageSize)
				.Select(b => b.Id)
				.ToListAsync();

			return Ok(new PageResult<LabBatchRowDto>
			{
				Items = await BuildBatchRowsAsync(ids),
				Total = total,
				Page = page,
				PageSize = pageSize
			});
		}

		// POST api/Lab/batches   (screen 3; one or more Delivered, unbatched consignments)
		[HttpPost("batches")]
		public async Task<IActionResult> CreateBatch([FromBody] LabBatchCreateDto dto)
		{
			if (await RequireWriteAsync("Only the lab coordinator can create batches.") is { } denied) return denied;

			if (dto == null || dto.ConsignmentIds == null || dto.ConsignmentIds.Count == 0)
				return BadRequest(new { Success = false, Message = "Select at least one consignment." });

			if (!Enum.IsDefined(dto.Priority))
				return BadRequest(new { Success = false, Message = "Priority must be Low, Medium or High." });

			var wanted = dto.ConsignmentIds.Distinct().ToList();
			var consignments = await _db.SampleConsignments.AsNoTracking()
				.Where(c => wanted.Contains(c.Id) && !c.IsDeleted)
				.Select(c => new { c.Id, c.Code, c.Status, c.BatchId })
				.ToListAsync();

			var missing = wanted.Where(id => consignments.All(c => c.Id != id)).ToList();
			if (missing.Count > 0)
				return BadRequest(new { Success = false, Message = $"Consignment(s) not found: {string.Join(", ", missing)}." });

			var notReady = consignments.Where(c => c.Status != ConsignmentStatus.Delivered || c.BatchId != null).ToList();
			if (notReady.Count > 0)
			{
				return BadRequest(new
				{
					Success = false,
					Message = $"Only delivered consignments without a batch can be batched: {string.Join(", ", notReady.Select(c => c.Code))}."
				});
			}

			LabUserDto? analyst = null;
			var assignee = Clean(dto.AssignedToUserId);
			if (assignee != null)
			{
				analyst = (await LoadAnalystsAsync()).FirstOrDefault(a => a.UserId == assignee);
				if (analyst == null)
					return BadRequest(new { Success = false, Message = "The selected user is not a lab analyst." });
			}

			var items = await _db.SampleItems.AsNoTracking()
				.Where(i => !i.IsDeleted && !i.Collection!.IsDeleted && i.Collection.ConsignmentId != null && wanted.Contains(i.Collection.ConsignmentId.Value))
				.Select(i => new { i.SampleType, ConsignmentId = i.Collection!.ConsignmentId!.Value })
				.ToListAsync();

			if (items.Count == 0)
				return BadRequest(new { Success = false, Message = "The selected consignments carry no samples." });

			var parameters = await ActiveParametersAsync();
			var parameterRows = items.Sum(i => _engine.ParametersFor(i.SampleType, parameters).Count);
			var ordered = wanted.Select(id => consignments.First(c => c.Id == id)).OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase).ToList();

			SampleBatch? batch = null;
			for (var attempt = 1; attempt <= LabCodes.MaxAttempts; attempt++)
			{
				var now = DateTime.Now;
				await using var tx = await _db.Database.BeginTransactionAsync();
				try
				{
					batch = new SampleBatch
					{
						Code = await LabCodes.NextBatchCodeAsync(_db, now),
						BatchDate = dto.BatchDate?.Date ?? DateTime.Today,
						SampleCount = items.Count,
						Priority = dto.Priority,
						Remarks = Clean(dto.Remarks),
						Status = SampleBatchStatus.Created,
						CreatedByUserId = _access.UserId,
						CreatedByName = _access.Name,
						CreatedAt = now,
						UpdatedAt = now
					};

					if (analyst != null)
					{
						batch.AssignedToUserId = analyst.UserId;
						batch.AssignedToName = analyst.Name;
						batch.AssignedAt = now;
						batch.AssignedByName = _access.Name;
						batch.Status = SampleBatchStatus.TakenForAnalysis;
						batch.TakenForAnalysisAt = now;
					}

					_db.SampleBatches.Add(batch);
					await _db.SaveChangesAsync();   // the unique Code index is the guard between API instances

					// Claim the consignments atomically: another coordinator may have batched one meanwhile.
					var claimed = await _db.SampleConsignments
						.Where(c => wanted.Contains(c.Id) && !c.IsDeleted && c.BatchId == null && c.Status == ConsignmentStatus.Delivered)
						.ExecuteUpdateAsync(s => s.SetProperty(c => c.BatchId, batch.Id).SetProperty(c => c.UpdatedAt, now));

					if (claimed != wanted.Count)
					{
						await tx.RollbackAsync();
						_db.ChangeTracker.Clear();
						return Conflict(new { Success = false, Message = "One or more consignments were batched by somebody else. Refresh and try again." });
					}

					_activities.BatchCreated(batch, now);
					foreach (var c in ordered)
						_activities.SampleReceived(batch, c.Code, items.Count(i => i.ConsignmentId == c.Id), now);
					_activities.SampleLogged(batch, items.Count, now);
					_activities.ParameterAssigned(batch, parameterRows, items.Count, now);

					if (analyst != null)
					{
						_activities.Assigned(batch, analyst.Name, false, now);
						_activities.StatusUpdated(batch, SampleBatchStatus.TakenForAnalysis, now);

						var collections = await _db.SampleCollections
							.Where(x => !x.IsDeleted && x.ConsignmentId != null && wanted.Contains(x.ConsignmentId.Value))
							.ToListAsync();
						LabV1Sync.MarkTestInProgress(_db, batch.Code, collections, _access.Name, User.Identity?.Name, now);
					}

					await _db.SaveChangesAsync();
					await tx.CommitAsync();
					break;
				}
				catch (DbUpdateException ex) when (LabCodes.IsUniqueViolation(ex) && attempt < LabCodes.MaxAttempts)
				{
					await tx.RollbackAsync();
					_db.ChangeTracker.Clear();
					batch = null;
				}
			}

			if (batch == null)
				return StatusCode(500, new { Success = false, Message = "The batch could not be created. Please try again." });

			return Ok(await BuildBatchDetailAsync(batch.Id));
		}

		// GET api/Lab/batches/{id}
		[HttpGet("batches/{id:int}")]
		public async Task<IActionResult> GetBatch(int id)
		{
			if (await RequireReadAsync() is { } denied) return denied;

			if (!await ScopedBatches().AnyAsync(b => b.Id == id))
				return NotFound(new { Success = false, Message = "Batch not found." });

			return Ok(await BuildBatchDetailAsync(id));
		}

		// PATCH api/Lab/batches/{id}/assign   (body: LabBatchAssignDto)
		[HttpPatch("batches/{id:int}/assign")]
		public async Task<IActionResult> AssignBatch(int id, [FromBody] LabBatchAssignDto dto)
		{
			if (await RequireWriteAsync("Only the lab coordinator can assign batches.") is { } denied) return denied;

			var batch = await _db.SampleBatches.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Batch not found." });

			if (batch.Status == SampleBatchStatus.Completed)
				return Conflict(new { Success = false, Message = $"{batch.Code} is completed and can no longer be changed." });

			var assignee = Clean(dto?.AssignedToUserId);
			if (assignee == null && dto?.Priority == null)
				return BadRequest(new { Success = false, Message = "Choose an analyst or a priority." });

			if (dto?.Priority is { } priority)
			{
				if (!Enum.IsDefined(priority))
					return BadRequest(new { Success = false, Message = "Priority must be Low, Medium or High." });
				batch.Priority = priority;
			}

			var now = DateTime.Now;

			if (assignee != null && assignee != batch.AssignedToUserId)
			{
				var analyst = (await LoadAnalystsAsync()).FirstOrDefault(a => a.UserId == assignee);
				if (analyst == null)
					return BadRequest(new { Success = false, Message = "The selected user is not a lab analyst." });

				var reassigned = batch.AssignedToUserId != null;
				batch.AssignedToUserId = analyst.UserId;
				batch.AssignedToName = analyst.Name;
				batch.AssignedAt = now;
				batch.AssignedByName = _access.Name;
				_activities.Assigned(batch, analyst.Name, reassigned, now);

				if (batch.Status == SampleBatchStatus.Created)
					await TakeForAnalysisAsync(batch, now);
			}

			batch.UpdatedAt = now;
			await _db.SaveChangesAsync();

			return Ok(await BuildBatchDetailAsync(id));
		}

		// PATCH api/Lab/batches/{id}/status?status=TakenForAnalysis|Completed&remarks=
		[HttpPatch("batches/{id:int}/status")]
		public async Task<IActionResult> SetBatchStatus(int id, [FromQuery] string? status, [FromQuery] string? remarks)
		{
			if (await RequireWriteAsync("Only the lab coordinator can change a batch status.") is { } denied) return denied;

			if (!TryParseEnum<SampleBatchStatus>(status, out var target))
				return BadRequest(new { Success = false, Message = "Status must be TakenForAnalysis or Completed." });

			if (target != SampleBatchStatus.TakenForAnalysis && target != SampleBatchStatus.Completed)
			{
				return BadRequest(new
				{
					Success = false,
					Message = "In Progress and Analysis Completed are set by the system when values are entered; only TakenForAnalysis or Completed can be requested."
				});
			}

			var batch = await _db.SampleBatches.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Batch not found." });

			if (batch.Status == SampleBatchStatus.Completed)
				return Conflict(new { Success = false, Message = $"{batch.Code} is already completed." });

			var now = DateTime.Now;

			if (target == SampleBatchStatus.TakenForAnalysis)
			{
				if (batch.Status != SampleBatchStatus.Created)
					return BadRequest(new { Success = false, Message = $"{batch.Code} has already been taken for analysis." });
				if (string.IsNullOrWhiteSpace(batch.AssignedToUserId))
					return BadRequest(new { Success = false, Message = "Assign an analyst before taking the batch for analysis." });

				await TakeForAnalysisAsync(batch, now);
				batch.UpdatedAt = now;
				await _db.SaveChangesAsync();
				return Ok(await BuildBatchDetailAsync(id));
			}

			// Completed: every sample must carry submitted values.
			var statuses = await ItemsOfBatches(new List<int> { id }).Select(i => i.AnalysisStatus).ToListAsync();
			if (statuses.Count == 0 || statuses.Any(s => s != SampleAnalysisStatus.Completed))
			{
				var pending = statuses.Count(s => s != SampleAnalysisStatus.Completed);
				return BadRequest(new { Success = false, Message = $"Every sample must be completed first ({pending} of {statuses.Count} still pending)." });
			}

			await using (var tx = await _db.Database.BeginTransactionAsync())
			{
				batch.Status = SampleBatchStatus.Completed;
				batch.CompletedAt = now;
				batch.AnalysisCompletedAt ??= now;
				batch.FinalRemarks = Clean(remarks);
				batch.UpdatedAt = now;

				var consignments = await _db.SampleConsignments.Where(c => c.BatchId == id && !c.IsDeleted).ToListAsync();
				var consignmentIds = consignments.Select(c => c.Id).ToList();
				var collections = await _db.SampleCollections
					.Where(x => !x.IsDeleted && x.ConsignmentId != null && consignmentIds.Contains(x.ConsignmentId.Value))
					.ToListAsync();

				LabV1Sync.MarkCompleted(_db, batch.Code, consignments, collections, _access.Name, User.Identity?.Name, now);
				_activities.StatusUpdated(batch, SampleBatchStatus.Completed, now, batch.FinalRemarks);

				await _db.SaveChangesAsync();
				await tx.CommitAsync();
			}

			try
			{
				await _reports.GenerateForBatchAsync(id, _access.UserId, _access.Name, HttpContext.RequestAborted);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lab report generation failed for batch {BatchId}", id);
			}

			// The Report Generated timeline step, once the report service has produced the reports.
			_db.ChangeTracker.Clear();
			var reloaded = await _db.SampleBatches.FirstAsync(b => b.Id == id);
			var sampleReports = await _db.LabReports.CountAsync(r => r.BatchId == id);
			var logged = await _db.LabActivities.AnyAsync(a => a.BatchId == id && a.Kind == LabActivityKind.ReportGenerated);
			if (!logged && (sampleReports > 0 || reloaded.ReportCode != null))
			{
				_activities.ReportGenerated(reloaded, reloaded.ReportCode, sampleReports, reloaded.ReportGeneratedAt ?? DateTime.Now);
				await _db.SaveChangesAsync();
			}

			return Ok(await BuildBatchDetailAsync(id));
		}

		// ================================================================ samples and parameters

		// GET api/Lab/batches/{id}/samples/stats
		[HttpGet("batches/{id:int}/samples/stats")]
		public async Task<IActionResult> GetSampleStats(int id)
		{
			if (await RequireReadAsync() is { } denied) return denied;
			if (!await ScopedBatches().AnyAsync(b => b.Id == id))
				return NotFound(new { Success = false, Message = "Batch not found." });

			var statuses = await ItemsOfBatches(new List<int> { id }).Select(i => i.AnalysisStatus).ToListAsync();
			return Ok(new LabSampleStatsDto
			{
				Total = statuses.Count,
				NotStarted = statuses.Count(s => s == SampleAnalysisStatus.NotStarted),
				InProgress = statuses.Count(s => s == SampleAnalysisStatus.InProgress),
				Completed = statuses.Count(s => s == SampleAnalysisStatus.Completed)
			});
		}

		// GET api/Lab/batches/{id}/samples?status=&sampleType=&parameter=&q=&from=&to=&page=&pageSize=
		[HttpGet("batches/{id:int}/samples")]
		public async Task<IActionResult> GetSamples(int id,
			[FromQuery] string? status, [FromQuery] string? sampleType, [FromQuery] string? parameter, [FromQuery] string? q,
			[FromQuery] DateTime? from, [FromQuery] DateTime? to,
			[FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
		{
			if (await RequireReadAsync() is { } denied) return denied;
			(page, pageSize) = Paging(page, pageSize);

			var batch = await ScopedBatches().FirstOrDefaultAsync(b => b.Id == id);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Batch not found." });

			var filtered = FilterSamples(await LoadBatchSamplesAsync(batch), status, sampleType, parameter, q, from, to, out var error);
			if (error != null)
				return BadRequest(new { Success = false, Message = error });

			return Ok(Page(filtered.Select(s => s.Row), page, pageSize));
		}

		// GET api/Lab/batches/{id}/samples/export   (.xlsx, same filters; also ?access_token=)
		[HttpGet("batches/{id:int}/samples/export")]
		[AllowAnonymous]
		[LabQueryToken]
		public async Task<IActionResult> ExportSamples(int id,
			[FromQuery] string? status, [FromQuery] string? sampleType, [FromQuery] string? parameter, [FromQuery] string? q,
			[FromQuery] DateTime? from, [FromQuery] DateTime? to)
		{
			if (await RequireReadAsync() is { } denied) return denied;

			var batch = await ScopedBatches().FirstOrDefaultAsync(b => b.Id == id);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Batch not found." });

			var filtered = FilterSamples(await LoadBatchSamplesAsync(batch), status, sampleType, parameter, q, from, to, out var error);
			if (error != null)
				return BadRequest(new { Success = false, Message = error });

			using var workbook = new XLWorkbook();
			var sheet = workbook.Worksheets.Add("Samples");
			var headers = new[] { "Sample ID", "Sample Code", "Sample Type", "Soil Samples", "Water Samples", "Parameter", "Status", "Progress %" };
			WriteHeader(sheet, headers);

			var row = 2;
			foreach (var s in filtered.Select(x => x.Row))
			{
				sheet.Cell(row, 1).Value = s.SampleId;
				sheet.Cell(row, 2).Value = s.SampleCode;
				sheet.Cell(row, 3).Value = s.PaymentType.ToString();
				sheet.Cell(row, 4).Value = s.SampleType == SampleType.Water ? 0 : 1;
				sheet.Cell(row, 5).Value = s.SampleType == SampleType.Soil ? 0 : 1;
				sheet.Cell(row, 6).Value = s.CurrentParameter ?? "";
				sheet.Cell(row, 7).Value = SampleStatusText(s.Status);
				sheet.Cell(row, 8).Value = s.ProgressPercent;
				row++;
			}

			SetWidths(sheet, 18, 24, 12, 13, 14, 26, 14, 11);
			return Xlsx(workbook, $"{batch.Code}-samples.xlsx");
		}

		// GET api/Lab/batches/{id}/parameters/stats   (parameter rows across the batch)
		[HttpGet("batches/{id:int}/parameters/stats")]
		public async Task<IActionResult> GetParameterStats(int id)
		{
			if (await RequireReadAsync() is { } denied) return denied;

			var batch = await ScopedBatches().FirstOrDefaultAsync(b => b.Id == id);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Batch not found." });

			var rows = (await LoadBatchSamplesAsync(batch)).SelectMany(s => ParameterRows(s).Parameters).ToList();
			return Ok(new LabParameterStatsDto
			{
				TotalParameters = rows.Count,
				Completed = rows.Count(r => r.RowStatus == SampleAnalysisStatus.Completed),
				InProgress = rows.Count(r => r.RowStatus == SampleAnalysisStatus.InProgress),
				NotStarted = rows.Count(r => r.RowStatus == SampleAnalysisStatus.NotStarted)
			});
		}

		// GET api/Lab/batches/{id}/parameters?status=&sampleType=&parameter=&q=&page=&pageSize=
		[HttpGet("batches/{id:int}/parameters")]
		public async Task<IActionResult> GetParameters(int id,
			[FromQuery] string? status, [FromQuery] string? sampleType, [FromQuery] string? parameter, [FromQuery] string? q,
			[FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
		{
			if (await RequireReadAsync() is { } denied) return denied;
			(page, pageSize) = Paging(page, pageSize);

			var batch = await ScopedBatches().FirstOrDefaultAsync(b => b.Id == id);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Batch not found." });

			var rows = FilterParameterRows(await LoadBatchSamplesAsync(batch), status, sampleType, parameter, q, out var error);
			if (error != null)
				return BadRequest(new { Success = false, Message = error });

			return Ok(Page(rows, page, pageSize));
		}

		// GET api/Lab/batches/{id}/parameters/export   (.xlsx; also ?access_token=)
		[HttpGet("batches/{id:int}/parameters/export")]
		[AllowAnonymous]
		[LabQueryToken]
		public async Task<IActionResult> ExportParameters(int id,
			[FromQuery] string? status, [FromQuery] string? sampleType, [FromQuery] string? parameter, [FromQuery] string? q)
		{
			if (await RequireReadAsync() is { } denied) return denied;

			var batch = await ScopedBatches().FirstOrDefaultAsync(b => b.Id == id);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Batch not found." });

			var samples = FilterParameterRows(await LoadBatchSamplesAsync(batch), status, sampleType, parameter, q, out var error);
			if (error != null)
				return BadRequest(new { Success = false, Message = error });

			using var workbook = new XLWorkbook();
			var sheet = workbook.Worksheets.Add("Parameters");
			var headers = new[]
			{
				"Sample ID", "Sample Code", "Parameter Code", "Parameter Name", "Sample Type", "Soil Samples", "Water Samples",
				"Unit", "Reporting Limit", "Normal Range", "Test Value", "Result", "Status", "Entry Status"
			};
			WriteHeader(sheet, headers);

			var row = 2;
			foreach (var sample in samples)
			{
				foreach (var p in sample.Parameters)
				{
					sheet.Cell(row, 1).Value = sample.Sample.SampleId;
					sheet.Cell(row, 2).Value = sample.Sample.SampleCode;
					sheet.Cell(row, 3).Value = p.Code;
					sheet.Cell(row, 4).Value = p.Name;
					sheet.Cell(row, 5).Value = sample.Sample.PaymentType.ToString();
					sheet.Cell(row, 6).Value = sample.Sample.SampleType == SampleType.Water ? 0 : 1;
					sheet.Cell(row, 7).Value = sample.Sample.SampleType == SampleType.Soil ? 0 : 1;
					sheet.Cell(row, 8).Value = p.Unit ?? "";
					sheet.Cell(row, 9).Value = p.ReportingLimit ?? "";
					sheet.Cell(row, 10).Value = p.NormalRange ?? "";
					sheet.Cell(row, 11).Value = p.EnteredValue ?? "";
					sheet.Cell(row, 12).Value = p.ResultLabel ?? "";
					sheet.Cell(row, 13).Value = p.Status?.ToString() ?? "";
					sheet.Cell(row, 14).Value = SampleStatusText(p.RowStatus);
					row++;
				}
			}

			SetWidths(sheet, 18, 24, 14, 26, 12, 13, 14, 12, 15, 14, 12, 12, 12, 14);
			return Xlsx(workbook, $"{batch.Code}-parameters.xlsx");
		}

		// ================================================================ documents

		// GET api/Lab/batches/{id}/documents?kind=&uploader=&q=&from=&to=&page=&pageSize=
		[HttpGet("batches/{id:int}/documents")]
		public async Task<IActionResult> GetDocuments(int id,
			[FromQuery] string? kind, [FromQuery] string? uploader, [FromQuery] string? q,
			[FromQuery] DateTime? from, [FromQuery] DateTime? to,
			[FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
		{
			if (await RequireReadAsync() is { } denied) return denied;
			(page, pageSize) = Paging(page, pageSize);

			if (!await ScopedBatches().AnyAsync(b => b.Id == id))
				return NotFound(new { Success = false, Message = "Batch not found." });

			var query = _db.LabDocuments.AsNoTracking().Where(d => d.BatchId == id && !d.IsDeleted);

			if (!string.IsNullOrWhiteSpace(kind))
			{
				var kinds = ParseEnums<LabDocumentKind>(kind);
				if (kinds == null)
					return BadRequest(new { Success = false, Message = "Kind must be Batch, Sample or Reference." });
				if (kinds.Count > 0)
					query = query.Where(d => kinds.Contains(d.Kind));
			}

			if (!string.IsNullOrWhiteSpace(uploader))
			{
				var who = uploader.Trim();
				var lower = who.ToLower();
				query = query.Where(d => d.UploadedByUserId == who || (d.UploadedByName != null && d.UploadedByName.ToLower().Contains(lower)));
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(d => d.FileName.ToLower().Contains(term) || (d.Description != null && d.Description.ToLower().Contains(term)));
			}

			if (from.HasValue) query = query.Where(d => d.CreatedAt >= from.Value.Date);
			if (to.HasValue)
			{
				var end = to.Value.Date.AddDays(1);
				query = query.Where(d => d.CreatedAt < end);
			}

			var total = await query.CountAsync();
			var rows = await query.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id)
				.Skip((page - 1) * pageSize).Take(pageSize)
				.ToListAsync();

			return Ok(new PageResult<LabDocumentDto> { Items = rows.Select(MapDocument).ToList(), Total = total, Page = page, PageSize = pageSize });
		}

		// POST api/Lab/batches/{id}/documents?kind=Batch|Sample|Reference&description=   (multipart "files")
		[HttpPost("batches/{id:int}/documents")]
		[RequestSizeLimit(MaxDocumentsPerUpload * MaxDocumentBytes + 1024 * 1024)]
		[RequestFormLimits(MultipartBodyLengthLimit = MaxDocumentsPerUpload * MaxDocumentBytes + 1024 * 1024)]
		public async Task<IActionResult> UploadDocuments(int id, [FromQuery] string? kind, [FromQuery] string? description,
			[FromForm] List<IFormFile> files)
		{
			if (await RequireWriteAsync("Only the lab coordinator can upload documents.") is { } denied) return denied;

			var batch = await _db.SampleBatches.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Batch not found." });

			var documentKind = LabDocumentKind.Batch;
			if (!string.IsNullOrWhiteSpace(kind) && !TryParseEnum(kind, out documentKind))
				return BadRequest(new { Success = false, Message = "Kind must be Batch, Sample or Reference." });

			if (files == null || files.Count == 0)
				return BadRequest(new { Success = false, Message = "No files uploaded." });
			if (files.Count > MaxDocumentsPerUpload)
				return BadRequest(new { Success = false, Message = $"At most {MaxDocumentsPerUpload} files per upload." });

			foreach (var file in files)
			{
				if (file == null || file.Length == 0)
					return BadRequest(new { Success = false, Message = "An uploaded file is empty." });
				if (file.Length > MaxDocumentBytes)
					return BadRequest(new { Success = false, Message = $"\"{file.FileName}\" is larger than 20 MB." });
				if (!DocumentTypes.ContainsKey(Path.GetExtension(file.FileName)))
					return BadRequest(new { Success = false, Message = "Only PDF, JPG, PNG, WEBP, XLSX and DOCX files are allowed." });
			}

			var folder = Path.Combine(GetUploadsRoot(), "Sas", "lab", id.ToString());
			Directory.CreateDirectory(folder);

			var now = DateTime.Now;
			var saved = new List<LabDocument>();
			var written = new List<string>();

			try
			{
				foreach (var file in files)
				{
					var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
					var storedName = $"{Guid.NewGuid():N}{ext}";
					var physical = Path.Combine(folder, storedName);

					await using (var stream = new FileStream(physical, FileMode.CreateNew))
						await file.CopyToAsync(stream);
					written.Add(physical);

					var document = new LabDocument
					{
						BatchId = id,
						Kind = documentKind,
						FileName = Path.GetFileName(file.FileName),
						Description = Clean(description),
						StoredPath = $"Sas/lab/{id}/{storedName}",
						ContentType = DocumentTypes[ext],
						Size = file.Length,
						UploadedByUserId = _access.UserId,
						UploadedByName = _access.Name,
						CreatedAt = now
					};

					_db.LabDocuments.Add(document);
					saved.Add(document);
					_activities.DocumentUploaded(batch, document.FileName, documentKind, now);
				}

				batch.UpdatedAt = now;
				await _db.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lab document upload failed for batch {BatchId}", id);
				foreach (var path in written)
				{
					try { System.IO.File.Delete(path); } catch (Exception) { /* best effort */ }
				}
				return StatusCode(500, new { Success = false, Message = "The files could not be uploaded. Please try again." });
			}

			return Ok(saved.Select(MapDocument).ToList());
		}

		// DELETE api/Lab/documents/{docId}   (soft delete; the file stays for the audit trail)
		[HttpDelete("documents/{docId:int}")]
		public async Task<IActionResult> DeleteDocument(int docId)
		{
			if (await RequireWriteAsync("Only the lab coordinator can delete documents.") is { } denied) return denied;

			var document = await _db.LabDocuments.FirstOrDefaultAsync(d => d.Id == docId && !d.IsDeleted);
			if (document == null)
				return NotFound(new { Success = false, Message = "Document not found." });

			var batch = await _db.SampleBatches.FirstAsync(b => b.Id == document.BatchId);
			var now = DateTime.Now;

			document.IsDeleted = true;
			batch.UpdatedAt = now;
			_activities.DocumentDeleted(batch, document.FileName, now);
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Document deleted." });
		}

		// ================================================================ activity log

		// GET api/Lab/batches/{id}/activities?kind=&by=&q=&from=&to=&page=&pageSize=
		[HttpGet("batches/{id:int}/activities")]
		public async Task<IActionResult> GetActivities(int id,
			[FromQuery] string? kind, [FromQuery] string? by, [FromQuery] string? q,
			[FromQuery] DateTime? from, [FromQuery] DateTime? to,
			[FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
		{
			if (await RequireReadAsync() is { } denied) return denied;
			(page, pageSize) = Paging(page, pageSize);

			if (!await ScopedBatches().AnyAsync(b => b.Id == id))
				return NotFound(new { Success = false, Message = "Batch not found." });

			var query = _db.LabActivities.AsNoTracking().Where(a => a.BatchId == id);

			if (!string.IsNullOrWhiteSpace(kind))
			{
				var kinds = ParseEnums<LabActivityKind>(kind);
				if (kinds == null)
					return BadRequest(new { Success = false, Message = "Unknown activity kind." });
				if (kinds.Count > 0)
					query = query.Where(a => kinds.Contains(a.Kind));
			}

			if (!string.IsNullOrWhiteSpace(by))
			{
				var who = by.Trim();
				var lower = who.ToLower();
				query = query.Where(a => a.ByUserId == who || (a.ByName != null && a.ByName.ToLower().Contains(lower)));
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(a => a.Title.ToLower().Contains(term) || (a.Description != null && a.Description.ToLower().Contains(term)));
			}

			if (from.HasValue) query = query.Where(a => a.At >= from.Value.Date);
			if (to.HasValue)
			{
				var end = to.Value.Date.AddDays(1);
				query = query.Where(a => a.At < end);
			}

			var total = await query.CountAsync();
			var rows = await query.OrderByDescending(a => a.At).ThenByDescending(a => a.Id)
				.Skip((page - 1) * pageSize).Take(pageSize)
				.ToListAsync();

			return Ok(new PageResult<LabActivityDto> { Items = rows.Select(MapActivity).ToList(), Total = total, Page = page, PageSize = pageSize });
		}

		// ================================================================ sample-wise entry

		// GET api/Lab/samples/{itemId}
		[HttpGet("samples/{itemId:int}")]
		public async Task<IActionResult> GetSampleEntry(int itemId)
		{
			if (await RequireReadAsync() is { } denied) return denied;

			var batchId = await BatchIdOfItemAsync(itemId);
			if (batchId == null)
				return NotFound(new { Success = false, Message = "Sample not found in a lab batch." });

			var batch = await ScopedBatches().FirstOrDefaultAsync(b => b.Id == batchId.Value);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Sample not found in a lab batch." });

			return Ok(await BuildEntryAsync(batch, itemId));
		}

		// POST api/Lab/samples/preview   (computes without saving)
		[HttpPost("samples/preview")]
		public async Task<IActionResult> PreviewSample([FromBody] LabSampleValuesDto dto)
		{
			if (await RequireReadAsync() is { } denied) return denied;

			if (dto == null || dto.SampleItemId <= 0)
				return BadRequest(new { Success = false, Message = "Choose a sample." });

			var batchId = await BatchIdOfItemAsync(dto.SampleItemId);
			if (batchId == null || !await ScopedBatches().AnyAsync(b => b.Id == batchId.Value))
				return NotFound(new { Success = false, Message = "Sample not found in a lab batch." });

			var item = await _db.SampleItems.AsNoTracking()
				.Where(i => i.Id == dto.SampleItemId)
				.Select(i => new { i.SampleType, i.Crop1 })
				.FirstAsync();

			var parameters = _engine.ParametersFor(item.SampleType, await ActiveParametersAsync());
			var evaluation = _engine.Evaluate(item.SampleType, item.Crop1, parameters, ValueMap(dto));

			if (!evaluation.IsValid)
				return BadRequest(new { Success = false, Message = string.Join(" ", evaluation.Errors), Errors = evaluation.Errors });

			StampEntryFields(evaluation.Result.Parameters, parameters);
			return Ok(evaluation.Result);
		}

		// PUT api/Lab/samples/{itemId}/values   (Submit=false: draft -> InProgress; Submit=true: Completed)
		[HttpPut("samples/{itemId:int}/values")]
		public async Task<IActionResult> SaveSampleValues(int itemId, [FromBody] LabSampleValuesDto dto)
		{
			if (await RequireReadAsync() is { } denied) return denied;

			if (dto == null)
				return BadRequest(new { Success = false, Message = "No values supplied." });
			if (dto.SampleItemId != 0 && dto.SampleItemId != itemId)
				return BadRequest(new { Success = false, Message = "The sample in the body does not match the route." });

			var batchId = await BatchIdOfItemAsync(itemId);
			if (batchId == null)
				return NotFound(new { Success = false, Message = "Sample not found in a lab batch." });

			var batch = await _db.SampleBatches.FirstOrDefaultAsync(b => b.Id == batchId.Value && !b.IsDeleted);
			if (batch == null)
				return NotFound(new { Success = false, Message = "Sample not found in a lab batch." });

			var mine = _access.IsAnalyst && batch.AssignedToUserId == _access.UserId;
			if (!_access.CanWrite && !mine)
			{
				return _access.AnalystOnly
					? NotFound(new { Success = false, Message = "Sample not found in a lab batch." })
					: Forbid403("Only the assigned analyst, the lab coordinator or an admin can enter test values.");
			}

			if (batch.Status == SampleBatchStatus.Completed)
				return Conflict(new { Success = false, Message = $"{batch.Code} is completed and its report generated; values are locked." });
			if (batch.Status == SampleBatchStatus.Created)
				return BadRequest(new { Success = false, Message = $"{batch.Code} has not been taken for analysis yet." });

			var item = await _db.SampleItems.FirstAsync(i => i.Id == itemId);
			var parameters = _engine.ParametersFor(item.SampleType, await ActiveParametersAsync());
			var evaluation = _engine.Evaluate(item.SampleType, item.Crop1, parameters, ValueMap(dto));

			if (!evaluation.IsValid)
				return BadRequest(new { Success = false, Message = string.Join(" ", evaluation.Errors), Errors = evaluation.Errors });

			var rows = evaluation.Result.Parameters;
			if (dto.Submit)
			{
				var missing = parameters.Where(LabAutoResultEngine.IsEnterable)
					.Where(p => rows.First(r => r.LabParameterId == p.Id).EnteredValue == null)
					.Select(p => p.Name)
					.ToList();
				if (missing.Count > 0)
					return BadRequest(new { Success = false, Message = $"Enter every parameter before submitting. Missing: {string.Join(", ", missing)}." });
			}

			var now = DateTime.Now;
			var sampleId = await DisplayIdAsync(batch.Id, item);
			var entered = rows.Count(r => r.EnteredValue != null);

			await using var tx = await _db.Database.BeginTransactionAsync();

			var existing = await _db.SampleLabResults.Where(r => r.SampleItemId == itemId).ToListAsync();
			_db.SampleLabResults.RemoveRange(existing);

			var order = 1;
			foreach (var row in rows)
			{
				if (row.EnteredValue == null) { order++; continue; }
				_db.SampleLabResults.Add(new SampleLabResult
				{
					SampleItemId = itemId,
					Parameter = row.Name,
					NormalRange = row.NormalRange,
					Unit = row.Unit,
					EnteredValue = row.EnteredValue,
					ResultLabel = row.ResultLabel,
					Status = row.Status ?? LabResultStatus.Normal,
					Hint = row.Hint,
					SortOrder = order++,
					LabParameterId = row.LabParameterId,
					EnteredByName = _access.Name,
					EnteredAt = now
				});
			}

			if (dto.Submit)
			{
				item.AnalysisStatus = SampleAnalysisStatus.Completed;
				item.AnalysisStartedAt ??= now;
				item.AnalysisCompletedAt = now;
			}
			else
			{
				item.AnalysisStatus = entered > 0 ? SampleAnalysisStatus.InProgress : SampleAnalysisStatus.NotStarted;
				if (entered > 0) item.AnalysisStartedAt ??= now;
				item.AnalysisCompletedAt = null;
			}

			if (entered > 0 && batch.Status == SampleBatchStatus.TakenForAnalysis)
			{
				batch.Status = SampleBatchStatus.InProgress;
				batch.AnalysisStartedAt ??= now;
				_activities.AnalysisStarted(batch, sampleId, now);
			}

			if (dto.Submit)
				_activities.ResultEntered(batch, sampleId, evaluation.Result.OverallStatusText, now);
			else
				_activities.AnalysisInProgress(batch, sampleId, entered, rows.Count, now);

			batch.UpdatedAt = now;
			await _db.SaveChangesAsync();

			// Every sample submitted -> Analysis Completed ("Auto Result Ready"); a sample reopened -> back to In Progress.
			var statuses = await ItemsOfBatches(new List<int> { batch.Id }).Select(i => i.AnalysisStatus).ToListAsync();
			var allDone = statuses.Count > 0 && statuses.All(s => s == SampleAnalysisStatus.Completed);

			if (allDone && batch.Status != SampleBatchStatus.AnalysisCompleted)
			{
				batch.Status = SampleBatchStatus.AnalysisCompleted;
				batch.AnalysisStartedAt ??= now;
				batch.AnalysisCompletedAt = now;
				_activities.StatusUpdated(batch, SampleBatchStatus.AnalysisCompleted, now);
				await _db.SaveChangesAsync();
			}
			else if (!allDone && batch.Status == SampleBatchStatus.AnalysisCompleted)
			{
				batch.Status = SampleBatchStatus.InProgress;
				batch.AnalysisCompletedAt = null;
				_activities.StatusUpdated(batch, SampleBatchStatus.InProgress, now);
				await _db.SaveChangesAsync();
			}

			await tx.CommitAsync();

			var fresh = await _db.SampleBatches.AsNoTracking().FirstAsync(b => b.Id == batch.Id);
			return Ok(await BuildEntryAsync(fresh, itemId));
		}

		// ================================================================ builders

		private sealed class SampleContext
		{
			public LabSampleRowDto Row { get; init; } = new();
			public IReadOnlyList<LabParameter> Parameters { get; init; } = Array.Empty<LabParameter>();
			public List<SampleLabResult> Results { get; init; } = new();
			public string? FarmerMobile { get; init; }
			public DateTime CollectedOn { get; init; }
		}

		/// <summary>Every sample of a batch with its display id (SAS-SOIL-001, numbered per type in
		/// code order), applicable parameters and stored results.</summary>
		private async Task<List<SampleContext>> LoadBatchSamplesAsync(SampleBatch batch)
		{
			var parameters = await ActiveParametersAsync();

			var items = await ItemsOfBatches(new List<int> { batch.Id })
				.Select(i => new
				{
					i.Id,
					i.CollectionId,
					i.Code,
					i.SampleType,
					i.Crop1,
					i.AnalysisStatus,
					i.AnalysisStartedAt,
					i.AnalysisCompletedAt,
					CollectionCode = i.Collection!.Code,
					i.Collection.PaymentType,
					i.Collection.CollectionDate,
					FarmerName = i.Farmer!.Name,
					i.Farmer.Mobile,
					i.Farmer.Village,
					i.Farmer.DistrictName
				})
				.ToListAsync();

			items = items.OrderBy(i => i.CollectionCode, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.Id).ToList();

			var itemIds = items.Select(i => i.Id).ToList();
			var results = await _db.SampleLabResults.AsNoTracking()
				.Where(r => itemIds.Contains(r.SampleItemId))
				.OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
				.ToListAsync();

			var counters = new Dictionary<SampleType, int>();
			var list = new List<SampleContext>();

			foreach (var i in items)
			{
				counters[i.SampleType] = counters.TryGetValue(i.SampleType, out var n) ? n + 1 : 1;
				var applicable = _engine.ParametersFor(i.SampleType, parameters);
				var own = results.Where(r => r.SampleItemId == i.Id).ToList();
				var enteredIds = EnteredParameterIds(applicable, own);
				var enteredCount = applicable.Count(p => enteredIds.Contains(p.Id));

				list.Add(new SampleContext
				{
					Parameters = applicable,
					Results = own,
					FarmerMobile = i.Mobile,
					CollectedOn = i.CollectionDate,
					Row = new LabSampleRowDto
					{
						SampleItemId = i.Id,
						CollectionId = i.CollectionId,
						SampleId = $"SAS-{TypeToken(i.SampleType)}-{counters[i.SampleType]:000}",
						SampleCode = i.Code,
						SampleType = i.SampleType,
						PaymentType = i.PaymentType,
						FarmerName = i.FarmerName,
						Crop = i.Crop1,
						Village = i.Village,
						District = i.DistrictName,
						CurrentParameter = applicable.LastOrDefault(p => enteredIds.Contains(p.Id))?.Name,
						Status = i.AnalysisStatus,
						ReportGenerated = batch.Status == SampleBatchStatus.Completed,
						ParameterCount = applicable.Count,
						EnteredCount = enteredCount,
						ProgressPercent = applicable.Count == 0 ? 0 : (int)Math.Round(enteredCount * 100.0 / applicable.Count, MidpointRounding.AwayFromZero),
						AnalysisStartedAt = i.AnalysisStartedAt,
						AnalysisCompletedAt = i.AnalysisCompletedAt
					}
				});
			}

			return list;
		}

		/// <summary>Phase 1e (analyst entry form): copies ValueType, Options and IsDerived from the LabParameter
		/// master onto the parameter rows, so the page renders a select for Text rows (Texture) and a read-only
		/// cell for derived rows (Organic Matter) without knowing parameter codes.</summary>
		private static void StampEntryFields(IEnumerable<LabParameterRowDto> rows, IReadOnlyList<LabParameter> parameters)
		{
			var byId = parameters.ToDictionary(p => p.Id);
			foreach (var row in rows)
			{
				if (!byId.TryGetValue(row.LabParameterId, out var p)) continue;
				row.ValueType = p.ValueType;
				row.IsDerived = !LabAutoResultEngine.IsEnterable(p);
				row.Options = string.IsNullOrWhiteSpace(p.Options)
					? new List<string>()
					: p.Options.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			}
		}

		/// <summary>The sample's parameter rows with stored values, auto result and per-row entry status.</summary>
		private LabSampleParametersDto ParameterRows(SampleContext sample) =>
			new() { Sample = sample.Row, Parameters = Evaluate(sample).Parameters };

		private LabAutoResultDto Evaluate(SampleContext sample)
		{
			var values = new Dictionary<int, string?>();
			var byParameter = new Dictionary<int, SampleLabResult>();

			foreach (var p in sample.Parameters)
			{
				var stored = sample.Results.LastOrDefault(r => r.LabParameterId == p.Id)
					?? sample.Results.LastOrDefault(r => r.LabParameterId == null && string.Equals(r.Parameter, p.Name, StringComparison.OrdinalIgnoreCase));
				if (stored == null || string.IsNullOrWhiteSpace(stored.EnteredValue)) continue;
				values[p.Id] = stored.EnteredValue;
				byParameter[p.Id] = stored;
			}

			var result = _engine.Evaluate(sample.Row.SampleType, sample.Row.Crop, sample.Parameters, values).Result;
			StampEntryFields(result.Parameters, sample.Parameters);

			foreach (var row in result.Parameters)
			{
				if (row.EnteredValue == null) continue;
				row.RowStatus = sample.Row.Status == SampleAnalysisStatus.Completed ? SampleAnalysisStatus.Completed : SampleAnalysisStatus.InProgress;
				if (byParameter.TryGetValue(row.LabParameterId, out var stored))
				{
					row.EnteredAt = stored.EnteredAt;
					row.EnteredByName = stored.EnteredByName;
				}
				else
				{
					// derived rows carry their source's stamp
					var any = byParameter.Values.FirstOrDefault();
					row.EnteredAt = any?.EnteredAt;
					row.EnteredByName = any?.EnteredByName;
				}
			}

			return result;
		}

		private async Task<LabSampleEntryDto> BuildEntryAsync(SampleBatch batch, int itemId)
		{
			var samples = await LoadBatchSamplesAsync(batch);
			var sample = samples.First(s => s.Row.SampleItemId == itemId);
			var row = (await BuildBatchRowsAsync(new List<int> { batch.Id })).First();

			return new LabSampleEntryDto
			{
				Batch = row,
				PendingInBatch = samples.Count(s => s.Row.Status != SampleAnalysisStatus.Completed),
				CompletedInBatch = samples.Count(s => s.Row.Status == SampleAnalysisStatus.Completed),
				SamplesInBatch = samples.Select(s => s.Row).ToList(),
				Sample = sample.Row,
				FarmerMobile = sample.FarmerMobile,
				CollectedOn = sample.CollectedOn,
				IsLocked = batch.Status == SampleBatchStatus.Completed,
				Result = Evaluate(sample)
			};
		}

		private async Task<LabBatchDetailDto> BuildBatchDetailAsync(int id)
		{
			var batch = await _db.SampleBatches.AsNoTracking().FirstAsync(b => b.Id == id);
			var header = (await BuildBatchRowsAsync(new List<int> { id })).First();
			var samples = await LoadBatchSamplesAsync(batch);

			var timeline = await _db.LabActivities.AsNoTracking()
				.Where(a => a.BatchId == id && TimelineKinds.Contains(a.Kind))
				.OrderBy(a => a.At).ThenBy(a => a.Id)
				.ToListAsync();

			var consignmentIds = await _db.SampleConsignments.AsNoTracking()
				.Where(c => c.BatchId == id && !c.IsDeleted)
				.OrderBy(c => c.Code)
				.Select(c => c.Id)
				.ToListAsync();

			var completedBy = timeline.LastOrDefault(a => a.Kind == LabActivityKind.StatusUpdated &&
				(a.Description ?? "").StartsWith("Status updated to Completed", StringComparison.Ordinal))?.ByName;

			var steps = new List<LabBatchStepDto>
			{
				new()
				{
					Status = SampleBatchStatus.TakenForAnalysis, Title = "Taken for Analysis", At = batch.TakenForAnalysisAt,
					ByName = batch.AssignedByName,
					Description = batch.AssignedToName == null ? "Waiting for an analyst" : $"Assigned to {batch.AssignedToName} for analysis"
				},
				new()
				{
					Status = SampleBatchStatus.InProgress, Title = "In Progress", At = batch.AnalysisStartedAt,
					ByName = batch.AnalysisStartedAt == null ? null : batch.AssignedToName, Description = "Test value entry started"
				},
				new()
				{
					Status = SampleBatchStatus.AnalysisCompleted, Title = "Analysis Completed", At = batch.AnalysisCompletedAt,
					ByName = batch.AnalysisCompletedAt == null ? null : batch.AssignedToName, Description = "All samples analysed; auto results ready"
				},
				new()
				{
					Status = SampleBatchStatus.Completed, Title = "Completed", At = batch.CompletedAt,
					ByName = completedBy, Description = "Batch completed and report generated"
				}
			};

			foreach (var step in steps) step.IsDone = step.At != null;
			var current = steps.FindLastIndex(s => s.IsDone);
			steps[current < 0 ? 0 : current].IsCurrent = true;

			var recent = samples
				.OrderByDescending(s => s.Row.AnalysisCompletedAt ?? s.Row.AnalysisStartedAt ?? DateTime.MinValue)
				.ThenBy(s => samples.IndexOf(s))
				.Take(5)
				.Select(s => s.Row)
				.ToList();

			return new LabBatchDetailDto
			{
				Header = header,
				Remarks = batch.Remarks,
				FinalRemarks = batch.FinalRemarks,
				CreatedByName = batch.CreatedByName,
				CreatedAt = batch.CreatedAt,
				TakenForAnalysisAt = batch.TakenForAnalysisAt,
				AnalysisStartedAt = batch.AnalysisStartedAt,
				AnalysisCompletedAt = batch.AnalysisCompletedAt,
				CompletedAt = batch.CompletedAt,
				ParameterRowCount = samples.Sum(s => s.Parameters.Count),
				DocumentCount = await _db.LabDocuments.CountAsync(d => d.BatchId == id && !d.IsDeleted),
				Progress = steps,
				SampleTypeSummary = samples
					.GroupBy(s => s.Row.SampleType)
					.OrderBy(g => g.Key)
					.Select(g => new LabSampleTypeSummaryDto
					{
						SampleType = g.Key,
						Total = g.Count(),
						Completed = g.Count(s => s.Row.Status == SampleAnalysisStatus.Completed),
						InProgress = g.Count(s => s.Row.Status == SampleAnalysisStatus.InProgress),
						NotStarted = g.Count(s => s.Row.Status == SampleAnalysisStatus.NotStarted)
					})
					.ToList(),
				RecentSamples = recent,
				Timeline = timeline.Select(MapActivity).ToList(),
				Consignments = await BuildConsignmentRowsAsync(consignmentIds)
			};
		}

		private async Task<List<LabBatchRowDto>> BuildBatchRowsAsync(List<int> ids)
		{
			if (ids.Count == 0) return new List<LabBatchRowDto>();

			var batches = await _db.SampleBatches.AsNoTracking().Where(b => ids.Contains(b.Id)).ToListAsync();

			var consignments = await _db.SampleConsignments.AsNoTracking()
				.Where(c => c.BatchId != null && ids.Contains(c.BatchId.Value) && !c.IsDeleted)
				.OrderBy(c => c.Code)
				.Select(c => new { BatchId = c.BatchId!.Value, c.Code })
				.ToListAsync();

			var items = await ItemsOfBatches(ids)
				.Select(i => new { BatchId = i.Collection!.Consignment!.BatchId!.Value, i.AnalysisStatus, i.Collection.PaymentType })
				.ToListAsync();

			var reports = await _db.LabReports.AsNoTracking()
				.Where(r => ids.Contains(r.BatchId))
				.GroupBy(r => r.BatchId)
				.Select(g => new { BatchId = g.Key, FirstId = g.Min(r => r.Id) })
				.ToListAsync();

			var now = DateTime.Now;
			var rows = new List<LabBatchRowDto>();

			foreach (var id in ids)
			{
				var b = batches.FirstOrDefault(x => x.Id == id);
				if (b == null) continue;

				var own = items.Where(i => i.BatchId == id).ToList();
				var days = AnalysisDays(b.TakenForAnalysisAt, b.AnalysisCompletedAt, now);

				rows.Add(new LabBatchRowDto
				{
					Id = b.Id,
					Code = b.Code,
					BatchDate = b.BatchDate,
					ConsignmentCodes = consignments.Where(c => c.BatchId == id).Select(c => c.Code).ToList(),
					SampleCount = b.SampleCount,
					SampleType = own.Any(i => i.PaymentType == SamplePaymentType.Paid) ? SamplePaymentType.Paid : SamplePaymentType.Free,
					AssignedToUserId = b.AssignedToUserId,
					AssignedToName = b.AssignedToName,
					AssignedToAvatarUrl = AvatarUrl(b.AssignedToUserId),
					AssignedAt = b.AssignedAt,
					AssignedByName = b.AssignedByName,
					Priority = b.Priority,
					Status = b.Status,
					AnalysisDays = days,
					IsDelayed = IsDelayed(b.Status, days),
					PendingEntries = own.Count(i => i.AnalysisStatus != SampleAnalysisStatus.Completed),
					CompletedEntries = own.Count(i => i.AnalysisStatus == SampleAnalysisStatus.Completed),
					ReportId = reports.FirstOrDefault(r => r.BatchId == id)?.FirstId,
					ReportCode = b.ReportCode
				});
			}

			return rows;
		}

		private async Task<List<LabConsignmentRowDto>> BuildConsignmentRowsAsync(List<int> ids)
		{
			if (ids.Count == 0) return new List<LabConsignmentRowDto>();

			var consignments = await _db.SampleConsignments.AsNoTracking()
				.Where(c => ids.Contains(c.Id))
				.Select(c => new
				{
					c.Id,
					c.Code,
					c.DispatchedAt,
					c.DeliveredAt,
					c.Status,
					c.BatchId,
					BatchCode = c.Batch != null ? c.Batch.Code : null,
					BatchStatus = c.Batch != null ? (SampleBatchStatus?)c.Batch.Status : null,
					c.CourierService,
					c.TrackingNumber,
					c.DispatchedByName
				})
				.ToListAsync();

			var collections = await _db.SampleCollections.AsNoTracking()
				.Where(x => !x.IsDeleted && x.ConsignmentId != null && ids.Contains(x.ConsignmentId.Value))
				.OrderBy(x => x.Id)
				.Select(x => new
				{
					ConsignmentId = x.ConsignmentId!.Value,
					x.PaymentType,
					x.HeadquarterId,
					Items = x.Items.Where(i => !i.IsDeleted).Select(i => new { i.SampleType, State = i.Farmer!.StateName }).ToList()
				})
				.ToListAsync();

			var hqIds = collections.Where(x => x.HeadquarterId != null).Select(x => x.HeadquarterId!.Value).Distinct().ToList();
			var locations = await _db.Headquarters.AsNoTracking()
				.Where(h => hqIds.Contains(h.Id))
				.Select(h => new { h.Id, h.HeadquarterName, Region = h.Region!.RegionName, State = h.Region.State!.StateName })
				.ToListAsync();

			var rows = new List<LabConsignmentRowDto>();
			foreach (var id in ids)
			{
				var c = consignments.FirstOrDefault(x => x.Id == id);
				if (c == null) continue;

				var own = collections.Where(x => x.ConsignmentId == id).ToList();
				var types = own.SelectMany(x => x.Items).Select(i => i.SampleType).ToList();
				var hq = own.Where(x => x.HeadquarterId != null)
					.Select(x => locations.FirstOrDefault(l => l.Id == x.HeadquarterId))
					.FirstOrDefault(l => l != null);

				rows.Add(new LabConsignmentRowDto
				{
					Id = c.Id,
					Code = c.Code,
					ShipmentDate = c.DispatchedAt,
					DeliveredAt = c.DeliveredAt,
					SampleType = own.Any(x => x.PaymentType == SamplePaymentType.Paid) ? SamplePaymentType.Paid : SamplePaymentType.Free,
					SoilSamples = types.Count(t => t != SampleType.Water),
					WaterSamples = types.Count(t => t != SampleType.Soil),
					TotalSamples = types.Count,
					StateName = hq?.State ?? own.SelectMany(x => x.Items).Select(i => i.State).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)),
					RegionName = hq?.Region,
					HeadquarterName = hq?.HeadquarterName,
					LabStatus = DeriveLabStatus(c.Status, c.BatchId, c.BatchStatus),
					Status = c.Status,
					BatchId = c.BatchId,
					BatchCode = c.BatchCode,
					CourierService = c.CourierService,
					TrackingNumber = c.TrackingNumber,
					DispatchedByName = c.DispatchedByName
				});
			}

			return rows;
		}

		private async Task<List<LabUserDto>> LoadAnalystsAsync()
		{
			var designationIds = await LabAccess.AnalystDesignationIdsAsync(_db);

			var users = await _db.Users.AsNoTracking()
				.Where(u => (u.DesignationId != null && designationIds.Contains(u.DesignationId.Value)) ||
							u.Role == AppRole.Admin || u.Role == AppRole.CorporateAdmin)
				.Select(u => new { u.Id, u.Name, u.UserName, Designation = u.Designation != null ? u.Designation.Name : null })
				.ToListAsync();

			var open = await _db.SampleBatches.AsNoTracking()
				.Where(b => !b.IsDeleted && b.AssignedToUserId != null && b.Status != SampleBatchStatus.Completed)
				.GroupBy(b => b.AssignedToUserId!)
				.Select(g => new { UserId = g.Key, Count = g.Count() })
				.ToListAsync();

			return users
				.Select(u => new LabUserDto
				{
					UserId = u.Id,
					Name = string.IsNullOrWhiteSpace(u.Name) ? (u.UserName ?? u.Id) : u.Name!,
					DesignationName = u.Designation,
					AvatarUrl = AvatarUrl(u.Id),
					OpenBatches = open.FirstOrDefault(o => o.UserId == u.Id)?.Count ?? 0
				})
				.OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		// ================================================================ transitions

		/// <summary>Created -> TakenForAnalysis: stamps the time, logs the status change and moves
		/// the v1 collections to TestInProgress (caller saves).</summary>
		private async Task TakeForAnalysisAsync(SampleBatch batch, DateTime now)
		{
			batch.Status = SampleBatchStatus.TakenForAnalysis;
			batch.TakenForAnalysisAt = now;
			_activities.StatusUpdated(batch, SampleBatchStatus.TakenForAnalysis, now);

			var collections = await _db.SampleCollections
				.Where(x => !x.IsDeleted && x.Consignment != null && x.Consignment.BatchId == batch.Id)
				.ToListAsync();
			LabV1Sync.MarkTestInProgress(_db, batch.Code, collections, _access.Name, User.Identity?.Name, now);
		}

		// ================================================================ filters

		private List<SampleContext> FilterSamples(List<SampleContext> samples, string? status, string? sampleType,
			string? parameter, string? q, DateTime? from, DateTime? to, out string? error)
		{
			error = null;
			IEnumerable<SampleContext> query = samples;

			if (!string.IsNullOrWhiteSpace(status))
			{
				var statuses = ParseEnums<SampleAnalysisStatus>(status);
				if (statuses == null) { error = "Status must be NotStarted, InProgress or Completed."; return new(); }
				if (statuses.Count > 0) query = query.Where(s => statuses.Contains(s.Row.Status));
			}

			if (!string.IsNullOrWhiteSpace(sampleType))
			{
				var filter = SampleTypeFilter(sampleType);
				if (filter == null) { error = "Sample type must be Soil, Water, SoilAndWater, Free or Paid."; return new(); }
				query = query.Where(s => filter(s.Row));
			}

			if (!string.IsNullOrWhiteSpace(parameter))
			{
				var key = parameter.Trim();
				query = query.Where(s => s.Row.CurrentParameter != null && (
					string.Equals(s.Row.CurrentParameter, key, StringComparison.OrdinalIgnoreCase) ||
					s.Parameters.Any(p => p.Name == s.Row.CurrentParameter && (p.Code.Equals(key, StringComparison.OrdinalIgnoreCase) || p.Id.ToString() == key))));
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim();
				query = query.Where(s => Has(s.Row.SampleId, term) || Has(s.Row.SampleCode, term) || Has(s.Row.FarmerName, term) ||
					Has(s.Row.Crop, term) || Has(s.Row.Village, term));
			}

			if (from.HasValue) query = query.Where(s => s.CollectedOn >= from.Value.Date);
			if (to.HasValue) query = query.Where(s => s.CollectedOn < to.Value.Date.AddDays(1));

			return query.ToList();
		}

		private List<LabSampleParametersDto> FilterParameterRows(List<SampleContext> samples, string? status, string? sampleType,
			string? parameter, string? q, out string? error)
		{
			error = null;
			IEnumerable<SampleContext> query = samples;

			if (!string.IsNullOrWhiteSpace(sampleType))
			{
				var filter = SampleTypeFilter(sampleType);
				if (filter == null) { error = "Sample type must be Soil, Water, SoilAndWater, Free or Paid."; return new(); }
				query = query.Where(s => filter(s.Row));
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim();
				query = query.Where(s => Has(s.Row.SampleId, term) || Has(s.Row.SampleCode, term) || Has(s.Row.FarmerName, term));
			}

			List<SampleAnalysisStatus>? statuses = null;
			if (!string.IsNullOrWhiteSpace(status))
			{
				statuses = ParseEnums<SampleAnalysisStatus>(status);
				if (statuses == null) { error = "Status must be NotStarted, InProgress or Completed."; return new(); }
			}

			var key = parameter?.Trim();
			var rowFilter = (statuses != null && statuses.Count > 0) || !string.IsNullOrWhiteSpace(key);
			var result = new List<LabSampleParametersDto>();

			foreach (var sample in query)
			{
				var dto = ParameterRows(sample);
				if (rowFilter)
				{
					dto.Parameters = dto.Parameters.Where(p =>
						(statuses == null || statuses.Count == 0 || statuses.Contains(p.RowStatus)) &&
						(string.IsNullOrWhiteSpace(key) || p.Name.Equals(key, StringComparison.OrdinalIgnoreCase) ||
						 p.Code.Equals(key, StringComparison.OrdinalIgnoreCase) || p.LabParameterId.ToString() == key)).ToList();
					if (dto.Parameters.Count == 0) continue;
				}
				result.Add(dto);
			}

			return result;
		}

		private static Func<LabSampleRowDto, bool>? SampleTypeFilter(string raw)
		{
			if (TryParseEnum<SampleType>(raw, out var type) && !int.TryParse(raw.Trim(), out _))
				return s => s.SampleType == type;
			if (TryParseEnum<SamplePaymentType>(raw, out var payment) && !int.TryParse(raw.Trim(), out _))
				return s => s.PaymentType == payment;
			if (TryParseEnum<SampleType>(raw, out type))
				return s => s.SampleType == type;
			return null;
		}

		private static IQueryable<SampleConsignment> WhereLabStatus(IQueryable<SampleConsignment> query, List<LabConsignmentStatus> statuses)
		{
			var inTransit = statuses.Contains(LabConsignmentStatus.InTransit);
			var pending = statuses.Contains(LabConsignmentStatus.BatchPending);
			var created = statuses.Contains(LabConsignmentStatus.BatchCreated);
			var completed = statuses.Contains(LabConsignmentStatus.Completed);

			return query.Where(c =>
				(inTransit && (c.Status == ConsignmentStatus.PendingPickup || c.Status == ConsignmentStatus.Dispatched || c.Status == ConsignmentStatus.InTransit)) ||
				(pending && c.Status == ConsignmentStatus.Delivered && c.BatchId == null) ||
				(created && c.BatchId != null && c.Status != ConsignmentStatus.Completed && c.Batch!.Status != SampleBatchStatus.Completed) ||
				(completed && (c.Status == ConsignmentStatus.Completed || (c.BatchId != null && c.Batch!.Status == SampleBatchStatus.Completed))));
		}

		/// <summary>A consignment is in a state when one of its collections was collected at a
		/// headquarter of that state, or one of its farmers lives there.</summary>
		private System.Linq.Expressions.Expression<Func<SampleConsignment, bool>> ConsignmentInState(int stateId) =>
			c => c.Collections.Any(x => !x.IsDeleted && (
				(x.HeadquarterId != null && _db.Headquarters.Any(h => h.Id == x.HeadquarterId && h.Region!.StateId == stateId)) ||
				x.Items.Any(i => !i.IsDeleted && i.Farmer!.StateId == stateId)));

		// ================================================================ scoping

		private IQueryable<SampleBatch> ScopedBatches()
		{
			var query = _db.SampleBatches.AsNoTracking().Where(b => !b.IsDeleted);
			if (_access.AnalystOnly)
			{
				var me = _access.UserId;
				query = query.Where(b => b.AssignedToUserId == me);
			}
			return query;
		}

		private IQueryable<SampleConsignment> ScopedConsignments()
		{
			var query = _db.SampleConsignments.AsNoTracking().Where(c => !c.IsDeleted);
			if (_access.AnalystOnly)
			{
				var me = _access.UserId;
				query = query.Where(c => c.BatchId != null && !c.Batch!.IsDeleted && c.Batch.AssignedToUserId == me);
			}
			return query;
		}

		private IQueryable<SampleItem> ItemsOfBatches(List<int> batchIds) =>
			_db.SampleItems.AsNoTracking().Where(i => !i.IsDeleted && !i.Collection!.IsDeleted &&
				i.Collection.Consignment != null && !i.Collection.Consignment.IsDeleted &&
				i.Collection.Consignment.BatchId != null && batchIds.Contains(i.Collection.Consignment.BatchId.Value));

		private async Task<int?> BatchIdOfItemAsync(int itemId) =>
			await _db.SampleItems.AsNoTracking()
				.Where(i => i.Id == itemId && !i.IsDeleted && !i.Collection!.IsDeleted && i.Collection.Consignment != null && !i.Collection.Consignment.IsDeleted)
				.Select(i => i.Collection!.Consignment!.BatchId)
				.FirstOrDefaultAsync();

		private async Task<string> DisplayIdAsync(int batchId, SampleItem item)
		{
			var sameType = await ItemsOfBatches(new List<int> { batchId })
				.Where(i => i.SampleType == item.SampleType)
				.Select(i => new { i.Id, i.Collection!.Code })
				.ToListAsync();

			var index = sameType.OrderBy(i => i.Code, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.Id).ToList().FindIndex(i => i.Id == item.Id) + 1;
			return $"SAS-{TypeToken(item.SampleType)}-{Math.Max(index, 1):000}";
		}

		private List<LabParameter>? _parameters;

		private async Task<List<LabParameter>> ActiveParametersAsync() =>
			_parameters ??= await _db.LabParameters.AsNoTracking().Where(p => p.IsActive).ToListAsync();

		private async Task<ObjectResult?> RequireReadAsync()
		{
			await _access.LoadAsync(User);
			return _access.CanRead ? null : Forbid403("You do not have access to the lab portal.");
		}

		private async Task<ObjectResult?> RequireWriteAsync(string message)
		{
			await _access.LoadAsync(User);
			if (!_access.CanRead) return Forbid403("You do not have access to the lab portal.");
			return _access.CanWrite ? null : Forbid403(message);
		}

		// ================================================================ small helpers

		public static int FinancialYearStart(DateTime date) => date.Month >= 4 ? date.Year : date.Year - 1;

		/// <summary>Whole days from TakenForAnalysisAt to AnalysisCompletedAt (or now); null = Pending.</summary>
		public static int? AnalysisDays(DateTime? takenAt, DateTime? completedAt, DateTime now)
		{
			if (takenAt == null) return null;
			var days = (int)Math.Floor(((completedAt ?? now) - takenAt.Value).TotalDays);
			return days < 0 ? 0 : days;
		}

		private bool IsDelayed(SampleBatchStatus status, int? days) =>
			status != SampleBatchStatus.Completed && days.HasValue && days.Value > DelayedAfterDays;

		private static LabConsignmentStatus DeriveLabStatus(ConsignmentStatus status, int? batchId, SampleBatchStatus? batchStatus)
		{
			if (status == ConsignmentStatus.Completed || batchStatus == SampleBatchStatus.Completed) return LabConsignmentStatus.Completed;
			if (batchId != null) return LabConsignmentStatus.BatchCreated;
			if (status == ConsignmentStatus.Delivered) return LabConsignmentStatus.BatchPending;
			return LabConsignmentStatus.InTransit;
		}

		private static string TypeToken(SampleType type) => type switch
		{
			SampleType.Soil => "SOIL",
			SampleType.Water => "WATER",
			_ => "SW"
		};

		private static string SampleStatusText(SampleAnalysisStatus status) => status switch
		{
			SampleAnalysisStatus.NotStarted => "Not Started",
			SampleAnalysisStatus.InProgress => "In Progress",
			_ => "Completed"
		};

		private static HashSet<int> EnteredParameterIds(IReadOnlyList<LabParameter> applicable, List<SampleLabResult> results)
		{
			var set = new HashSet<int>();
			foreach (var r in results.Where(r => !string.IsNullOrWhiteSpace(r.EnteredValue)))
			{
				if (r.LabParameterId.HasValue) { set.Add(r.LabParameterId.Value); continue; }
				var match = applicable.FirstOrDefault(p => string.Equals(p.Name, r.Parameter, StringComparison.OrdinalIgnoreCase) && !set.Contains(p.Id));
				if (match != null) set.Add(match.Id);
			}
			return set;
		}

		private static Dictionary<int, string?> ValueMap(LabSampleValuesDto dto)
		{
			var map = new Dictionary<int, string?>();
			foreach (var v in dto.Values ?? new List<LabSampleValueDto>())
				if (v != null) map[v.LabParameterId] = v.Value;
			return map;
		}

		private Dictionary<string, string>? _avatars;

		/// <summary>Community avatars (Uploads/Community/avatars/{userId}.{ext}), served by api/Community/file.</summary>
		private string? AvatarUrl(string? userId)
		{
			if (string.IsNullOrWhiteSpace(userId)) return null;

			if (_avatars == null)
			{
				_avatars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				var folder = Path.Combine(GetUploadsRoot(), "Community", "avatars");
				try
				{
					if (Directory.Exists(folder))
					{
						foreach (var file in Directory.EnumerateFiles(folder))
						{
							var key = Path.GetFileNameWithoutExtension(file);
							if (!string.IsNullOrWhiteSpace(key))
								_avatars[key] = $"api/Community/file/Community/avatars/{Path.GetFileName(file)}";
						}
					}
				}
				catch (Exception)
				{
					// an unreadable folder simply means "no avatars"
				}
			}

			return _avatars.TryGetValue(userId, out var url) ? url : null;
		}

		private LabDocumentDto MapDocument(LabDocument d) => new()
		{
			Id = d.Id,
			BatchId = d.BatchId,
			Kind = d.Kind,
			FileName = d.FileName,
			Description = d.Description,
			Extension = Path.GetExtension(d.FileName).TrimStart('.').ToUpperInvariant(),
			Url = $"api/Sas/file/{d.StoredPath}",
			ContentType = d.ContentType,
			Size = d.Size,
			UploadedByName = d.UploadedByName,
			UploadedByAvatarUrl = AvatarUrl(d.UploadedByUserId),
			CreatedAt = d.CreatedAt
		};

		private LabActivityDto MapActivity(LabActivity a) => new()
		{
			Id = a.Id,
			Kind = a.Kind,
			Title = a.Title,
			Description = a.Description,
			ByName = a.ByName,
			ByRole = a.ByRole,
			ByAvatarUrl = AvatarUrl(a.ByUserId),
			At = a.At
		};

		private static bool Has(string? value, string term) =>
			value != null && value.Contains(term, StringComparison.OrdinalIgnoreCase);

		private static (int Page, int PageSize) Paging(int page, int pageSize) =>
			(page < 1 ? 1 : page, pageSize < 1 ? DefaultPageSize : pageSize > MaxPageSize ? MaxPageSize : pageSize);

		private static PageResult<T> Page<T>(IEnumerable<T> source, int page, int pageSize)
		{
			var list = source as IList<T> ?? source.ToList();
			return new PageResult<T>
			{
				Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
				Total = list.Count,
				Page = page,
				PageSize = pageSize
			};
		}

		private static void WriteHeader(IXLWorksheet sheet, string[] headers)
		{
			for (var c = 0; c < headers.Length; c++)
			{
				var cell = sheet.Cell(1, c + 1);
				cell.Value = headers[c];
				cell.Style.Font.Bold = true;
				cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
			}
			sheet.SheetView.FreezeRows(1);
		}

		private static void SetWidths(IXLWorksheet sheet, params double[] widths)
		{
			for (var c = 0; c < widths.Length; c++)
				sheet.Column(c + 1).Width = widths[c];
		}

		private FileContentResult Xlsx(XLWorkbook workbook, string fileName)
		{
			using var stream = new MemoryStream();
			workbook.SaveAs(stream);
			return File(stream.ToArray(), XlsxType, fileName);
		}

		private string GetUploadsRoot() => Path.Combine(_env.ContentRootPath, "Uploads");

		private ObjectResult Forbid403(string message) => StatusCode(403, new { Success = false, Message = message });

		private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

		private static bool TryParseEnum<TEnum>(string? raw, out TEnum value) where TEnum : struct, Enum
		{
			value = default;
			if (string.IsNullOrWhiteSpace(raw)) return false;
			if (Enum.TryParse(raw.Trim(), true, out value) && Enum.IsDefined(typeof(TEnum), value)) return true;
			value = default;
			return false;
		}

		/// <summary>Comma separated enum names or integers; empty -> empty list; any unknown -> null.</summary>
		private static List<TEnum>? ParseEnums<TEnum>(string? raw) where TEnum : struct, Enum
		{
			var list = new List<TEnum>();
			if (string.IsNullOrWhiteSpace(raw)) return list;
			foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (!TryParseEnum<TEnum>(token, out var value)) return null;
				list.Add(value);
			}
			return list;
		}
	}

	/// <summary>
	/// Lets an [AllowAnonymous] download action accept the caller's JWT as ?access_token= (the
	/// client opens exports with &lt;a download&gt;, see LabApi.AuthorizedUrl). A request that already
	/// carries a Bearer header passes through; otherwise the query token is validated with the
	/// API's own JwtBearer options and becomes the request user. Without a valid token: 401.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	internal sealed class LabQueryTokenAttribute : Attribute, IAsyncAuthorizationFilter
	{
		public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
		{
			var http = context.HttpContext;
			if (http.User?.Identity?.IsAuthenticated == true) return;

			var token = http.Request.Query["access_token"].ToString();
			if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) token = token["Bearer ".Length..];

			if (!string.IsNullOrWhiteSpace(token))
			{
				var options = http.RequestServices.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
					.Get(JwtBearerDefaults.AuthenticationScheme);

				foreach (var handler in options.TokenHandlers)
				{
					try
					{
						var result = await handler.ValidateTokenAsync(token, options.TokenValidationParameters);
						if (result.IsValid && result.ClaimsIdentity != null)
						{
							http.User = new ClaimsPrincipal(result.ClaimsIdentity);
							return;
						}
					}
					catch (Exception)
					{
						// try the next handler; an invalid token ends in 401 below
					}
				}
			}

			context.Result = new UnauthorizedObjectResult(new { Success = false, Message = "Sign in again to download this file." });
		}
	}
}
