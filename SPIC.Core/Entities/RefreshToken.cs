namespace SPIC.Core.Entities;

// ------------------------------------------------------------------------------------------------
// Refresh tokens: lets a client trade in a still-valid refresh token for a new short-lived JWT
// (api/Authentication/refresh) instead of forcing a full re-login every time the 60-minute JWT
// expires. Needed primarily for the JMDO native background tracking service, which can easily
// outlive a single JWT while the app is backgrounded - but used by every client (AuthHttpMessageHandler
// attempts one silent refresh on any 401 before forcing logout).
// ------------------------------------------------------------------------------------------------

/// <summary>One issued refresh token. The raw token value is never stored - only its hash
/// (TokenHash), so a database read alone can never be used to impersonate a user. Rotated on every
/// use: redeeming a token immediately revokes it and issues a new one (ReplacedByTokenHash links the
/// chain), so a refresh token is effectively single-use - replaying an already-used one is a reuse/
/// theft signal, not a valid way to get a second JWT from the same token.</summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the raw token (the raw value is returned to the client exactly
    /// once, at issuance, and never persisted anywhere server-side).</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    /// <summary>Set when this token was redeemed and rotated into a newer one - the hash of that
    /// newer token, so a revoked-via-rotation token can be told apart from a revoked-via-logout one
    /// if that distinction is ever needed (e.g. reuse-detection reporting).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? RevokedAt { get; set; }
}
