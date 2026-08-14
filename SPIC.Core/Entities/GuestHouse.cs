namespace SPIC.Core.Entities;

/// <summary>
/// Lifecycle status of a guest house booking.
/// </summary>
public enum GuestHouseBookingStatus
{
	Draft = 0,            // Booking started but not yet confirmed
	PendingPayment = 1,   // Details confirmed, awaiting payment
	Confirmed = 2,        // Paid and confirmed
	Cancelled = 3,        // Cancelled (with possible refund)
	Completed = 4         // Stay completed / checked out
}

/// <summary>
/// Payment status for a guest house booking.
/// </summary>
public enum GuestHousePaymentStatus
{
	Pending = 0,              // No payment attempted yet
	Paid = 1,                 // Payment successful
	Failed = 2,               // Payment attempt failed
	Refunded = 3,             // Full amount refunded after cancellation
	PartiallyRefunded = 4     // Part of the amount refunded
}

/// <summary>
/// Payment methods offered on the payment page.
/// </summary>
public enum GuestHousePaymentMethod
{
	Razorpay = 0,   // Razorpay gateway
	UPI = 1,        // UPI ID / QR code
	Card = 2,       // Credit / Debit Card
	NetBanking = 3  // Net Banking
}

/// <summary>
/// Status of a refund raised against a cancelled booking.
/// </summary>
public enum GuestHouseRefundStatus
{
	Pending = 0,      // Refund requested, not yet started
	Processing = 1,   // Refund initiated / bank processing
	Completed = 2,    // Refund credited to the guest
	Failed = 3        // Refund could not be processed
}

/// <summary>
/// Master record of a guest house (e.g. T-Nagar Guest House, Tirupathi Guest House).
/// Guest house names are data, not hard-coded.
/// </summary>
public class GuestHouse
{
	public int Id { get; set; }                                          // Primary key
	public string Name { get; set; } = string.Empty;                     // Name of the guest house (e.g. "T-Nagar Guest House")
	public string? Address { get; set; }                                 // Full postal address shown on the select-guest-house card
	public string? PhoneNumber { get; set; }                             // Contact phone number displayed on the card
	public string? Description { get; set; }                             // Short description / overview of the guest house
	public bool IsActive { get; set; } = true;                           // Whether the guest house is available for booking

	// Audit
	public string? CreatedBy { get; set; }                               // User who created the record
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the record was created
	public string? UpdatedBy { get; set; }                               // User who last updated the record
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the record was last updated

	// Relationships
	public ICollection<GuestHouseImage> Images { get; set; } = new List<GuestHouseImage>();                      // Gallery images of the guest house
	public ICollection<GuestHouseRoom> Rooms { get; set; } = new List<GuestHouseRoom>();                          // Room types available in this guest house
	public ICollection<GuestHouseCancellationPolicy> CancellationPolicies { get; set; } = new List<GuestHouseCancellationPolicy>(); // Cancellation rules applicable to this guest house
	public ICollection<GuestHouseBooking> Bookings { get; set; } = new List<GuestHouseBooking>();                 // All bookings made against this guest house
}

/// <summary>
/// An image belonging to a guest house. A guest house can have multiple images.
/// </summary>
public class GuestHouseImage
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseId { get; set; }                                // FK to the parent guest house
	public GuestHouse? GuestHouse { get; set; }                          // Navigation to the parent guest house

	public string? FileName { get; set; }                                // Original name of the image file
	public string? FilePath { get; set; }                                // Stored path/location of the image on the server
	public bool IsPrimary { get; set; }                                  // Whether this is the main/cover image
	public int DisplayOrder { get; set; }                                // Order in which the image appears in the gallery
	public bool IsActive { get; set; } = true;                           // Whether the image is currently shown

	// Audit
	public string? CreatedBy { get; set; }                               // User who uploaded the image
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the image was uploaded
}

