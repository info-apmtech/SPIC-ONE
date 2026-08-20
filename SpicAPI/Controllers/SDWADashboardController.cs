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
                DealerName = user.Name ?? string.Empty,
                DealerCode = dealer?.SPICCode ?? dealer?.DealerCode ?? string.Empty,
                Region = regionName,
                State = stateName,
                District = districtName,
                HQ = hqName,
                Email = user.Email ?? string.Empty,
                Phone = phone,
                Address = address,
                ProfileCompletion = CalculateProfileCompletion(dealer)
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
            var appointmentDate = dealer.DateOfAppointment.Date;
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
    }
}
