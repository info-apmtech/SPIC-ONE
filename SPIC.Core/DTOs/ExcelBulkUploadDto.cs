using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	public sealed class ExcelBulkUploadResult
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;

		public string FileName { get; set; } = string.Empty;
		public string CategoryId { get; set; } = string.Empty;
		public DateTime? ReportDate { get; set; }

		public int TotalRows { get; set; }
		public int RowsInserted { get; set; }
		public int RowsUpdated { get; set; }
		public int RowsSkipped { get; set; }
		public int IfmsDealerMobileNumbersUpdated { get; set; }
		public int ExistingDuplicateRowsDetected { get; set; }

		public ExcelBulkUploadResultMasters NewMastersCreated { get; set; } = new();
		public List<string> Warnings { get; set; } = new();
	}

	public sealed class ExcelBulkUploadResultMasters
	{
		public int States { get; set; }
		public int Districts { get; set; }
		public int SubDistricts { get; set; }
		public int IfmsDealers { get; set; }
		public int DealerTypes { get; set; }
		public int DealershipNatures { get; set; }
		public int Companies { get; set; }
		public int Plants { get; set; }
		public int Products { get; set; }

		// Used by Company Sales / Wholesale Sales / Reconciliation imports.
		public int Units { get; set; }
		public int Statuses { get; set; }
		public int TxnTypes { get; set; }
		public int AckThroughs { get; set; }
		public int Warehouses { get; set; }
	}
}