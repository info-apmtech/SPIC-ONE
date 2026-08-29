using System;
using System.Collections.Generic;
using System.Linq;
using SPIC.Core.DTOs;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Shared roll-up of the CreditLimitSales figures used by the MO Approval detail
    /// page (SchemeApprovalBody) and the dealer's ApplyWelfareScheme page so both always
    /// present the exact same "3-Year Total Quantity" (Urea + DAP + 20:20 + SSP in the
    /// Fertilizer category, across the same financial years) for the same dealer.
    /// </summary>
    public static class DealerSalesCalculator
    {
        public enum SalesKind { Urea, DAP, Npk2020, SSP }

        public static bool IsProductMatch(SalesKind kind, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return kind switch
            {
                SalesKind.Urea => name.Trim().Equals("Urea", StringComparison.OrdinalIgnoreCase),
                SalesKind.DAP => name.Contains("DAP", StringComparison.OrdinalIgnoreCase),
                SalesKind.Npk2020 => name.Contains("20:20", StringComparison.OrdinalIgnoreCase) ||
                                     name.Contains("20-20", StringComparison.OrdinalIgnoreCase) ||
                                     name.Contains("NPK", StringComparison.OrdinalIgnoreCase),
                SalesKind.SSP => name.Contains("SSP", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        public static bool IsFertilizerCategory(string? categoryName) =>
            !string.IsNullOrWhiteSpace(categoryName) &&
            categoryName.Contains("Fertilizer", StringComparison.OrdinalIgnoreCase);

        public static bool IsSpecialityCategory(string? categoryName) =>
            !string.IsNullOrWhiteSpace(categoryName) &&
            (categoryName.Contains("Speciality", StringComparison.OrdinalIgnoreCase) ||
             categoryName.Contains("Specialty", StringComparison.OrdinalIgnoreCase));

        // Urea comes from the CreditLimit (SPIC) data source.
        public static bool IsCreditLimitUrea(DealerSalesProductDto p) =>
            IsFertilizerCategory(p.CategoryName) && IsProductMatch(SalesKind.Urea, p.ProductName);

        // DAP / 20:20 / SSP come from the CreditLimitForGreenStar data source
        // (its "Other Fertilizer" rows = Fertilizer-category products excluding Urea).
        public static bool IsGreenStarOtherFertilizer(DealerSalesProductDto p) =>
            IsFertilizerCategory(p.CategoryName) && !IsCreditLimitUrea(p);

        // SP Turnover uses the CreditLimitForGreenStar logic: specialty-product amounts only.
        public static bool IsSpecialityProduct(DealerSalesProductDto p) =>
            IsSpecialityCategory(p.CategoryName);

        private static IEnumerable<DealerSalesProductDto> GetYearProducts(
            IEnumerable<DealerSalesProductDto> products, int? fyId)
        {
            if (!fyId.HasValue) return Enumerable.Empty<DealerSalesProductDto>();
            return products.Where(p => p.FinancialYearId == fyId.Value);
        }

        public static decimal GetYearQty(IEnumerable<DealerSalesProductDto> products, int? fyId, SalesKind kind)
        {
            if (!fyId.HasValue) return 0;
            var rows = GetYearProducts(products, fyId);
            return kind switch
            {
                SalesKind.Urea => rows.Where(IsCreditLimitUrea).Sum(p => p.Quantity),
                _ => rows.Where(p => IsGreenStarOtherFertilizer(p) && IsProductMatch(kind, p.ProductName)).Sum(p => p.Quantity)
            };
        }

        public static decimal GetYearTotal(IEnumerable<DealerSalesProductDto> products, int? fyId) =>
            GetYearQty(products, fyId, SalesKind.Urea)
            + GetYearQty(products, fyId, SalesKind.DAP)
            + GetYearQty(products, fyId, SalesKind.Npk2020)
            + GetYearQty(products, fyId, SalesKind.SSP);

        public static decimal GetYearTurnover(IEnumerable<DealerSalesProductDto> products, int? fyId)
        {
            if (!fyId.HasValue) return 0;
            return GetYearProducts(products, fyId).Where(IsSpecialityProduct).Sum(p => p.GrossAmount) / 100000m;
        }

        public static decimal GetThreeYearTotal(
            IEnumerable<DealerSalesProductDto> products,
            IEnumerable<DealerSalesYearOptionDto> years) =>
            years.Sum(y => GetYearTotal(products, y.Id));

        public static decimal GetThreeYearTurnover(
            IEnumerable<DealerSalesProductDto> products,
            IEnumerable<DealerSalesYearOptionDto> years) =>
            years.Sum(y => GetYearTurnover(products, y.Id));
    }
}