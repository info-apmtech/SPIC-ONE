using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SPIC.Core.Entities
{
	public class DealerRegistration
	{
		public int Id { get; set; }
		[Display(Name = "State")]
		public int StateId { get; set; }
		public string Region { get; set; }
		public string HQ { get; set; }
		public DealerStatus Status { get; set; }
		[Display(Name = "Dealer Code and Name")]
		public string DealerCodeandName { get; set; }
		[Display(Name = "Parent Dealer")]
		public string ParentDealer { get; set; }
		[Display(Name = "Firm Name")]
		public string FirmName { get; set; }
		[Display(Name = "Date Of Appointment")]
		public DateTime DateOfAppointment { get; set; }
		[Display(Name = "Business Type")]
		public string BusinessEntityTypeId { get; set; }
		[Display(Name = "Google Map URL")]
		public string GoogleMapURL { get; set; }
		[Display(Name = "Shop No / Room No / Block No")]
		public string ShopNoORRoomNoOrBlockNo { get; set; }
		[Display(Name = "Sub Village")]
		public string SubVillage { get; set; }
		public string Village { get; set; }
		[Display(Name = "Pin Code")]
		public string PinCode { get; set; }
		public string Block { get; set; }
		public string Taluk { get; set; }
		[Display(Name = "District")]
		public string DistrictId { get; set; }
		[Display(Name = "Official Contact Number")]
		public string OfficialContactNumber { get; set; }
		[Display(Name = "WhatsApp Number")]
		public string WhatsAppNumber { get; set; }
		[Display(Name = "Alternative Number")]
		public string? AlternativeNumber { get; set; }
	
		public int BankDetailsId { get; set; }
		[Display(Name = "GST Number")]
		public string GSTNumber { get; set; }
		public string GSTFilePath { get; set; }
		[Display(Name = "PAN Number")]
		public string PANNumber { get; set; }
		public string PANFilePath { get; set; }
		[Display(Name = "Aadhaar Number")]
		public string AadhaarNumber { get; set; }
		public string AadhaarFilePath { get; set; }
		[Display(Name = "WholeSale Fertilizer")]
		public int WholeSaleFertilizerId { get; set; }
		[Display(Name = "Retail Fertilizer")]
		public int RetailFertilizerId { get; set; }
		[Display(Name = "Is Office Automation")]
		public bool IsOfficeAutomation { get; set; }
		[Display(Name = "Is SDWA")]
		public bool IsSDWA { get; set; }
		[Display(Name = "Experience")]
		public int ExperienceId { get; set; }
		[Display(Name = "Warehouse Facilities")]
		public int WarehouseFacilitiesId { get; set; }
		[Display(Name = "Rail Facilities")]
		public int RailFacilitiesId { get; set; }
		[Display(Name = "Port Facilities")]
		public int PortFacilitiesId { get; set; }
		[Display(Name = "Regional Block")]
		public int RegionalBlockId { get; set; }
		public int OwnerShipInfoId { get; set; }
		public int OccupationId { get; set; }
		public int InvestmentId { get; set; }
		public int BankBranchId { get; set; }
		public int AssetBankInfoId { get; set; }
		public int LandId { get; set; }
		public int BuildingId { get; set; }
		public int MovableId { get; set; }
		public int InfrastructureId { get; set; }
		public int LoanLiabilitiesId { get; set; }
		public int FiscalYearValuationId { get; set; }
	}
	public class BankDetails
	{
		public int Id { get; set; }
		[Display(Name = "Accound Holder Name")]
		public string AccoundHolderName { get; set; }
		[Display(Name = "Accound Number")]
		public string AccoundNumber { get; set; }
		[Display(Name = "Bank")]
		public string BankId { get; set; }
		public string BankBranchId { get; set; }
	}
	public class WholeSaleFertilizer
	{
		public int Id { get; set; }
		[Display(Name = "WholeSale Fertilizer License Details")]
		public string LicenseDetails { get; set; }
		[Display(Name = "Expiry Date")]
		public DateTime ExpiryDate { get; set; }
		[Display(Name = "nFMS Code")]
		public string nFMSCode { get; set; }
		public string LicenseFilePath { get; set; }
	}
	public class RetailFertilizer
	{
		public int Id { get; set; }
		[Display(Name = "Retail Fertilizer License Details")]
		public string LicenseDetails { get; set; }
		[Display(Name = "Expiry Date")]
		public DateTime ExpiryDate { get; set; }
		[Display(Name = "nFMS Code")]
		public string nFMSCode { get; set; }
		public string LicenseFilePath { get; set; }
	}
	public class Experience
	{
		public int Id { get; set; }
		[Display(Name = "Company")]
		public string CompanyId { get; set; }
		[Display(Name = "No Of Years")]
		public int NoOfYears { get; set; }
		public int Quantity { get; set; }
		public decimal TurnOver { get; set; }
		[Display(Name = "Is Active")]
		public bool IsActive { get; set; }
	}
	public class Company
	{
		public int Id { get; set; }
		[Display(Name = "Company")]
		public string CompanyName { get; set; }
	}
	public class WarehouseFacilities
	{
		public int Id { get; set; }
		[Display(Name = "warehouse")]
		public string warehouseId { get; set; }
		public int Distance { get; set; }
		public int Freight { get; set; }
	}
	public class RailFacilities
	{
		public int Id { get; set; }
		[Display(Name = "Rail Facilities Name")]
		public string RailFacilitiesName { get; set; }
		public int Distance { get; set; }
		public int Freight { get; set; }
	}
	public class PortFacilities
	{
		public int Id { get; set; }
		[Display(Name = "Port")]
		public string PortId { get; set; }
		public int Distance { get; set; }
		public int Freight { get; set; }
	}
	public class RegionalBlocks
	{
		public int Id { get; set; }
		[Display(Name = "Name Of Block")]
		public string NameOfBlock { get; set; }
		[Display(Name = "Major Crops")]
		public string MajorCrops { get; set; }
		[Display(Name = "No Of Dealers")]
		public int NoOfDealer { get; set; }
		[Display(Name = "No Of Farmer")]
		public int NoOfFarmer { get; set; }
		[Display(Name = "Season From Month")]
		public string SeasonFromMonth { get; set; }
		[Display(Name = "Season To Month")]
		public string SeasonToMonth { get; set; }
		public IrrigationType Irrigation { get; set; }
	}
	public class BusinessEntityType
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
	public class OwnerShipInfo
	{
		public int Id { get; set; }
		[Display(Name = "Proprietor Name")]
		public string Name { get; set; }
		[Display(Name = "Father Name")]
		public string FatherName { get; set; }
		public string Email { get; set; }
		[Display(Name = "Aadhaar Number")]
		public string AadhaarNumber { get; set; }
		public string AadhaarFilePath { get; set; }
		[Display(Name = "PAN Number")]
		public string PANNumber { get; set; }
		public string PANFilePath { get; set; }
		public string ProprietorImagePath { get; set; }
		[Display(Name = "Family Member Name")]

		public string FamilyMemberName { get; set; }
		[Display(Name = "Date Of Birth")]

		public DateTime DateOfBirth { get; set; }
		public int Age { get; set; }
		[Display(Name = "Relationship")]
		public string RelationshipId { get; set; }
		public string Occupation { get; set; }
	}
	public class Occupation
	{
		public int Id { get; set; }
		[Display(Name = "Name of Company")]
		public string NameofCompany { get; set; }
		[Display(Name = "Sector")]
		public string SectorId { get; set; }
		[Display(Name = "Annual Turnover")]
		public decimal AnnualTurnover { get; set; }
	}
	public class Investment
	{
		public int Id { get; set; }
		[Display(Name = "Capital Investment")]
		public decimal CapitalInvestment { get; set; }
		[Display(Name = "Remarks")]
		public decimal CapitalInvestmentRemarks { get; set; }
		[Display(Name = "Cash Credit Limit")]
		public string CashCreditLimit { get; set; }
		[Display(Name = "Remarks")]
		public decimal CashCreditLimitRrmarks { get; set; }
	}
	public class BankBranch
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
	public class AssetBankInfo
	{
		public int Id { get; set; }
		public string BankId { get; set; }
		public string BankBranchId { get; set; }
		public string Value { get; set; }
		public string Remarks { get; set; }
		public string FileUploadPath { get; set; }
	}
	public class Land
	{
		public int Id { get; set; }
		public string LandName { get; set; }
		public int SurvayNumber { get; set; }
		public string LandSize { get; set; }
		public decimal PropertyValue { get; set; }
		public string UploadedLandDocumentPath { get; set; }
		public string UploadedECDocumentPath { get; set; }
	}
	public class Building
	{
		public int Id { get; set; }
		public string BuildingName { get; set; }
		public string PropertyValue { get; set; }
		public string SurveyNumber { get; set; }
		public string LandSize { get; set; }
		public string Remarks { get; set; }
		public string UploadedBuildingDocumentPath { get; set; }
		public string UploadedECDocumentPath { get; set; }
	}
	public class Movable
	{
		public int Id { get; set; }
		public string AssetValue { get; set; }
		public string Remarks { get; set; }
	}
	public class Infrastructure
	{
		public int Id { get; set; }
		public string OwnGodownCapacity { get; set; }
		public string RentGodownCapacity { get; set; }
	}
	public class LoanLiabilities
	{
		public int Id { get; set; }
		public string LoanSource { get; set; }
		public decimal LoanValue { get; set; }
		public string Remarks { get; set; }
	}
	public class FiscalYearValuation
	{
		public int Id { get; set; }
		public string Parameter { get; set; }
		public decimal Value { get; set; }
		public string Mark { get; set; }
	}
	public enum DealerStatus
	{
		Active, InActive, Terminated
	}
	public enum IrrigationType
	{
		CANAL, TANK, WELL
	}
}
