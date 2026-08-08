using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Xml.Linq;

namespace SPIC.Core.Entities
{
    public class DealerRegistration
    {
        public int Id { get; set; }
        public string? UserTableId { get; set; }
        [Display(Name = "Dealar / Department")]
        public bool IsDealer { get; set; }
        // NEW: Keep old IsDealer for backward compatibility. Add DealerType enum to support Institution.
        public RegistrationDealerType? DealerType { get; set; }
        public bool InSpic { get; set; }
        public bool InGreenStar { get; set; }

        public string? DealerCode { get; set; }
        public string? SPICCode { get; set; }
        public string? GreenStarCode { get; set; }
        public string? TnCode { get; set; }
        public string? NCode { get; set; }

        [Display(Name = "State")]
        public int StateId { get; set; }
        public int Region { get; set; }
        public int HQ { get; set; }
        public DealerStatus Status { get; set; }
        [Display(Name = "Parent Dealer")]
        public int ParentDealer { get; set; }
        [Display(Name = "Firm Name")]
        public string FirmName { get; set; }
        [Display(Name = "Spic Date Of Appointment")]
        public DateTime DateOfAppointment { get; set; }
        [Display(Name = "Greenstar Date Of Appointment")]
        public DateTime? GreenstarDateOfAppointment { get; set; }
        [Display(Name = "Business Type")]
        public string? BusinessEntityType { get; set; }
        //In Active Status
        public DateTime? LastTransactionDate { get; set; }
        public bool? IsLastTransactionIsSale { get; set; }
        public decimal DebitorBalance { get; set; }
        //Terminated status
        public bool? IsFinalAmountSettled { get; set; }
        public DateTime? FinalApprovalDate { get; set; }
        public DateTime? SettlementDate { get; set; }
        public decimal SettlementAmount { get; set; }