/// <summary>
/// A room type belonging to a guest house, maintained as an inventory quantity
/// (per the availability design). E.g. AC Deluxe Room, Non AC Standard Room, Family Suite, Dormitory Bed.
/// </summary>
public class GuestHouseRoom
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseId { get; set; }                                // FK to the parent guest house
	public GuestHouse? GuestHouse { get; set; }                          // Navigation to the parent guest house

	public string? RoomType { get; set; }                                // Name of the room type (e.g. "AC Deluxe Room")
	public string? Description { get; set; }                             // Description shown on the room details page
	public int? Capacity { get; set; }                                   // Maximum total occupants the room can hold
	public int? NumberOfAdults { get; set; }                             // Base number of adults the room accommodates (max occupancy)
	public int? NumberOfChildren { get; set; }                           // Number of children accommodated with the adults
	public decimal PricePerNight { get; set; }                           // Base price for one night (₹)
	public decimal? ExtraCotPrice { get; set; }                          // Price per extra cot per night (shown as "Extra Bed ₹250 per night")
	public int AvailableQuantity { get; set; }                           // Inventory / number of such rooms available
	public bool IsActive { get; set; } = true;                           // Whether the room type can be booked

	// Audit
	public string? CreatedBy { get; set; }                               // User who created the record
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the record was created
	public string? UpdatedBy { get; set; }                               // User who last updated the record
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the record was last updated

	// Relationships
	public ICollection<GuestHouseRoomImage> Images { get; set; } = new List<GuestHouseRoomImage>();           // Photos of this room type
	public ICollection<GuestHouseRoomAmenity> Amenities { get; set; } = new List<GuestHouseRoomAmenity>();    // Amenities (AC, WiFi, TV, Hot Water, etc.)
	public ICollection<GuestHouseRoomAvailability> Availabilities { get; set; } = new List<GuestHouseRoomAvailability>(); // Per-date availability records
	public ICollection<GuestHouseBooking> Bookings { get; set; } = new List<GuestHouseBooking>();             // Bookings made for this room type
}

/// <summary>
/// An image belonging to a room type. A room can have multiple images.
/// </summary>
public class GuestHouseRoomImage
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseRoomId { get; set; }                            // FK to the parent room type
	public GuestHouseRoom? GuestHouseRoom { get; set; }                  // Navigation to the parent room type

	public string? FileName { get; set; }                                // Original name of the image file
	public string? FilePath { get; set; }                                // Stored path/location of the image on the server
	public bool IsPrimary { get; set; }                                  // Whether this is the main/cover image of the room
	public int DisplayOrder { get; set; }                                // Order in which the image appears in the gallery
	public bool IsActive { get; set; } = true;                           // Whether the image is currently shown

	// Audit
	public string? CreatedBy { get; set; }                               // User who uploaded the image
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the image was uploaded
}

/// <summary>
/// An amenity available in a room type (AC, WiFi, TV, Hot Water, Parking, Locker, Common Bath, Breakfast, etc.).
/// </summary>
public class GuestHouseRoomAmenity
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseRoomId { get; set; }                            // FK to the parent room type
	public GuestHouseRoom? GuestHouseRoom { get; set; }                  // Navigation to the parent room type

	public string? AmenityName { get; set; }                             // Amenity name (e.g. "AC", "WiFi", "TV", "Breakfast")
	public bool IsActive { get; set; } = true;                           // Whether the amenity is currently shown

	// Audit
	public string? CreatedBy { get; set; }                               // User who created the record
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the record was created
}

/// <summary>
/// Per-date availability for a room type, used later for the check-in → check-out availability lookup.
/// </summary>
public class GuestHouseRoomAvailability
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseRoomId { get; set; }                            // FK to the parent room type
	public GuestHouseRoom? GuestHouseRoom { get; set; }                  // Navigation to the parent room type

	public DateTime Date { get; set; }                                   // The date this availability record applies to
	public int TotalRooms { get; set; }                                  // Total number of rooms of this type
	public int AvailableRooms { get; set; }                              // Rooms still free on this date
	public int BookedRooms { get; set; }                                 // Rooms already booked on this date
	public bool IsBlocked { get; set; }                                  // Whether the date is blocked (no bookings allowed)

	// Audit
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the record was created
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the record was last updated
}

/// <summary>
/// A guest house booking placed by a dealer/employee. Prices are snapshotted at booking time.
/// </summary>
public class GuestHouseBooking
{
	public int Id { get; set; }                                          // Primary key
	public string? BookingReference { get; set; }                        // Generated public reference shown to the user (e.g. "BK7A3K9XY")
	public int GuestHouseId { get; set; }                                // FK to the booked guest house
	public GuestHouse? GuestHouse { get; set; }                          // Navigation to the booked guest house
	public int GuestHouseRoomId { get; set; }                            // FK to the booked room type
	public GuestHouseRoom? GuestHouseRoom { get; set; }                  // Navigation to the booked room type

	// Booking Dates
	public DateTime? CheckInDate { get; set; }                           // Date of check-in
	public TimeSpan? CheckInTime { get; set; }                           // Time of check-in (e.g. 2 PM)
	public DateTime? CheckOutDate { get; set; }                          // Date of check-out
	public TimeSpan? CheckOutTime { get; set; }                          // Time of check-out (e.g. 12 AM)

	// Booking Quantity
	public int? NumberOfNights { get; set; }                             // Stay length in nights
	public int? NumberOfPersons { get; set; }                            // Total number of guests
	public int? NumberOfAdults { get; set; }                             // Number of adult guests
	public int? NumberOfChildren { get; set; }                           // Number of children (below 12 years)
	public int? ExtraCotQuantity { get; set; }                           // Number of extra cots booked

