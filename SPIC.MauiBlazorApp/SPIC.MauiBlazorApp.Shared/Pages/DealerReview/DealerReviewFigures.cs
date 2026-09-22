using DR = SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Pages.DealerReview
{
    /// <summary>
    /// The derived numbers SavedDealerReview shows: SPIC/GreenStar valuation values, the
    /// MO recommended credit limits, the net financial position and the sales-planning totals.
    /// Every method is a verbatim move of the page helper with the same name, reading the same
    /// fields, so the values and rounding are unchanged.
    ///
    /// The page builds ONE instance once the registration and its lookups are loaded and hands
    /// the same instance to every section that needs a figure. That keeps the section parameter
    /// reference-stable, which is what lets a section skip re-rendering when an unrelated part
    /// of the page changes — passing a freshly computed value per render would defeat it.
    /// </summary>
    public sealed class DealerReviewFigures
    {
        private readonly DR.DealerRegistration? _dealer;
        private readonly List<DR.Product> _products;
        private readonly List<DR.Category> _categories;
        private readonly List<DR.Unit> _units;
        private readonly List<DR.AnnualSaleDataLastFYofDealerRegistration> _annualSales;
        private readonly List<DR.DealerCreditLimitSales> _creditLimitSalesData;
        private readonly DR.DealerCreditLimitProposal? _creditProposal;
        private readonly List<DR.DealerAssetBank> _assetBanks;
        private readonly List<DR.DealerAssetLand> _assetLands;
        private readonly List<DR.DealerAssetBuilding> _assetBuildings;
        private readonly List<DR.DealerLoanLiabilities> _loans;
        private readonly List<DR.SalesPlanningInDealerRegistration> _salesPlanning;

        public DealerReviewFigures(
            DR.DealerRegistration? dealer,
            List<DR.Product> products,
            List<DR.Category> categories,
            List<DR.Unit> units,
            List<DR.AnnualSaleDataLastFYofDealerRegistration> annualSales,
            List<DR.DealerCreditLimitSales> creditLimitSalesData,
            DR.DealerCreditLimitProposal? creditProposal,
            List<DR.DealerAssetBank> assetBanks,
            List<DR.DealerAssetLand> assetLands,
            List<DR.DealerAssetBuilding> assetBuildings,
            List<DR.DealerLoanLiabilities> loans,
            List<DR.SalesPlanningInDealerRegistration> salesPlanning)
        {
            _dealer = dealer;
            _products = products;
            _categories = categories;
            _units = units;
            _annualSales = annualSales;
            _creditLimitSalesData = creditLimitSalesData;
            _creditProposal = creditProposal;
            _assetBanks = assetBanks;
            _assetLands = assetLands;
            _assetBuildings = assetBuildings;
            _loans = loans;
            _salesPlanning = salesPlanning;
        }

        // Score → credit percent (same table the CreditLimit pages use)
        public static double GetCreditPercentFromScore(double scorePercent) => scorePercent switch
        {
            >= 75 => 25,
            >= 60 => 20,
            >= 45 => 15,
            >= 30 => 10,
            _ => 0
        };

        private static bool IsFertilizerCategory(DR.Category? c) =>
            c?.Name != null && c.Name.Contains("Fertilizer", StringComparison.OrdinalIgnoreCase);

        private static bool IsUreaProduct(DR.Product? p) =>
            p?.Name != null && p.Name.Contains("Urea", StringComparison.OrdinalIgnoreCase);

        private static bool IsSpecialityCategory(DR.Category? c) =>
            c?.Name != null &&
            (c.Name.Contains("Speciality", StringComparison.OrdinalIgnoreCase) ||
             c.Name.Contains("Specialty", StringComparison.OrdinalIgnoreCase));

        private bool IsSpicPerfProduct(DR.Product? p)
        {
            if (p == null) return false;
            var cat = _categories.FirstOrDefault(c => c.Id == p.CategoryId);
            return IsFertilizerCategory(cat) && IsUreaProduct(p);
        }

        private bool IsGreenstarFertProduct(DR.Product? p)
        {
            if (p == null) return false;
            var cat = _categories.FirstOrDefault(c => c.Id == p.CategoryId);
            return IsFertilizerCategory(cat) && !IsUreaProduct(p);
        }

        private bool IsSpecialityProduct(DR.Product? p)
        {
            if (p == null) return false;
            var cat = _categories.FirstOrDefault(c => c.Id == p.CategoryId);
            return IsSpecialityCategory(cat);
        }

        // SPIC — annual (from the last-FY annual sales)
        public decimal SpicAnnualUreaQty() =>
            _annualSales.Where(s => IsSpicPerfProduct(_products.FirstOrDefault(p => p.Id == s.ProductId)))
                        .Sum(s => s.OwnRetailsSaleQty + s.SaleToDealerQty);

        public decimal SpicAnnualUreaTurnover() =>
            _annualSales.Where(s => IsSpicPerfProduct(_products.FirstOrDefault(p => p.Id == s.ProductId)))
                        .Sum(s => s.OwnRetailsSaleAmount + s.SaleToDealerAmount);

        // SPIC — performance (latest FY = _creditProposal.FY3, sourced from DealerCreditLimitSales,
        // the same table the real CreditLimit page uses)
        public decimal SpicPerfUreaQty() =>
            _creditLimitSalesData
                .Where(s => s.FinancialYearId == (_creditProposal?.FY3 ?? 0))
                .Where(s => IsSpicPerfProduct(_products.FirstOrDefault(p => p.Id == s.ProductId)))
                .Sum(s => s.Quantity);

        public decimal SpicPerfUreaTurnover() =>
            DealerReviewFormat.ConvertToLakhs(_creditLimitSalesData
                .Where(s => s.FinancialYearId == (_creditProposal?.FY3 ?? 0))
                .Where(s => IsSpicPerfProduct(_products.FirstOrDefault(p => p.Id == s.ProductId)))
                .Sum(s => s.GrossAmount));

        public decimal SpicTenureYears()
        {
            if (_dealer == null) return 0;
            var appt = _dealer.DateOfAppointment;
            if (appt == default || appt > DateTime.Now) return 0;
            return Math.Round((decimal)((DateTime.Now - appt).TotalDays / 365.25), 1);
        }

        public decimal SpicFixedAssets() =>
            (_assetLands?.Sum(l => l.PropertyValue) ?? 0) + (_assetBuildings?.Sum(b => b.PropertyValue) ?? 0);

        public decimal SpicSecurityTotalValue() =>
            (_creditProposal?.SpicFDAmount ?? 0) + (_creditProposal?.SpicBGAmount ?? 0)
            + (_creditProposal?.SpicCollateralAmount ?? 0) + (_creditProposal?.SpicTradeDepositAmount ?? 0);

        public decimal SpicPropertyPercent() =>
            SpicPerfUreaTurnover() > 0 ? (SpicFixedAssets() / SpicPerfUreaTurnover()) * 100m : 0m;

        public decimal SpicSecurityPercent() =>
            SpicPerfUreaTurnover() > 0 ? (SpicSecurityTotalValue() / SpicPerfUreaTurnover()) * 100m : 0m;

        public string SpicOverduesDisplay() =>
            _creditProposal?.SpicMonthlyAvgNetOverdues.HasValue == true
                ? _creditProposal.SpicMonthlyAvgNetOverdues.Value.ToString("N2") : "-";

        // GreenStar — annual
        public decimal GsAnnualFertQty() =>
            _annualSales.Where(s => IsGreenstarFertProduct(_products.FirstOrDefault(p => p.Id == s.ProductId)))
                        .Sum(s => s.OwnRetailsSaleQty + s.SaleToDealerQty);

        public decimal GsAnnualFertTurnover() =>
            _annualSales.Where(s => IsGreenstarFertProduct(_products.FirstOrDefault(p => p.Id == s.ProductId)))
                        .Sum(s => s.OwnRetailsSaleAmount + s.SaleToDealerAmount);

        // GreenStar — performance (FY3, from DealerCreditLimitSales, the same table the real
        // CreditLimitForGreenStar page uses)
        public decimal GsPerfFertQty() =>
            _creditLimitSalesData
                .Where(s => s.FinancialYearId == (_creditProposal?.FY3 ?? 0))
                .Where(s => IsGreenstarFertProduct(_products.FirstOrDefault(p => p.Id == s.ProductId)))
                .Sum(s => s.Quantity);

        public decimal GsPerfFertTurnover() =>
            DealerReviewFormat.ConvertToLakhs(_creditLimitSalesData
                .Where(s => s.FinancialYearId == (_creditProposal?.FY3 ?? 0))
                .Where(s => IsGreenstarFertProduct(_products.FirstOrDefault(p => p.Id == s.ProductId)))
                .Sum(s => s.GrossAmount));

        public decimal GsSpTurnover() =>
            DealerReviewFormat.ConvertToLakhs(_creditLimitSalesData
                .Where(s => s.FinancialYearId == (_creditProposal?.FY3 ?? 0))
                .Where(s => IsSpecialityProduct(_products.FirstOrDefault(p => p.Id == s.ProductId)))
                .Sum(s => s.GrossAmount));

        public decimal GsSecurityTotalValue() =>
            (_creditProposal?.GreenstarFDAmount ?? 0) + (_creditProposal?.GreenstarBGAmount ?? 0)
            + (_creditProposal?.GreenstarCollateralAmount ?? 0) + (_creditProposal?.GreenstarTradeDepositAmount ?? 0);

        public decimal GsPropertyPercent() =>
            GsPerfFertTurnover() > 0 ? (SpicFixedAssets() / GsPerfFertTurnover()) * 100m : 0m;

        public decimal GsSecurityPercent() =>
            GsPerfFertTurnover() > 0 ? (GsSecurityTotalValue() / GsPerfFertTurnover()) * 100m : 0m;

        public string GsOverduesDisplay() =>
            _creditProposal?.GreenstarMonthlyAvgNetOverdues.HasValue == true
                ? _creditProposal.GreenstarMonthlyAvgNetOverdues.Value.ToString("N2") : "-";

        // SPIC MO recommended = (Urea-fertilizer FY3 turnover in Lakhs × percent) + SPIC Additional
        public decimal GetSpicMoRecommendedLimit()
        {
            if (_creditProposal == null) return 0m;

            var pct = GetCreditPercentFromScore(_creditProposal.Q11Mark);
            var proposed = SpicPerfUreaTurnover() * (decimal)(pct / 100.0);
            return proposed + _creditProposal.AdditionalCreditLimit;
        }

        // GFL MO recommended = ((non-Urea fertilizer + Speciality) FY3 turnover in Lakhs × percent) + Greenstar Additional
        public decimal GetGflMoRecommendedLimit()
        {
            if (_creditProposal == null) return 0m;

            var turnover = GsPerfFertTurnover() + GsSpTurnover();
            var pct = GetCreditPercentFromScore(_creditProposal.GQ12Mark);
            var proposed = turnover * (decimal)(pct / 100.0);
            return proposed + _creditProposal.GreenstarAdditionalCreditLimit;
        }

        // Proposed Limit = MO Recommended − Additional (kept in sync automatically, whatever the MO calc uses)
        public decimal GetSpicProposedLimit() =>
            GetSpicMoRecommendedLimit() - (_creditProposal?.AdditionalCreditLimit ?? 0m);

        public decimal GetGflProposedLimit() =>
            GetGflMoRecommendedLimit() - (_creditProposal?.GreenstarAdditionalCreditLimit ?? 0m);

        // ---- Net financial position -------------------------------------------
        public decimal GetCapitalPosition() => _dealer?.CapitalInvestment ?? 0m;

        public decimal GetTotalAssets()
        {
            decimal banks = _assetBanks?.Sum(x => x.Value) ?? 0m;
            decimal lands = _assetLands?.Sum(x => x.PropertyValue) ?? 0m;
            decimal buildings = _assetBuildings?.Sum(x => x.PropertyValue) ?? 0m;
            decimal movables = _dealer?.AssetValue ?? 0m;
            return banks + lands + buildings + movables;
        }

        public decimal GetTotalLoans() => _loans?.Sum(x => x.LoanValue) ?? 0m;

        public decimal GetNetValue() => GetCapitalPosition() + GetTotalAssets() - GetTotalLoans();

        // ---- Sales planning ----------------------------------------------------
        public string GetUnitName(DR.SalesPlanningInDealerRegistration item)
        {
            var category = _categories.FirstOrDefault(c => c.Id == item.CategoryId);
            return _units.FirstOrDefault(u => u.Id == category?.UnitId)?.Name ?? "MT";
        }

        public decimal GetTotalSalesVolume()
        {
            if (_salesPlanning == null) return 0;
            decimal total = 0;
            foreach (var item in _salesPlanning)
                for (int i = 0; i < DealerReviewFormat.Months.Length; i++)
                    total += DealerReviewFormat.GetSalesQty(item, i);
            return total;
        }

        public decimal GetTotalForecastValue()
        {
            if (_salesPlanning == null) return 0;
            decimal total = 0;
            foreach (var item in _salesPlanning)
                for (int i = 0; i < DealerReviewFormat.Months.Length; i++)
                    total += DealerReviewFormat.GetSalesAmount(item, i);
            return total;
        }
    }
}