        //Address Details
        [Display(Name = "Google Map URL")]
        public string? GoogleMapURL { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        [Display(Name = "Shop No / Room No / Block No")]
        public string ShopNoORRoomNoOrBlockNo { get; set; }
        public string? Street { get; set; }
        [Display(Name = "Sub Village")]
        public string? SubVillage { get; set; }
        public string Village { get; set; }
        [Display(Name = "Pin Code")]
        public string PinCode { get; set; }
        public string? Block { get; set; }
        public string? Taluk { get; set; }
        [Display(Name = "District")]
        public int? DistrictId { get; set; }
        public int? DealerStateId { get; set; }
        [Display(Name = "Official Contact Number")]
        public string OfficialContactNumber { get; set; }
        [Display(Name = "WhatsApp Number")]
        public string WhatsAppNumber { get; set; }
        [Display(Name = "Alternative Number")]
        public string? AlternativeNumber { get; set; }


        //Bank Details
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public int? BankId { get; set; }
        public string Branch { get; set; }
        public string IFSC { get; set; }
        public string? ChequeFilePath { get; set; }

        [Display(Name = "GST Number")]
        public string? GSTNumber { get; set; }
        public string? GSTFilePath { get; set; }
        [Display(Name = "PAN Number")]
        public string? PANNumber { get; set; }
        public string? PANFilePath { get; set; }
        [Display(Name = "Aadhaar Number")]
        public string? AadhaarNumber { get; set; }
        public string? AadhaarFilePath { get; set; }

        //Trade Deposit Details
        public decimal TradeDepositAmount { get; set; }
        public string? TradeDepositReceiptNo { get; set; }
        public DateTime? TradeDepositDate { get; set; }

        //Trade Deposit Details - Greenstar
        public decimal? GreenstarTradeDepositAmountReg { get; set; }
        public string? GreenstarTradeDepositReceiptNoReg { get; set; }
        public DateTime? GreenstarTradeDepositDateReg { get; set; }

        //Wholesale Fertilizer
        [Display(Name = "WholeSale Fertilizer License")]
        public string? WholeSaleFertilizerLicenseNumber { get; set; }
        public DateTime WholesaleLicenseExpiryDate { get; set; }
        public string? WholesalemFMSCode { get; set; }
        public string? WholesaleLicenseFilePath { get; set; }

        //Retail Fertilizer
        [Display(Name = "Retail Fertilizer License")]
        public string? RetailFertilizerLicenseNumber { get; set; }
        public DateTime RetailLicenseExpiryDate { get; set; }
        public string? RetailmFMSCode { get; set; }
        public string? RetailLicenseFilePath { get; set; }

        [Display(Name = "Is Office Automation")]
        public bool IsOfficeAutomation { get; set; }
        public DateTime? ExpectedOfficeAutomationDate { get; set; }

        [Display(Name = "Is SDWA")]
        public bool IsSDWA { get; set; }

        ////[Display(Name = "Experience")]
        ////public int ExperienceId { get; set; }
        //[Display(Name = "Warehouse Facilities")]
        //public int WarehouseFacilitiesId { get; set; }
        //[Display(Name = "Rail Facilities")]
        //public int RailFacilitiesId { get; set; }
        //[Display(Name = "Port Facilities")]
        //public int PortFacilitiesId { get; set; }
        //[Display(Name = "Regional Block")]
        //public int RegionalBlockId { get; set; }
        //public int OwnerShipInfoId { get; set; }
        //public int OccupationId { get; set; }
        //public int InvestmentId { get; set; }
        //public int BankBranchId { get; set; }
        //public int AssetBankInfoId { get; set; }
        //public int LandId { get; set; }
        //public int BuildingId { get; set; }
        //public int MovableId { get; set; }
        //public int InfrastructureId { get; set; }
        //public int LoanLiabilitiesId { get; set; }
        //public int FiscalYearValuationId { get; set; }
        public int YearsofExperiance { get; set; }
        public EntityType? EntityType { get; set; }
        public DateTime CreditLimitExperiance { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        //Approval Status Null pending, true for approved, false for rejected
        public bool? RMApproved { get; set; }
        public bool? SMApproved { get; set; }
        public bool? AVPApproved { get; set; }
        //Invesment

        [Display(Name = "Capital Investment")]
        public decimal CapitalInvestment { get; set; }
        [Display(Name = "Remarks")]
        public string? CapitalInvestmentRemarks { get; set; }
        [Display(Name = "Cash Credit Limit")]
        public decimal CashCreditLimit { get; set; }
        [Display(Name = "Remarks")]
        public string? CashCreditLimitRrmarks { get; set; }
        //movable asset
        public decimal AssetValue { get; set; }
        public string? Remarks { get; set; }
        //infra
        public decimal OwnGodownCapacity { get; set; }
        public decimal RentGodownCapacity { get; set; }
        [Display(Name = "Legal Name")]
        public string? GSTLegalName { get; set; }
        [Display(Name = "Trade Name")]
        public string? GSTTradeName { get; set; }
        [Display(Name = "Constitution of Business")]
        public string? GSTConstitutionofBusiness { get; set; }
        [Display(Name = "Inactive Proposal")]
        public FutureBusinessProposal? InactiveProposal { get; set; }
        public bool? IsSubmittedForReview { get; set; } = false;

        // True when this registration was created through the "Create New Dealer" flow.
        // New dealers have no DealerCode until the final approval generates one.
        public bool IsNewDealerRegistration { get; set; }

        // Dealership Application Fee (New Dealer flow — SPIC)
        public int? DealershipApplicationFeeBankId { get; set; }
        public string? DealershipApplicationFeeDDNumber { get; set; }
        public DateTime? DealershipApplicationFeeDDDate { get; set; }
        public decimal? DealershipApplicationFeeAmount { get; set; }
        public string? DealershipApplicationFeePayableAt { get; set; }

        // Trade Deposit Details — SPIC (New Dealer flow)
        public string? SpicTradeDepositDDNumber { get; set; }
        public int? SpicTradeDepositDDBankId { get; set; }
        public DateTime? SpicTradeDepositDDDate { get; set; }
        public decimal? SpicTradeDepositDDAmount { get; set; }
		public string? DealershipApplicationFeeFilePath { get; set; }
		public string? SpicTradeDepositFilePath { get; set; }

		// Trade Deposit Details — GFL / Greenstar (New Dealer flow)
		public string? GflTradeDepositDDNumber { get; set; }
        public int? GflTradeDepositDDBankId { get; set; }
        public DateTime? GflTradeDepositDDDate { get; set; }
        public decimal? GflTradeDepositDDAmount { get; set; }
		public string? GflTradeDepositFilePath { get; set; }

	}
    public class DealerApprovalHistory
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public string ApprovedBy { get; set; }
        public string Role { get; set; }
        public DateTime ApprovedAt { get; set; }
        public string Remarks { get; set; }
        public bool IsApproved { get; set; }
        public decimal? RmCreditLimit { get; set; }
        public decimal? RmCreditLimitGfl { get; set; }
        public decimal? SmCreditLimit { get; set; }
        public decimal? SmCreditLimitGfl { get; set; }
        public decimal? AvpCreditLimit { get; set; }
        public decimal? AvpCreditLimitGfl { get; set; }
    }
    public enum EntityType
    {
        soleProprietor = 1, Partnership, LLP, PvtLtd, PubLtd, Society
    }
    public enum DealerStatus
    {
        Active, InActive, Terminated
    }
    public enum IrrigationType
    {
        CANAL, TANK, WELL
    }

