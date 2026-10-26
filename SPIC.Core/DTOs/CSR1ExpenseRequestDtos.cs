using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
    // Extends the existing CSR1SaveRequest without changing the current crop/product contract.
    public class CSR1SaveRequestWithExpenses : CSR1SaveRequest
    {
        public List<CSR1ExpenseSaveDto> Expenses { get; set; } = new();
    }

    public class CSR1ExpenseSaveDto
    {
        public int ExpenseId { get; set; }
        public decimal ExpenseAmount { get; set; }
    }
}
