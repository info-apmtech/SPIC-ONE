namespace SPIC.Core.Entities;

/// <summary>
/// The Guest House guest-count rule, in one place so the Guest Details page and the booking API apply
/// exactly the same check (the page alone is never enough - the API is called directly by anything).
///
///   Number of Persons is required (at least 1);
///   Number of Adults is required (at least 1: a stay cannot consist of children only);
///   Number of Children is optional (empty means 0);
///   Adults + Children must not exceed Persons.
/// </summary>
public static class GuestHouseGuestCounts
{
	public const string TotalExceedsPersonsMessage = "Total adults and children cannot exceed the selected number of persons.";

	/// <summary>Returns the first problem with the counts, or null when they are acceptable.</summary>
	public static string? Validate(int? persons, int? adults, int? children)
	{
		if (persons is null)
			return "Please select the number of persons.";
		if (persons < 1)
			return "Number of persons must be at least 1.";

		if (adults is null)
			return "Please select the number of adults.";
		if (adults < 1)
			return "At least one adult is required.";

		if (children is < 0)
			return "Number of children cannot be negative.";

		if (adults.Value + (children ?? 0) > persons.Value)
			return TotalExceedsPersonsMessage;

		return null;
	}

	/// <summary>
	/// Parses the page's string values (select values) into counts. Anything that is not a whole number
	/// - including text typed into a tampered request - becomes null, i.e. "not provided", which
	/// <see cref="Validate"/> then rejects for persons and adults.
	/// </summary>
	public static int? Parse(string? value) => int.TryParse(value, out var n) ? n : null;
}
