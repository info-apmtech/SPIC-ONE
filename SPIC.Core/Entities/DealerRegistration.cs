//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Text;

//namespace SPIC.Core.Entities
//{
//	public class DealerRegistration
//	{
//		public int Id { get; set; }
//		[Display(Name = "State")]
//		public int StateId { get; set; }
//		public string Region { get; set; }
//		public string HQ { get; set; }
//		public DealerStatus Status { get; set; }
//		[Display(Name = "Dealer Code and Name")]
//		public string DealerCodeandName { get; set; }
//		[Display(Name = "Parent Dealer")]
//		public string ParentDealer { get; set; }
//		[Display(Name = "Firm Name")]
//		public string FirmName { get; set; }
//		[Display(Name = "Date Of Appointment")]
//		public DateTime DateOfAppointment { get; set; }
//		[Display(Name = "Business Type")]
//		public string BusinessType { get; set; }
//		[Display(Name = "Google Map URL")]
//		public string GoogleMapURL { get; set; }
//		[Display(Name = "Shop No / Room No / Block No")]
//		public string ShopNoORRoomNoOrBlockNo { get; set; }
//		[Display(Name = "Sub Village")]
//		public string SubVillage { get; set; }
//		public string Village { get; set; }
//		[Display(Name = "Pin Code")]
//		public string PinCode { get; set; }
//		public string Block { get; set; }
//		public string Taluk { get; set; }
//		[Display(Name = "District")]
//		public string DistrictId { get; set; }
//		[Display(Name = "Official Contact Number")]
//		public string OfficialContactNumber { get; set; }
//		[Display(Name = "WhatsApp Number")]
//		public string WhatsAppNumber { get; set; }
//		[Display(Name = "Alternative Number")]
//		public string? AlternativeNumber { get; set; }
	
//		public int BankDetailsId { get; set; }
//		[Display(Name = "GST Number")]
//		public string GSTNumber { get; set; }
//		public string GSTFilePath { get; set; }
//		[Display(Name = "PAN Number")]
//		public string PANNumber { get; set; }
//		public string PANFilePath { get; set; }
//		[Display(Name = "Aadhaar Number")]
//		public string AadhaarNumber { get; set; }
//		public string AadhaarFilePath { get; set; }
//		public int WholeSaleFertilizerId { get; set; }
//		public int RetailFertilizerId { get; set; }
//		public bool IsOfficeAutomation { get; set; }
//		public bool IsSDWA { get; set; }
//		public int ExperienceId { get; set; }
//	}
//	public class BankDetails
//	{
//		public int Id { get; set; }
//		[Display(Name = "Accound Holder Name")]
//		public string AccoundHolderName { get; set; }
//		[Display(Name = "Accound Number")]
//		public string AccoundNumber { get; set; }
//		[Display(Name = "Bank")]
//		public string BankId { get; set; }
//	}
//	public class WholeSaleFertilizer
//	{
//		public int Id { get; set; }
//		[Display(Name = "WholeSale Fertilizer License Details")]
//		public string LicenseDetails { get; set; }
//		[Display(Name = "Expiry Date")]
//		public DateTime ExpiryDate { get; set; }
//		[Display(Name = "nFMS Code")]
//		public string nFMSCode { get; set; }
//		public string LicenseFilePath { get; set; }
//	}
//	public class RetailFertilizer
//	{
//		public int Id { get; set; }
//		[Display(Name = "Retail Fertilizer License Details")]
//		public string LicenseDetails { get; set; }
//		[Display(Name = "Expiry Date")]
//		public DateTime ExpiryDate { get; set; }
//		[Display(Name = "nFMS Code")]
//		public string nFMSCode { get; set; }
//		public string LicenseFilePath { get; set; }
//	}
//	public class Experience
//	{
//		public int Id { get; set; }
//		public string CompanyId { get; set; }
//		public int NoOfYears { get; set; }
//		public int Quantity { get; set; }
//		public decimal TurnOver { get; set; }
//		public bool IsActive { get; set; }
//	}
//	public class Company
//	{
//		public int Id { get; set; }
//		public string CompanyName { get; set; }
//	}
//	public class WarehouseFacilities
//	{
//		public int Id { get; set; }
//		public string warehouseId { get; set; }
//		public int Distance { get; set; }
//		public int Freight { get; set; }
//	}
//	public enum DealerStatus
//	{
//		Active, InActive, Terminated
//	}
//}