	// Pricing
	public decimal RoomPrice { get; set; }                               // Room price per night snapshot (₹)
	public decimal? ExtraCotPrice { get; set; }                          // Extra cot price per night snapshot (₹)
	public decimal? SubTotal { get; set; }                               // Room total + extra cot total before tax
	public decimal? TaxAmount { get; set; }                              // Applicable tax amount (e.g. 12% GST)
	public decimal? TotalAmount { get; set; }                            // Grand total payable

	// Status
	public GuestHouseBookingStatus BookingStatus { get; set; } = GuestHouseBookingStatus.Draft; // Current booking stage
	public GuestHousePaymentStatus PaymentStatus { get; set; } = GuestHousePaymentStatus.Pending; // Payment stage of the booking

	// Audit
	public string? CreatedBy { get; set; }                               // User who created the booking
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the booking was created
	public string? UpdatedBy { get; set; }                               // User who last updated the booking
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the booking was last updated

	// Relationships
	public ICollection<GuestHouseBookingGuest> Guests { get; set; } = new List<GuestHouseBookingGuest>();             // Guest information for this booking
	public ICollection<GuestHouseBookingDocument> Documents { get; set; } = new List<GuestHouseBookingDocument>();    // Uploaded ID proof documents
	public ICollection<GuestHouseBookingPayment> Payments { get; set; } = new List<GuestHouseBookingPayment>();       // Payments made for this booking
	public GuestHouseBookingCancellation? Cancellation { get; set; }     // Cancellation record (if the booking was cancelled)
	public GuestHouseBookingRefund? Refund { get; set; }                 // Refund record (if a refund was raised)
}

/// <summary>
/// Guest information captured for a booking. The dealer/employee code is snapshotted
/// following the project's submitted-record convention; no user entity is duplicated.
/// </summary>
public class GuestHouseBookingGuest
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseBookingId { get; set; }                         // FK to the parent booking
	public GuestHouseBooking? GuestHouseBooking { get; set; }            // Navigation to the parent booking

	public string? EmployeeOrDealerCode { get; set; }                    // Employee ID / Dealer Code entered on the guest form
	public string? GuestName { get; set; }                               // Name of the guest staying
	public string? CompanyName { get; set; }                             // Company / firm of the guest
	public string? PhoneNumber { get; set; }                             // Guest contact number
	public string? Email { get; set; }                                   // Guest email ID
	public string? AadhaarOrPassportNumber { get; set; }                 // Guest ID proof number (Aadhaar / Passport)
	public string? Nationality { get; set; }                             // Nationality (Indian / NRI / Foreigner)
	public int? NumberOfPersons { get; set; }                            // Total number of persons in the stay
	public int? NumberOfAdults { get; set; }                             // Number of adults in the stay
	public int? NumberOfChildren { get; set; }                           // Number of children (below 12 years)
	public string? Address { get; set; }                                 // Guest address

	// Audit
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the guest record was created
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the guest record was last updated
}

/// <summary>
/// An optional ID proof document uploaded against a booking.
/// </summary>
public class GuestHouseBookingDocument
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseBookingId { get; set; }                         // FK to the parent booking
	public GuestHouseBooking? GuestHouseBooking { get; set; }            // Navigation to the parent booking

	public string? DocumentType { get; set; }                            // Type of document (e.g. "Aadhaar", "Passport")
	public string? FileName { get; set; }                                // Original name of the uploaded file
	public string? FilePath { get; set; }                                // Stored path/location of the file on the server
	public string? ContentType { get; set; }                             // MIME type of the file (e.g. "application/pdf", "image/jpeg")
	public long? FileSize { get; set; }                                  // Size of the file in bytes
	public bool IsVerified { get; set; }                                 // Whether the office verified this document
	public string? UploadedBy { get; set; }                              // User who uploaded the document
	public DateTime UploadedAt { get; set; } = DateTime.Now;             // When the document was uploaded
}

/// <summary>
/// A payment attempt/record for a booking. Supports multiple payment methods (Razorpay, UPI, Card, Net Banking).
/// Gateway integration is not implemented yet.
/// </summary>
public class GuestHouseBookingPayment
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseBookingId { get; set; }                         // FK to the parent booking
	public GuestHouseBooking? GuestHouseBooking { get; set; }            // Navigation to the parent booking

	public string? PaymentReference { get; set; }                        // Internal reference for this payment
	public GuestHousePaymentMethod PaymentMethod { get; set; }           // Method used (Razorpay / UPI / Card / NetBanking)
	public GuestHousePaymentStatus PaymentStatus { get; set; } = GuestHousePaymentStatus.Pending; // Outcome of the payment
	public decimal Amount { get; set; }                                  // Amount paid (₹)
	public string? TransactionId { get; set; }                           // Gateway/bank transaction ID
	public DateTime? PaymentDate { get; set; }                           // When the payment was made
	public string? GatewayResponse { get; set; }                         // Raw gateway response / reference (reserved for later integration)

	// Audit
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the payment record was created
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the payment record was last updated
}

