using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.Entities;

public enum EnquiryType
{
	GeneralEnquiry = 0,
	WelfareSchemes = 1,
	RoomBookings = 2,
	SalesEnquiry = 3,
	TechnicalSupport = 4,
	Service = 5,
	Feedback = 6,
	Other = 7
}

public enum ContactMessageStatus
{
	New = 0,
	InProgress = 1,
	Resolved = 2,
	Closed = 3
}

public class ContactUsMessage
{
	[Key]
	public int Id { get; set; }

	[Required]
	[StringLength(100)]
	public string FullName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	[StringLength(150)]
	public string EmailAddress { get; set; } = string.Empty;

	[Phone]
	[StringLength(20)]
	public string? PhoneNumber { get; set; }

	[Required]
	public EnquiryType? EnquiryType { get; set; }

	[Required]
	[StringLength(200)]
	public string Subject { get; set; } = string.Empty;

	[Required]
	[StringLength(2000)]
	public string Message { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public ContactMessageStatus Status { get; set; } = ContactMessageStatus.New;
}
