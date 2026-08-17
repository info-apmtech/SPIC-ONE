using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

public enum ContactCardType
{
	Address = 0,
	Email = 1,
	Phone = 2
}

public enum ContactActionType
{
	WhatsApp = 0,
	Call = 1,
	Email = 2,
	Maps = 3
}

public class ContactInformation
{
	public string Title { get; set; } = string.Empty;

	public string Icon { get; set; } = string.Empty;

	public string? Description { get; set; }

	public string ContactValue { get; set; } = string.Empty;

	public string? AdditionalInformation { get; set; }

	public string? ButtonText { get; set; }

	public string? ButtonUrl { get; set; }

	public ContactCardType CardType { get; set; }
}

public class QuickContactAction
{
	public string ActionName { get; set; } = string.Empty;

	public string Icon { get; set; } = string.Empty;

	public ContactActionType ActionType { get; set; }

	public string? Target { get; set; }

	public int DisplayOrder { get; set; }

	public bool IsActive { get; set; } = true;
}

public class LocationInformation
{
	public string LocationName { get; set; } = string.Empty;

	public string Address { get; set; } = string.Empty;

	public double? Latitude { get; set; }

	public double? Longitude { get; set; }

	public string? GoogleMapsUrl { get; set; }

	public string? GetDirectionsUrl { get; set; }

	public string? MapImageUrl { get; set; }
}

public class ContactUsViewModel
{
	public string PageTitle { get; set; } = string.Empty;

	public string PageDescription { get; set; } = string.Empty;

	public List<ContactInformation> ContactInformation { get; set; } = new();

	public List<QuickContactAction> QuickContactActions { get; set; } = new();

	public LocationInformation LocationInformation { get; set; } = new();

	public ContactUsMessage ContactUsMessage { get; set; } = new();
}
