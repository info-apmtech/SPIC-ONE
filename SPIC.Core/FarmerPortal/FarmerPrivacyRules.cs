namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Farmer privacy notice and account deletion (final fix wave, review I4; docs/farmer-portal.md "Hardening").
/// The notice text on /farmer/privacy is a DRAFT for SPIC legal review. Consent is recorded on the farmer profile
/// (FarmerProfiles.PrivacyAcceptedAt / PrivacyVersion) at the first profile save and at a sign-in that continues past
/// the login consent line when the recorded version is older than <see cref="Version"/>.
/// </summary>
public static class FarmerPrivacyRules
{
    /// <summary>The privacy notice version. Change it when the notice text changes (farmers then accept it again at sign-in).</summary>
    public const string Version = "2026-11-28";

    public const string PrivacyPath = "/farmer/privacy";
    public const string DeleteInfoPath = "/farmer/delete-account";

    /// <summary>True when the profile has not accepted this version of the notice yet.</summary>
    public static bool NeedsAcceptance(string? acceptedVersion) =>
        !string.Equals(acceptedVersion, Version, StringComparison.Ordinal);

    /// <summary>What "Delete my account" removes, in the farmer's words (the confirm sheet and the public page list it).</summary>
    public static readonly IReadOnlyList<string> RemovedItems = new[]
    {
        "Your profile: name, phone number, village, farm location (GPS), crops, land and irrigation",
        "Your fertilizer plans and harvest forecasts",
        "Your leaf checks and the leaf photos you took",
        "Your questions and answers with SPIC AI",
        "Your saved sign-ins on every phone",
        "Call-back requests your SPIC officer has not answered yet",
        "Your Community discussions and replies (they are taken down) and your Community photo",
    };

    /// <summary>What stays, and why (shown on the same sheet and page).</summary>
    public static readonly IReadOnlyList<string> KeptItems = new[]
    {
        "Call-back requests an officer already handled stay in their work history, without your name or phone number",
        "Soil test samples and reports in the SPIC lab stay with the lab (they are not linked to this app any more)",
        "Daily counts of SPIC AI answers and leaf checks stay, with no name or number, so the app's daily limits stay fair",
        "Sign-in codes already sent stay until they are removed automatically after one day",
    };

    public const string CannotUndo = "This cannot be undone. You can sign up again later with the same number, as a new account.";

    /// <summary>
    /// The farmer id left on a deleted farmer's AI usage rows (residual fix R2). Not a user id (user ids are GUIDs),
    /// so no per-farmer count ever matches it; the global ceiling still counts the rows.
    /// </summary>
    public const string DeletedUserTombstone = "deleted-account";

    /// <summary>The author name left on taken-down Community rows of a deleted farmer.</summary>
    public const string DeletedAuthorName = "Deleted account";
}