    public enum RegistrationDealerType
    {
        Dealer, Department, Institution
    }
    public enum FutureBusinessProposal
    {
        FutureBusiness = 1,
        Terminated = 2,
        NotTraceable = 3
    }
    public class DealerExperience
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        [Display(Name = "Company")]
        public string CompanyId { get; set; }
        [Display(Name = "No Of Years")]
        public int NoOfYears { get; set; }
        public decimal Quantity { get; set; }
        public decimal TurnOver { get; set; }
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
    }
    public class AnnualSaleDataLastFYofDealerRegistration
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public int CategoryId { get; set; }
        public int ProductId { get; set; }
        public decimal OwnRetailsSaleQty { get; set; }
        public decimal OwnRetailsSaleAmount { get; set; }
        public decimal SaleToDealerQty { get; set; }
        public decimal SaleToDealerAmount { get; set; }
    }
    public class DealerWarehouseFacilities
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        [Display(Name = "warehouse")]
        public int WarehouseId { get; set; }
        public double Distance { get; set; }
        public double Freight { get; set; }
    }
    public class DealerRailFacilities
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        [Display(Name = "Rail Facilities Name")]
        public int RailFacilitiesId { get; set; }
        public double Distance { get; set; }
        public double Freight { get; set; }
    }
    public class DealerPortFacilities
    {
        public int Id { get; set; }
        [Display(Name = "Port")]
        public int DealerId { get; set; }
        public int PortId { get; set; }
        public double Distance { get; set; }
        public double Freight { get; set; }
    }
    public enum Months
    {
        January, February, March, April, May, June, July, August, September, October, November, December
    }
    public class DealerMarketDetail
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        [Display(Name = "Name Of Block")]
        public string NameOfBlock { get; set; }
        [Display(Name = "Major Crops")]
        public int MajorCrops { get; set; }
        [Display(Name = "No Of Dealers")]
        public int NoOfDealer { get; set; }
        [Display(Name = "No Of Farmer")]
        public int NoOfFarmer { get; set; }
        [Display(Name = "Season From Month")]
        public Months SeasonFromMonth { get; set; }
        [Display(Name = "Season To Month")]
        public Months SeasonToMonth { get; set; }
        public bool IsCanal { get; set; }
        public bool IsTank { get; set; }
        public bool IsWell { get; set; }
		public bool IsRainfed { get; set; }
	}
    public class DealerCompaniesOperatingInArea
    {
        public int Id { get; set; }
        public int DealerId { get; set; }

        [Display(Name = "Company")]
        public string? CompaniesOperating { get; set; }
    }
    public enum Gender
    {
        Male, Female, Transgender
    }
    public enum MaritalStatus
    {
        Single, Married, Divorced, Widowed
    }
    public class DealerOwnershipInfo
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        [Display(Name = "Proprietor Name")]
        public string Name { get; set; }
        [Display(Name = "Father Name")]
        public string FatherName { get; set; }
        public Gender Gender { get; set; }
        public MaritalStatus MaritalStatus { get; set; }
        [Display(Name = "Date Of Birth")]
        public DateTime DOB { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        [Display(Name = "Aadhaar Number")]
        public string? AadhaarNumber { get; set; }
        public string? AadhaarFilePath { get; set; }
        [Display(Name = "PAN Number")]
        public string? PANNumber { get; set; }
        public string? PANFilePath { get; set; }
        public string ProprietorImagePath { get; set; }
        //address details
        [Display(Name = "Shop No / Room No / Block No")]
        public string ShopNoORRoomNoOrBlockNo { get; set; }
        public string? Street { get; set; }
        [Display(Name = "Sub Village")]
        public string? SubVillage { get; set; }
        public string Village { get; set; }
        [Display(Name = "Pin Code")]
        public string PinCode { get; set; }
        public string? Block { get; set; }
        public string? Taluk { get; set; }
        [Display(Name = "District")]
        public int DistrictId { get; set; }
        public int StateId { get; set; }

    }
    public class PartnerFamilyDetails
    {
        public int Id { get; set; }
        public int OwnershipPartnerId { get; set; }
        [Display(Name = "Family Member Name")]
        public string FamilyMemberName { get; set; }
        [Display(Name = "Date Of Birth")]
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        [Display(Name = "Relationship")]
        public int RelationshipId { get; set; }
        public string? Occupation { get; set; }
    }
    public class PartnerOccupation
    {
        public int Id { get; set; }
        public int OwnershipPartnerId { get; set; }
        [Display(Name = "Name of Company")]
        public string NameofCompany { get; set; }
        [Display(Name = "Sector")]
        public int SectorId { get; set; }
        [Display(Name = "Annual Turnover")]
        public decimal AnnualTurnover { get; set; }
    }

    public class SalesPlanningInDealerRegistration
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public int CategoryId { get; set; }
        public int ProductId { get; set; }
        public decimal AprilQty { get; set; }
        public decimal AprilAmount { get; set; }
        public decimal MayQty { get; set; }
        public decimal MayAmount { get; set; }
        public decimal JuneQty { get; set; }
        public decimal JuneAmount { get; set; }
        public decimal JulyQty { get; set; }
        public decimal JulyAmount { get; set; }
        public decimal AugustQty { get; set; }
        public decimal AugustAmount { get; set; }
        public decimal SeptemberQty { get; set; }
        public decimal SeptemberAmount { get; set; }
        public decimal OctoberQty { get; set; }
        public decimal OctoberAmount { get; set; }
        public decimal NovemberQty { get; set; }
        public decimal NovemberAmount { get; set; }
        public decimal DecemberQty { get; set; }
        public decimal DecemberAmount { get; set; }
        public decimal JanuaryQty { get; set; }
        public decimal JanuaryAmount { get; set; }
        public decimal FebruaryQty { get; set; }
        public decimal FebruaryAmount { get; set; }
        public decimal MarchQty { get; set; }
        public decimal MarchAmount { get; set; }

    }

    public class DealerAssetBank
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public int BankId { get; set; }
        public string? BankBranch { get; set; }
        public decimal Value { get; set; }
        public string? Remarks { get; set; }
        public string? FileUploadPath { get; set; }
    }
    public class DealerAssetLand
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public string? LandName { get; set; }
        public string? SurvayNumber { get; set; }
        public decimal LandSize { get; set; }
        public decimal PropertyValue { get; set; }
        public string? UploadedLandDocumentPath { get; set; }
        public string? UploadedECDocumentPath { get; set; }
        public string? UploadedValuationCertificatePath { get; set; }
    }
    public class DealerAssetBuilding
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public string? BuildingName { get; set; }
        public decimal PropertyValue { get; set; }
        public string? SurveyNumber { get; set; }
        public decimal? LandSize { get; set; }
        [Display(Name = "Shop No / Room No / Block No")]
        public string ShopNoORRoomNoOrBlockNo { get; set; }
        public string? Street { get; set; }
        [Display(Name = "Sub Village")]
        public string? SubVillage { get; set; }
        public string Village { get; set; }
        [Display(Name = "Pin Code")]
        public string PinCode { get; set; }
        public string? Block { get; set; }
        public string? Taluk { get; set; }
        [Display(Name = "District")]
        public int DistrictId { get; set; }
        public int StateId { get; set; }
        public string? Remarks { get; set; }
        public string? UploadedBuildingDocumentPath { get; set; }
        public string? UploadedECDocumentPath { get; set; }
        public string? UploadedValuationCertificatePath { get; set; }
    }
    public class DealerLoanLiabilities
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public string? LoanSource { get; set; }
        public decimal LoanValue { get; set; }
        public string? Remarks { get; set; }
    }

    public class DealerCreditLimitProposal
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        //Existing Credit Limit For Spic
        public decimal SpicExistingCreditLimitAmount { get; set; }
        public DateTime SpicExistingCreditLimitFrom { get; set; }
        public DateTime SpicExistingCreditLimitTo { get; set; }
        //Existing Credit Limit For Greenstar
        public decimal GSExistingCreditLimitAmount { get; set; }
        public DateTime GSExistingCreditLimitFrom { get; set; }
        public DateTime GSExistingCreditLimitTo { get; set; }

        public int FY1 { get; set; }
        public int FY2 { get; set; }
        public int FY3 { get; set; }
        public double Q1Mark { get; set; }
        public double Q2Mark { get; set; }
        public double Q3Mark { get; set; }
        public double Q4Mark { get; set; }
        public double Q5Mark { get; set; }
        public double Q6Mark { get; set; }
        public double Q7Mark { get; set; }
        public double Q8Mark { get; set; }
        public double Q9Mark { get; set; }
        public double Q10Mark { get; set; }
        public double Q11Mark { get; set; }
        public double GQ1Mark { get; set; }
        public double GQ2Mark { get; set; }
        public double GQ3Mark { get; set; }
        public double GQ4Mark { get; set; }
        public double GQ5Mark { get; set; }
        public double GQ6Mark { get; set; }
        public double GQ7Mark { get; set; }
        public double GQ8Mark { get; set; }
        public double GQ9Mark { get; set; }
        public double GQ10Mark { get; set; }
        public double GQ11Mark { get; set; }
        public double GQ12Mark { get; set; }
        public decimal AdditionalCreditLimit { get; set; }
        public decimal GreenstarAdditionalCreditLimit { get; set; }

        // SPIC Securities
        public string? SpicFDNumber { get; set; }
        public string? SpicFDOtherDetails { get; set; }
        public decimal SpicFDAmount { get; set; }
        public string? SpicBGNumber { get; set; }
        public string? SpicBGOtherDetails { get; set; }
        public decimal SpicBGAmount { get; set; }
        public string? SpicCollateralNumber { get; set; }
        public string? SpicCollateralOtherDetails { get; set; }
        public decimal SpicCollateralAmount { get; set; }
        public string? SpicTradeDepositNumber { get; set; }
        public string? SpicTradeDepositOtherDetails { get; set; }
        public decimal SpicTradeDepositAmount { get; set; }

        // Greenstar Securities
        public string? GreenstarFDNumber { get; set; }
        public string? GreenstarFDOtherDetails { get; set; }
        public decimal GreenstarFDAmount { get; set; }
        public string? GreenstarBGNumber { get; set; }
        public string? GreenstarBGOtherDetails { get; set; }
        public decimal GreenstarBGAmount { get; set; }
        public string? GreenstarCollateralNumber { get; set; }
        public string? GreenstarCollateralOtherDetails { get; set; }
        public decimal GreenstarCollateralAmount { get; set; }
        public string? GreenstarTradeDepositNumber { get; set; }
        public string? GreenstarTradeDepositOtherDetails { get; set; }
        public decimal GreenstarTradeDepositAmount { get; set; }

        // Raw valuation values (manually entered, not calculated marks)
        public double? SpicMonthlyAvgNetOverdues { get; set; }
        public double? GreenstarMonthlyAvgNetOverdues { get; set; }
    }
    public class DealerCreditLimitSalesPerformance
    {
        public int Id { get; set; }
        public int CreditLimitId { get; set; }
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public decimal FY1Qty { get; set; }
        public decimal FY1Amount { get; set; }
        public decimal FY2Qty { get; set; }
        public decimal FY2Amount { get; set; }
        public decimal FY3Qty { get; set; }
        public decimal FY3Amount { get; set; }
    }
    public class DealerRegistrationDocuments
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public string SpecimanFilePath { get; set; }
        public string BankGauranteeFilePath { get; set; }
        public int FY1 { get; set; }
        public string FY1ITReturnFilePath { get; set; }
        public int FY2 { get; set; }
        public string FY2ITReturnFilePath { get; set; }
        public string? ValuationCertificateFilePath { get; set; }
        public string? RetailerListFilePath { get; set; }
        public string? PartnershipDeadFilePath { get; set; }
        public string? BoardReasolutionFilePath { get; set; }
        public string? AffidavitFilePath { get; set; }
        public string? GreenstarSpecimanFilePath { get; set; }
        public string? AuthorizationLetterFilePath { get; set; }
        public string? DeedOfGuaranteeFilePath { get; set; }
        public string? LlpAgreementFilePath { get; set; }
		public string? ArticlesOfAssociationFilePath { get; set; }   // AOA
		public string? MemorandumOfAssociationFilePath { get; set; } // MOA
		public string? ByLaw { get; set; }
		public string? RequestLetterFilePath { get; set; }

	}
    public class DealerCreditLimitSales
    {
        public int Id { get; set; }
        //public int DealerId { get; set; }
        public int StateId { get; set; }
        //Dealer Code
        public string CustomerNumber { get; set; }
        //Dealer Name
        public int CustomerId { get; set; }
        //Product
        public int ProductId { get; set; }
        //Categories
        public int CategoryId { get; set; }
        //Product Groups
        public int ProductGroupId { get; set; }
        //Financial year 
        public int FinancialYearId { get; set; }
        public decimal Quantity { get; set; }
        public decimal GrossAmount { get; set; }

    }


    public class SalesPerfViewRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";

        public decimal? FY1Qty { get; set; }
        public decimal? FY1Amount { get; set; }

        public decimal? FY2Qty { get; set; }
        public decimal? FY2Amount { get; set; }

        public decimal? FY3Qty { get; set; }
        public decimal? FY3Amount { get; set; }
    }
}