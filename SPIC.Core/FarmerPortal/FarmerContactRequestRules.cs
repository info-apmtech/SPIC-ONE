using SPIC.Core.Entities;

namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Call-back request rules (POST api/Farmer/contacts/{contactId}/request, PUT api/FarmerRequests/{id}):
/// topics, preferred times, the message / note limits and the open-request cap. Shared by the API
/// (authoritative) and the request sheet. Pure, pinned by the harness.
/// </summary>
public static class FarmerContactRequestRules
{
    public const int MessageMax = 500;
    public const int NoteMax = 500;

    /// <summary>At most this many Open requests from one farmer to one staff member.</summary>
    public const int MaxOpenPerStaff = 3;

    /// <summary>Topic labels, in the order the sheet shows them.</summary>
    public static readonly IReadOnlyList<(FarmerContactTopic Topic, string Label, string Icon)> Topics = new[]
    {
        (FarmerContactTopic.Fertilizer, "Fertilizer", "bi-basket2"),
        (FarmerContactTopic.CropProblem, "Crop problem", "bi-bug"),
        (FarmerContactTopic.SoilTest, "Soil test", "bi-moisture"),
        (FarmerContactTopic.Product, "Product", "bi-box-seam"),
        (FarmerContactTopic.Dealer, "Dealer", "bi-shop"),
        (FarmerContactTopic.Other, "Other", "bi-chat-dots")
    };

    /// <summary>Preferred call times: stored key -> label.</summary>
    public static readonly IReadOnlyList<(string Key, string Label)> PreferredTimes = new[]
    {
        ("Morning", "Morning (9 am - 12 pm)"),
        ("Afternoon", "Afternoon (12 - 4 pm)"),
        ("Evening", "Evening (4 - 7 pm)"),
        ("Anytime", "Any time")
    };

    public static string TopicLabel(FarmerContactTopic topic) =>
        Topics.FirstOrDefault(t => t.Topic == topic).Label ?? topic.ToString();

    public static string TopicIcon(FarmerContactTopic topic) =>
        Topics.FirstOrDefault(t => t.Topic == topic).Icon ?? "bi-chat-dots";

    public static string? PreferredTimeLabel(string? key) =>
        key is null ? null : PreferredTimes.FirstOrDefault(p => p.Key == key).Label;

    public static string StatusLabel(FarmerContactRequestStatus status) => status switch
    {
        FarmerContactRequestStatus.Open => "Open",
        FarmerContactRequestStatus.Contacted => "Contacted",
        FarmerContactRequestStatus.Closed => "Closed",
        _ => status.ToString()
    };

    /// <summary>The message as stored: trimmed, "" when blank.</summary>
    public static string CleanText(string? text) => string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();

    /// <summary>
    /// Field errors (field name -> plain words) for a new request; empty when valid. Topic must be one
    /// of the six; the message is optional (required for "Other"), at most 500 characters; the
    /// preferred time is optional and must be one of the listed keys.
    /// </summary>
    public static Dictionary<string, string> Validate(FarmerContactTopic? topic, string? message, string? preferredTime)
    {
        var errors = new Dictionary<string, string>();
        if (topic is null || !Enum.IsDefined(topic.Value))
            errors["topic"] = "Please choose what you need help with.";

        var text = CleanText(message);
        if (text.Length > MessageMax)
            errors["message"] = $"Please keep the message under {MessageMax} letters.";
        else if (topic == FarmerContactTopic.Other && text.Length == 0)
            errors["message"] = "Please tell us a little about what you need.";

        if (!string.IsNullOrWhiteSpace(preferredTime) && PreferredTimes.All(p => p.Key != preferredTime.Trim()))
            errors["preferredTime"] = "Please choose a time from the list.";

        return errors;
    }

    /// <summary>True when the farmer may open another request to this staff member.</summary>
    public static bool CanOpenAnother(int openRequestsToThisStaff) => openRequestsToThisStaff < MaxOpenPerStaff;

    /// <summary>Errors for a staff update: a defined status and a note of at most 500 characters.</summary>
    public static Dictionary<string, string> ValidateUpdate(FarmerContactRequestStatus? status, string? note)
    {
        var errors = new Dictionary<string, string>();
        if (status is null || !Enum.IsDefined(status.Value))
            errors["status"] = "Choose Open, Contacted or Closed.";
        if (CleanText(note).Length > NoteMax)
            errors["note"] = $"The note can have at most {NoteMax} characters.";
        return errors;
    }

    /// <summary>The roles that see the staff page: the addressed roles plus Admin / SuperAdmin (view all).</summary>
    public static bool CanUseStaffPage(AppRole? role) =>
        role is { } r && (FarmerTeamRules.TeamRoles.Contains(r) || IsAdminViewer(r));

    /// <summary>
    /// The farmer's phone on the staff page: the full number only for the officer the request is addressed
    /// to (the farmer asked THEM to call); anyone else (Admin / SuperAdmin viewing all) gets it masked,
    /// "98xxxxxx21", and no call link.
    /// </summary>
    public static string? PhoneForStaffView(string? farmerPhone, bool callerIsAddressee) =>
        string.IsNullOrWhiteSpace(farmerPhone) ? null
        : callerIsAddressee ? farmerPhone
        : FarmerPhone.Mask(farmerPhone);

    /// <summary>Admin / SuperAdmin see every request (read-only).</summary>
    public static bool IsAdminViewer(AppRole? role) => role is AppRole.Admin or AppRole.SuperAdmin;
}
