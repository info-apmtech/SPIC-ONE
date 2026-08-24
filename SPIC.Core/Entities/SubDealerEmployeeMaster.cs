using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.Entities
{
	/// <summary>
	/// Sub Dealer beneficiary master.
	/// One row represents one Sub Dealer of a main dealer together with the
	/// nominee / beneficiary details used by the SDWA Welfare Application.
	/// Business key: DealerCode + SubDealerCode.
	/// </summary>
	public class SubDealerBeneficiary
	{
		public int Id { get; set; }

		// Reference id from the uploaded beneficiary file (optional)
		public long? BeneficiaryId { get; set; }

		// Main dealer this sub dealer belongs to
		public required string DealerCode { get; set; }
		public required string MainDealerFirmName { get; set; }
		public string? HQ { get; set; }
		public string? BranchDistrict { get; set; }

		// Sub dealer details
		public required string SubDealerCode { get; set; }
		public required string SubDealerName { get; set; }
		public string? SubDealerDistrict { get; set; }

		// Nominee / beneficiary information loaded by the welfare application
		public string? NomineeName { get; set; }
		public required string BeneficiaryName { get; set; }
		public DateTime? DOB { get; set; }
		public string? Relationship { get; set; }

		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public required string UpdatedBy { get; set; }
	}

	/// <summary>
	/// Approved Employee beneficiary master.
	/// One row represents one employee of a dealer together with the
	/// nominee / beneficiary details used by the SDWA Welfare Application.
	/// Business key: DealerCode + EmployeeName.
	/// </summary>
	public class EmployeeBeneficiary
	{
		public int Id { get; set; }

		// Reference id from the uploaded beneficiary file (optional)
		public long? BeneficiaryId { get; set; }

		public required string DealerCode { get; set; }
		public required string EmployeeName { get; set; }

		// Nominee / beneficiary information loaded by the welfare application
		public required string BeneficiaryName { get; set; }
		public DateTime? DOB { get; set; }
		public string? Relationship { get; set; }
		public string? MaritalStatus { get; set; }
		public string? EducationalQualification { get; set; }

		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public required string UpdatedBy { get; set; }
	}
}
