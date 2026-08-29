using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SDWADashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SDWADashboardController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("dealer-details")]
        public async Task<ActionResult<SDWADashboardDealerDto>> GetDealerDetails()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var dealer = await _db.DealerRegistrations
                .FirstOrDefaultAsync(d => d.UserTableId == userId);

            var regionName = string.Empty;
            var stateName = string.Empty;
            var districtName = string.Empty;
            var hqName = string.Empty;
            var phone = string.Empty;
            var address = string.Empty;

            if (dealer != null)
            {
                if (dealer.Region > 0)
                {
                    var region = await _db.Regions.FirstOrDefaultAsync(r => r.Id == dealer.Region);
                    regionName = region?.RegionName ?? string.Empty;
                }

                if (dealer.StateId > 0)
                {
                    var state = await _db.States.FirstOrDefaultAsync(s => s.Id == dealer.StateId);
                    stateName = state?.StateName ?? string.Empty;
                }

                if (dealer.DistrictId.HasValue && dealer.DistrictId.Value > 0)
                {
                    var district = await _db.Districts.FirstOrDefaultAsync(d => d.Id == dealer.DistrictId.Value);
                    districtName = district?.DistrictName ?? string.Empty;
                }

                if (dealer.HQ > 0)
                {
                    var hq = await _db.Headquarters.FirstOrDefaultAsync(h => h.Id == dealer.HQ);
                    hqName = hq?.HeadquarterName ?? string.Empty;
                }

                phone = dealer.OfficialContactNumber ?? string.Empty;

                var addrParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(dealer.ShopNoORRoomNoOrBlockNo)) addrParts.Add(dealer.ShopNoORRoomNoOrBlockNo);
                if (!string.IsNullOrWhiteSpace(dealer.Street)) addrParts.Add(dealer.Street);
                if (!string.IsNullOrWhiteSpace(dealer.Village)) addrParts.Add(dealer.Village);
                if (!string.IsNullOrWhiteSpace(dealer.Taluk)) addrParts.Add(dealer.Taluk);
                if (!string.IsNullOrWhiteSpace(dealer.PinCode)) addrParts.Add(dealer.PinCode);
                address = string.Join(", ", addrParts);
            }

            var dto = new SDWADashboardDealerDto
            {
                DealerId = dealer?.Id ?? 0,
                DealerName = user.Name ?? string.Empty,
                DealerCode = dealer?.SPICCode ?? dealer?.DealerCode ?? string.Empty,
                Region = regionName,
                State = stateName,
                District = districtName,
                HQ = hqName,
                Email = user.Email ?? string.Empty,
                Phone = phone,
                Address = address,
                ProfileCompletion = CalculateProfileCompletion(dealer),
                EntityType = dealer?.EntityType?.ToString()
            };

            return Ok(dto);
        }

        [HttpGet("welfare/eligibility")]
        public async Task<ActionResult<EligibilityResultDto>> GetWelfareEligibility([FromQuery] string? scheme = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var dealer = await _db.DealerRegistrations
                .FirstOrDefaultAsync(d => d.UserTableId == userId);

            if (dealer == null)
                return NotFound(new { Message = "Dealer registration not found." });

            if (dealer.Status != DealerStatus.Active)
            {
                return Ok(new EligibilityResultDto
                {
                    IsEligible = false,
                    DealerName = dealer.FirmName ?? string.Empty,
                    DealerCode = dealer.SPICCode ?? dealer.DealerCode ?? string.Empty,
                    SchemeName = scheme ?? string.Empty,
                    Criteria = new List<EligibilityCriterionDto>
                    {
                        new EligibilityCriterionDto
                        {
                            Name = "Dealer Status",
                            Required = "Active",
                            Actual = dealer.Status.ToString(),
                            IsSatisfied = false
                        }
                    }
                });
            }

            var state = await _db.States.FirstOrDefaultAsync(s => s.Id == dealer.StateId);
            var stateName = state?.StateName ?? "Unknown";
            var stateGroup = DetermineStateGroup(stateName);
            var dealerType = dealer.EntityType?.ToString() ?? "Unknown";

            var result = new EligibilityResultDto
            {
                DealerName = dealer.FirmName ?? string.Empty,
                DealerCode = dealer.SPICCode ?? dealer.DealerCode ?? string.Empty,
                State = stateName,
                StateGroup = stateGroup,
                DealerType = dealerType,
                SchemeName = scheme ?? string.Empty,
                Criteria = new List<EligibilityCriterionDto>()
            };

            ValidateDealershipDuration(dealer, result);
            ValidateStateConfigured(stateGroup, result);

            if (stateGroup != "Other")
            {
                ValidateLiftingCriteria(dealer, stateGroup, dealerType, result);
            }

            result.IsEligible = result.Criteria.All(c => c.IsSatisfied);
            return Ok(result);
        }

        private static string DetermineStateGroup(string stateName)
        {
            var name = stateName.Trim().ToUpperInvariant();

            if (name.Contains("TAMIL NADU") || name.Contains("PUDUCHERRY") || name.Contains("PONDICHERRY"))
                return "TN/PY";
            if (name.Contains("KERALA"))
                return "KL";
            if (name.Contains("ANDHRA") || name.Contains("TELANGANA") || name.Contains("KARNATAKA"))
                return "AP/TS/KA";
            if (name.Contains("MAHARASHTRA"))
                return "MH";

            return "Other";
        }

        private static void ValidateDealershipDuration(DealerRegistration dealer, EligibilityResultDto result)
        {
            var today = DateTime.UtcNow.Date;

            var effectiveAppointmentDate = GetEarliestAppointmentDate(
                dealer.DateOfAppointment,
                dealer.GreenstarDateOfAppointment);

            if (!effectiveAppointmentDate.HasValue)
            {
                result.Criteria.Add(new EligibilityCriterionDto
                {
                    Name = "Dealership Duration",
                    Required = "Minimum 1 year",
                    Actual = "Dealership appointment date not available",
                    IsSatisfied = false
                });
                return;
            }

            var appointmentDate = effectiveAppointmentDate.Value;
            var duration = today - appointmentDate;
            var totalDays = duration.TotalDays;
            var years = (int)(totalDays / 365);
            var remainingDays = (int)(totalDays % 365);
            var months = remainingDays / 30;

            string actualText;
            if (years > 0)
                actualText = months > 0 ? $"{years} year(s), {months} month(s)" : $"{years} year(s)";
            else if (months > 0)
                actualText = $"{months} month(s)";
            else
                actualText = $"{(int)totalDays} day(s)";

            result.Criteria.Add(new EligibilityCriterionDto
            {
                Name = "Dealership Duration",
                Required = "Minimum 1 year",
                Actual = actualText,
                IsSatisfied = totalDays >= 365
            });
        }

        private static DateTime? GetEarliestAppointmentDate(DateTime spicDate, DateTime? greenstarDate)
        {
            var candidates = new List<DateTime>();

            if (spicDate != default && spicDate.Date <= DateTime.UtcNow.Date)
                candidates.Add(spicDate.Date);

            if (greenstarDate.HasValue && greenstarDate.Value != default && greenstarDate.Value.Date <= DateTime.UtcNow.Date)
                candidates.Add(greenstarDate.Value.Date);

            return candidates.Count == 0 ? null : candidates.Min();
        }

        private static void ValidateStateConfigured(string stateGroup, EligibilityResultDto result)
        {
            if (stateGroup == "Other")
            {
                result.Criteria.Add(new EligibilityCriterionDto
                {
                    Name = "State Eligibility Configuration",
                    Required = "Configured state-wise eligibility criteria",
                    Actual = "Not configured for this state",
                    IsSatisfied = false
                });
            }
        }

        private static void ValidateLiftingCriteria(
            DealerRegistration dealer,
            string stateGroup,
            string dealerType,
            EligibilityResultDto result)
        {
            if (dealerType == "soleProprietor")
            {
                ValidateProprietorshipLifting(stateGroup, result);
            }
            else if (dealerType == "Partnership")
            {
                ValidatePartnershipLifting(stateGroup, result);
            }
            else if (dealerType == "LLP" || dealerType == "PvtLtd" || dealerType == "PubLtd" || dealerType == "Society")
            {
                result.Criteria.Add(new EligibilityCriterionDto
                {
                    Name = "Dealer Constitution",
                    Required = "Proprietorship or Partnership",
                    Actual = dealerType,
                    IsSatisfied = false
                });
            }
        }

        private static void ValidateProprietorshipLifting(string stateGroup, EligibilityResultDto result)
        {
            switch (stateGroup)
            {
                case "TN/PY":
                    result.Criteria.Add(new EligibilityCriterionDto
                    {
                        Name = "Proprietorship Lifting (TN/PY)",
                        Required = "3-year avg: 100 Tons fertilizer OR 3 Lakhs TO specialty products",
                        Actual = "Verify from sales records",
                        IsSatisfied = true
                    });
                    break;
                case "KL":
                    result.Criteria.Add(new EligibilityCriterionDto
                    {
                        Name = "Proprietorship Lifting (KL)",
                        Required = "100 Tons fertilizer OR 3 Lakhs TO specialty products",
                        Actual = "Verify from sales records",
                        IsSatisfied = true
                    });
                    break;
                case "AP/TS/KA":
                    result.Criteria.Add(new EligibilityCriterionDto
                    {
                        Name = "Proprietorship Lifting (AP/TS/KA)",
                        Required = "3-year avg: 100 Tons fertilizer OR 3 Lakhs TO specialty products",
                        Actual = "Verify from sales records",
                        IsSatisfied = true
                    });
                    break;
                case "MH":
                    result.Criteria.Add(new EligibilityCriterionDto
                    {
                        Name = "Proprietorship Lifting (MH)",
                        Required = "200 Tons fertilizer OR 7 Lakhs TO specialty products",
                        Actual = "Verify from sales records",
                        IsSatisfied = true
                    });
                    break;
            }
        }

        private static void ValidatePartnershipLifting(string stateGroup, EligibilityResultDto result)
        {
            switch (stateGroup)
            {
                case "TN/PY":
                    result.Criteria.Add(new EligibilityCriterionDto
                    {
                        Name = "Partnership Firm Lifting (TN/PY)",
                        Required = "3-year avg: 1,000 Tons fertilizer OR 30 Lakhs TO specialty products",
                        Actual = "Verify from sales records",
                        IsSatisfied = true
                    });
                    break;
                case "KL":
                    result.Criteria.Add(new EligibilityCriterionDto
                    {
                        Name = "Partnership Firm Lifting (KL)",
                        Required = "1,000 Tons fertilizer OR 30 Lakhs TO specialty products",
                        Actual = "Verify from sales records",
                        IsSatisfied = true
                    });
                    break;
                case "AP/TS/KA":
                    result.Criteria.Add(new EligibilityCriterionDto
                    {
                        Name = "Partnership Firm Lifting (AP/TS/KA)",
                        Required = "3-year avg: 1,000 Tons fertilizer OR 30 Lakhs TO specialty products",
                        Actual = "Verify from sales records",
                        IsSatisfied = true
                    });
                    break;
                case "MH":
                    result.Criteria.Add(new EligibilityCriterionDto
                    {
                        Name = "Partnership Firm Lifting (MH)",
                        Required = "3-year avg: 1,000 Tons fertilizer OR 30 Lakhs TO specialty products",
                        Actual = "Verify from sales records",
                        IsSatisfied = true
                    });
                    break;
            }
        }

        private static int CalculateProfileCompletion(DealerRegistration? dealer)
        {
            if (dealer == null)
                return 0;

            int filled = 0;
            int total = 10;

            if (!string.IsNullOrWhiteSpace(dealer.FirmName)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.SPICCode) || !string.IsNullOrWhiteSpace(dealer.DealerCode)) filled++;
            if (dealer.StateId > 0) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.OfficialContactNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.WhatsAppNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.GSTNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.PANNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.AadhaarNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.AccountHolderName) && !string.IsNullOrWhiteSpace(dealer.AccountNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.Village) && !string.IsNullOrWhiteSpace(dealer.PinCode)) filled++;

            return (int)Math.Round((double)filled / total * 100);
        }

        [HttpGet("dealer-sales-history")]
        public async Task<ActionResult<DealerSalesHistoryDto>> GetDealerSalesHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var dealer = await _db.DealerRegistrations
                .FirstOrDefaultAsync(d => d.UserTableId == userId);

            if (dealer == null)
                return NotFound(new { Message = "Dealer not found." });

            var today = DateTime.Today;
            int currentFyStartYear = today.Month >= 4 ? today.Year : today.Year - 1;

            var wantedStartYears = new[]
            {
                currentFyStartYear - 3,
                currentFyStartYear - 2,
                currentFyStartYear - 1
            };

            var completedFYs = await _db.FinancialYears
                .AsNoTracking()
                .Where(fy => wantedStartYears.Contains(fy.StartDate.Year))
                .OrderBy(fy => fy.StartDate)
                .ToListAsync();

            if (completedFYs.Count < 3)
            {
                var fallback = await _db.FinancialYears
                    .AsNoTracking()
                    .Where(fy => fy.EndDate < today)
                    .OrderByDescending(fy => fy.EndDate)
                    .Take(3)
                    .OrderBy(fy => fy.StartDate)
                    .ToListAsync();

                if (fallback.Count >= 3)
                    completedFYs = fallback;
            }

            var fyIds = completedFYs.Select(fy => fy.Id).ToList();

            var salesData = await _db.DealerCreditLimitSalesData
                .AsNoTracking()
                .Where(x => x.CustomerId == dealer.Id && fyIds.Contains(x.FinancialYearId))
                .GroupBy(x => new { x.FinancialYearId })
                .Select(g => new DealerSalesYearlyDto
                {
                    FinancialYearId = g.Key.FinancialYearId,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalGrossAmount = g.Sum(x => x.GrossAmount)
                })
                .ToListAsync();

            foreach (var yearly in salesData)
            {
                var fy = completedFYs.FirstOrDefault(f => f.Id == yearly.FinancialYearId);
                yearly.FinancialYearName = fy?.Name ?? $"FY-{yearly.FinancialYearId}";
            }

            decimal totalQty = salesData.Sum(x => x.TotalQuantity);
            int yearCount = Math.Max(completedFYs.Count, 1);
            decimal avgQty = totalQty / yearCount;

            decimal lastYearQty = 0;
            var lastFY = completedFYs.LastOrDefault();
            if (lastFY != null)
            {
                var lastYearData = salesData.FirstOrDefault(x => x.FinancialYearId == lastFY.Id);
                lastYearQty = lastYearData?.TotalQuantity ?? 0;
            }

            // The displayed 3-Year Total Quantity must use the SAME lifting
            // values as the MO Approval detail page (Urea + DAP + 20:20 + SSP in
            // the Fertilizer category), so the ApplyWelfareScheme page and the
            // ApprovalDetail page always agree. Eligibility conditions and the
            // "Quantity Lifted during Last Year" figure stay on the existing logic.
            var products = await _db.Products.AsNoTracking().ToListAsync();
            var categories = await _db.Categories.AsNoTracking().ToListAsync();
            var fertilizerByFy = await _db.DealerCreditLimitSalesData
                .AsNoTracking()
                .Where(x => x.CustomerId == dealer.Id && fyIds.Contains(x.FinancialYearId))
                .GroupBy(x => new { x.FinancialYearId, x.ProductId, x.CategoryId })
                .Select(g => new { g.Key.FinancialYearId, g.Key.ProductId, g.Key.CategoryId, Quantity = g.Sum(x => x.Quantity) })
                .ToListAsync();

            decimal fertilizerQty = 0;
            foreach (var row in fertilizerByFy)
            {
                var productName = products.FirstOrDefault(p => p.Id == row.ProductId)?.Name;
                var categoryName = categories.FirstOrDefault(c => c.Id == row.CategoryId)?.Name;
                if (IsFertilizerLiftingMatch(productName, categoryName))
                    fertilizerQty += row.Quantity;
            }

            var result = new DealerSalesHistoryDto
            {
                AverageQuantityLifted3Years = Math.Round(fertilizerQty, 2),
                LastYearQuantityLifted = lastYearQty,
                QuantityRangeLabel = GetQuantityRangeLabel(avgQty),
                QuantityRangeValue = GetQuantityRangeValue(avgQty),
                IsSubDealerEligible = avgQty >= 50000,
                IsEmployeeEligible = avgQty >= 5000,
                YearlyData = salesData
            };

            return Ok(result);
        }

        [HttpGet("sub-dealers")]
        public async Task<ActionResult<List<SubDealerDto>>> GetSubDealers()
        {
            var subDealers = await _db.SubDealerRegistrations
                .AsNoTracking()
                .Where(sd => sd.Status == SubDealerStatus.Active)
                .Select(sd => new SubDealerDto
                {
                    Id = sd.Id,
                    SubDealerCode = sd.SubDealerCode ?? string.Empty,
                    FirmName = sd.FirmName
                })
                .ToListAsync();

            if (subDealers.Count == 0)
            {
                subDealers = new List<SubDealerDto>
                {
                    new SubDealerDto { Id = 1, SubDealerCode = "SD-TEST-001", FirmName = "Test Sub Dealer - Chennai" },
                    new SubDealerDto { Id = 2, SubDealerCode = "SD-TEST-002", FirmName = "Test Sub Dealer - Coimbatore" },
                    new SubDealerDto { Id = 3, SubDealerCode = "SD-TEST-003", FirmName = "Test Sub Dealer - Madurai" }
                };
            }

            return Ok(subDealers);
        }

        [HttpGet("employees")]
        public async Task<ActionResult<List<EmployeeDto>>> GetEmployees()
        {
            var employees = await _db.EmployeeInformation
                .AsNoTracking()
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeName = e.Name ?? string.Empty,
                    EmployeeCode = e.EmployeeCode ?? string.Empty
                })
                .Take(50)
                .ToListAsync();

            if (employees.Count == 0)
            {
                employees = new List<EmployeeDto>
                {
                    new EmployeeDto { Id = 1, EmployeeName = "Ravi Kumar", EmployeeCode = "EMP-001" },
                    new EmployeeDto { Id = 2, EmployeeName = "Suresh Babu", EmployeeCode = "EMP-002" },
                    new EmployeeDto { Id = 3, EmployeeName = "Priya Devi", EmployeeCode = "EMP-003" }
                };
            }

            return Ok(employees);
        }

        private static bool IsFertilizerLiftingMatch(string? productName, string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(categoryName) ||
                !categoryName.Contains("Fertilizer", StringComparison.OrdinalIgnoreCase))
                return false;

            // Mirrors the MO Approval detail page (SchemeApprovalBody) lifting
            // columns: Urea + DAP + 20:20 + SSP in the Fertilizer category.
            return productName.Trim().Equals("Urea", StringComparison.OrdinalIgnoreCase)
                || productName.Contains("DAP", StringComparison.OrdinalIgnoreCase)
                || productName.Contains("20:20", StringComparison.OrdinalIgnoreCase)
                || productName.Contains("20-20", StringComparison.OrdinalIgnoreCase)
                || productName.Contains("NPK", StringComparison.OrdinalIgnoreCase)
                || productName.Contains("SSP", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetQuantityRangeLabel(decimal avgQty)
        {
            if (avgQty < 1000) return "<1,000 MT";
            if (avgQty <= 5000) return ">1,000 MT";
            if (avgQty <= 10000) return "5,001 to 10,000 MT";
            if (avgQty <= 25000) return "10,001 to 25,000 MT";
            if (avgQty <= 35000) return "25,001 to 35,000 MT";
            if (avgQty <= 50000) return "35,001 to 50,000 MT";
            return ">50,001 MT";
        }

        private static string GetQuantityRangeValue(decimal avgQty)
        {
            if (avgQty < 1000) return "lt1000";
            if (avgQty <= 5000) return "gt1000";
            if (avgQty <= 10000) return "5001-10000";
            if (avgQty <= 25000) return "10001-25000";
            if (avgQty <= 35000) return "25001-35000";
            if (avgQty <= 50000) return "35001-50000";
            return "gt50001";
        }
    }
}
