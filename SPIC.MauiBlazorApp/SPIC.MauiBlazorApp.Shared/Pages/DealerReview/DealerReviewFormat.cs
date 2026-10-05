using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using DR = SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Pages.DealerReview
{
    /// <summary>
    /// Formatting and lookup helpers shared by SavedDealerReview and its section components.
    /// Every member is a verbatim move of a helper that used to live in the page's @code block;
    /// none of them changes the text, markup or rounding the page produced before the split.
    /// </summary>
    public static class DealerReviewFormat
    {
        public static readonly string[] Months =
        [
            "April", "May", "June", "July", "August", "September",
            "October", "November", "December", "January", "February", "March"
        ];

        public static string MoneyOrDash(decimal? value) =>
            value.HasValue ? $"₹{value.Value:N2}" : "-";

        public static string NumOrDash(decimal v) => v == 0 ? "-" : v.ToString("N2");

        public static decimal ConvertToLakhs(decimal grossAmount) => grossAmount / 100000m;

        public static string GetReviewBankName(IReadOnlyDictionary<int, string> bankNames, int? bankId)
        {
            if (!bankId.HasValue || bankId.Value <= 0)
                return "-";

            return bankNames.TryGetValue(bankId.Value, out var name)
                ? name
                : bankId.Value.ToString();
        }

        public static string GetSelectedCompanyDisplay(DR.DealerRegistration? dealer)
        {
            if (dealer == null)
                return "-";

            var companies = new List<string>();
            if (dealer.InSpic) companies.Add("SPIC");
            if (dealer.InGreenStar) companies.Add("GreenStar");
            return companies.Count > 0 ? string.Join(" + ", companies) : "-";
        }

        public static bool HasNewDealerRegistrationEvidence(DR.DealerRegistration? dealer)
        {
            if (dealer == null)
                return false;

            return
                dealer.DealershipApplicationFeeBankId.HasValue ||
                !string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeeDDNumber) ||
                dealer.DealershipApplicationFeeDDDate.HasValue ||
                dealer.DealershipApplicationFeeAmount.HasValue ||
                !string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeePayableAt) ||
                !string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeeFilePath) ||
                dealer.SpicTradeDepositDDBankId.HasValue ||
                !string.IsNullOrWhiteSpace(dealer.SpicTradeDepositDDNumber) ||
                dealer.SpicTradeDepositDDDate.HasValue ||
                dealer.SpicTradeDepositDDAmount.HasValue ||
                !string.IsNullOrWhiteSpace(dealer.SpicTradeDepositFilePath) ||
                dealer.GflTradeDepositDDBankId.HasValue ||
                !string.IsNullOrWhiteSpace(dealer.GflTradeDepositDDNumber) ||
                dealer.GflTradeDepositDDDate.HasValue ||
                dealer.GflTradeDepositDDAmount.HasValue ||
                !string.IsNullOrWhiteSpace(dealer.GflTradeDepositFilePath);
        }

        public static string GetInactiveProposalDisplay(DR.DealerRegistration? dealer)
        {
            if (!dealer?.InactiveProposal.HasValue ?? true) return "-";
            return dealer!.InactiveProposal!.Value switch
            {
                DR.FutureBusinessProposal.FutureBusiness => "Future Business",
                DR.FutureBusinessProposal.Terminated => "To be Terminated",
                DR.FutureBusinessProposal.NotTraceable => "Not Traceable",
                _ => dealer.InactiveProposal.Value.ToString()
            };
        }

        public static string GetEntityTypeName(DR.EntityType? entityType)
        {
            if (!entityType.HasValue) return "-";
            return entityType.Value switch
            {
                DR.EntityType.soleProprietor => "Sole Proprietor",
                DR.EntityType.Partnership    => "Partnership",
                DR.EntityType.LLP            => "LLP",
                DR.EntityType.PvtLtd         => "PVT LTD",
                DR.EntityType.PubLtd         => "PUB LTD",
                DR.EntityType.Society        => "Society",
                _                            => "-"
            };
        }

        public static string FyName(IReadOnlyDictionary<int, string> financialYearNames, int id) =>
            financialYearNames.TryGetValue(id, out var n) ? n : id.ToString();

        public static string GetPartnerStateName(IReadOnlyList<DR.State> states, int id) =>
            states.FirstOrDefault(s => s.Id == id)?.StateName ?? "-";

        public static string GetPartnerDistrictName(IReadOnlyList<DR.District> districts, int id) =>
            districts.FirstOrDefault(d => d.Id == id)?.DistrictName ?? "-";

        public static decimal GetSalesQty(DR.SalesPlanningInDealerRegistration row, int monthIndex)
        {
            return monthIndex switch { 0 => row.AprilQty, 1 => row.MayQty, 2 => row.JuneQty, 3 => row.JulyQty, 4 => row.AugustQty, 5 => row.SeptemberQty, 6 => row.OctoberQty, 7 => row.NovemberQty, 8 => row.DecemberQty, 9 => row.JanuaryQty, 10 => row.FebruaryQty, 11 => row.MarchQty, _ => 0 };
        }

        public static decimal GetSalesAmount(DR.SalesPlanningInDealerRegistration row, int monthIndex)
        {
            return monthIndex switch { 0 => row.AprilAmount, 1 => row.MayAmount, 2 => row.JuneAmount, 3 => row.JulyAmount, 4 => row.AugustAmount, 5 => row.SeptemberAmount, 6 => row.OctoberAmount, 7 => row.NovemberAmount, 8 => row.DecemberAmount, 9 => row.JanuaryAmount, 10 => row.FebruaryAmount, 11 => row.MarchAmount, _ => 0 };
        }

        public static string GetApprovalStage(DR.DealerRegistration? dealer)
        {
            if (dealer == null) return "-";

            if (dealer.RMApproved == false || dealer.SMApproved == false || dealer.AVPApproved == false)
                return "Rejected";

            if (dealer.AVPApproved == true)
                return "Approved";

            bool isComplete = dealer.PinCode != null && dealer.PinCode.Trim() != "";
            if (!isComplete) return "Submitted";

            if (dealer.RMApproved == null)
                return "In RM";

            if (dealer.RMApproved == true && dealer.SMApproved == null)
                return "In SMM";

            if (dealer.RMApproved == true && dealer.SMApproved == true && dealer.AVPApproved == null)
                return "In AVP";

            return "Submitted";
        }

        public static string GetStepClass(bool? approved)
        {
            if (approved == true) return "completed";
            if (approved == false) return "rejected";
            return "pending";
        }

        public static string GetLineClass(bool? approved)
        {
            if (approved == true) return "completed-line";
            if (approved == false) return "rejected-line";
            return "pending-line";
        }

        // ---- Files -------------------------------------------------------------
        // The page resolved file urls against Http.BaseAddress and the login token; the
        // sections receive those two as plain string parameters so nothing here needs DI.

        public static string GetFileUrl(string? apiBaseUrl, string? authToken, string? path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            var baseUrl = apiBaseUrl ?? "";
            var segments = path.Split('/').Select(Uri.EscapeDataString);
            var urlPath = string.Join("/", segments);

            var token = authToken;
            if (!string.IsNullOrEmpty(token))
            {
                var rawToken = token.StartsWith("Bearer ") ? token.Substring("Bearer ".Length) : token;
                return $"{baseUrl}/api/DealerFile/view/{urlPath}?access_token={rawToken}";
            }

            return $"{baseUrl}/api/DealerFile/view/{urlPath}";
        }

        public static string GetShortFileName(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "-";
            return path.Replace('\\', '/').Split('/').Last();
        }

        /// <summary>The "file name + View" row; the render-tree sequence is unchanged from the page.</summary>
        public static RenderFragment FileRow(string? apiBaseUrl, string? authToken, string? path) => builder =>
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "file-view-none");
                builder.AddContent(2, "No file uploaded");
                builder.CloseElement();
                return;
            }

            var url = GetFileUrl(apiBaseUrl, authToken, path);
            var name = GetShortFileName(path);

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "file-view-row");

            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "class", "file-view-name");
            builder.AddAttribute(4, "title", name);
            builder.AddContent(5, name);
            builder.CloseElement();

            builder.OpenElement(6, "a");
            builder.AddAttribute(7, "class", "file-view-btn");
            builder.AddAttribute(8, "href", url);
            builder.AddAttribute(9, "target", "_blank");
            builder.AddAttribute(10, "rel", "noopener noreferrer");
            builder.AddAttribute(11, "data-enhance-nav", "false");
            builder.AddContent(12, "View");
            builder.CloseElement();

            builder.CloseElement();
        };
    }

    /// <summary>One card in the review Timeline. Moved out of SavedDealerReview unchanged.</summary>
    public record TimelineItem(
        string Title,
        string Status,
        string UserName,
        string DateText,
        string DotClass,
        string Icon,
        string CardClass,
        string BadgeClass
    );
}
