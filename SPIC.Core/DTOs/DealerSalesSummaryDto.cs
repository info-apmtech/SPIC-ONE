using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
    public class DealerSalesSummaryDto
    {
        public List<DealerSalesYearOptionDto> AvailableYears { get; set; } = new();
        public List<DealerSalesProductDto> ProductSales { get; set; } = new();
    }

    public class DealerSalesYearOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DealerSalesProductDto
    {
        public int FinancialYearId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal GrossAmount { get; set; }
    }
}