/// <summary>
/// Cancellation policy applicable to a guest house (e.g. free up to 24h, 50% refund within 24–48h, no refund within 24h).
/// Rules are data, not hard-coded into the booking entity.
/// </summary>
public class GuestHouseCancellationPolicy
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseId { get; set; }                                // FK to the guest house this policy applies to
	public GuestHouse? GuestHouse { get; set; }                          // Navigation to the guest house

	public string? PolicyName { get; set; }                              // Name of the policy (e.g. "Standard Cancellation Policy")
	public string? Description { get; set; }                             // Human readable rule description shown to the user
	public int? HoursBeforeCheckIn { get; set; }                         // Cut-off: hours before check-in that this policy tier applies
	public decimal? RefundPercentage { get; set; }                       // Percentage of the amount refunded under this tier
	public decimal? CancellationChargePercentage { get; set; }           // Percentage charged as cancellation fee under this tier
	public bool IsActive { get; set; } = true;                           // Whether this policy is currently applicable

	// Audit
	public string? CreatedBy { get; set; }                               // User who created the record
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the record was created
	public string? UpdatedBy { get; set; }                               // User who last updated the record
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the record was last updated
}

/// <summary>
/// Cancellation record for a booking, including the refund calculation snapshot.
/// </summary>
public class GuestHouseBookingCancellation
{
	public int Id { get; set; }                                          // Primary key
	public string? CancellationReference { get; set; }                   // Generated public reference (e.g. "CAN-2025-0089")
	public int GuestHouseBookingId { get; set; }                         // FK to the booking being cancelled
	public GuestHouseBooking? GuestHouseBooking { get; set; }            // Navigation to the booking

	public string? CancellationReason { get; set; }                      // Reason selected by the user (e.g. "Change of Plans")
	public string? CancelledBy { get; set; }                             // Who cancelled (user name / role)
	public DateTime CancelledAt { get; set; } = DateTime.Now;            // When the cancellation was recorded
	public decimal? CancellationCharge { get; set; }                     // Cancellation charges deducted (₹)
	public decimal? TaxAdjustment { get; set; }                          // GST tax adjustment deducted (₹)
	public decimal? RefundAmount { get; set; }                           // Net amount to be refunded (₹)
	public string? RefundMethod { get; set; }                            // How the refund will be made (e.g. "Original Payment Method")
	public GuestHouseRefundStatus RefundStatus { get; set; } = GuestHouseRefundStatus.Pending; // Refund stage
	public DateTime? EstimatedRefundDate { get; set; }                   // Expected refund date shown to the user
	public string? Remarks { get; set; }                                 // Additional remarks
}

/// <summary>
/// Refund record linked to a cancelled booking. No generic refund entity exists in the project, so this is created.
/// </summary>
public class GuestHouseBookingRefund
{
	public int Id { get; set; }                                          // Primary key
	public int GuestHouseBookingId { get; set; }                         // FK to the original booking
	public GuestHouseBooking? GuestHouseBooking { get; set; }            // Navigation to the original booking
	public int? GuestHouseBookingCancellationId { get; set; }            // FK to the cancellation that triggered the refund
	public GuestHouseBookingCancellation? GuestHouseBookingCancellation { get; set; } // Navigation to the cancellation

	public decimal? OriginalAmount { get; set; }                         // Total amount originally paid (₹)
	public decimal? CancellationCharge { get; set; }                     // Cancellation charges deducted (₹)
	public decimal? TaxAdjustment { get; set; }                          // Tax adjustment deducted (₹)
	public decimal? RefundAmount { get; set; }                           // Net amount refunded (₹)
	public string? RefundMethod { get; set; }                            // How the refund was made
	public GuestHouseRefundStatus RefundStatus { get; set; } = GuestHouseRefundStatus.Pending; // Refund stage
	public string? RefundReference { get; set; }                         // Gateway/bank refund reference (e.g. "TXN-AXBK-20250115-9842")
	public DateTime? ProcessedAt { get; set; }                           // When the refund was processed

	// Audit
	public DateTime CreatedAt { get; set; } = DateTime.Now;              // When the record was created
	public DateTime UpdatedAt { get; set; } = DateTime.Now;              // When the record was last updated
}
