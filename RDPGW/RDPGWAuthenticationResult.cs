namespace RDPGW.AspNetCore;

/// <summary>
/// Represents the result of an authentication attempt.
/// </summary>
public class RDPGWAuthenticationResult
{
    private RDPGWAuthenticationResult(bool isAuthenticated, string? userId = null, string? continuationToken = null)
    {
        IsAuthenticated = isAuthenticated;
        UserId = userId;
        ContinuationToken = continuationToken;
    }

    /// <summary>
    /// Indicates whether the authentication was successful.
    /// </summary>
    public bool IsAuthenticated { get; private set; }

    /// <summary>
    /// The user ID of the authenticated user, if authentication was successful.
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// An optional base64-encoded continuation token returned by a challenge-response scheme
    /// (e.g. an NTLM Type 2 challenge or a SPNEGO reply token). When set on a non-authenticated
    /// result, it is sent back to the client in the WWW-Authenticate header so it can perform the
    /// next leg of the handshake.
    /// </summary>
    public string? ContinuationToken { get; private set; }

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    public static RDPGWAuthenticationResult Failed() => new RDPGWAuthenticationResult(false);

    /// <summary>
    /// Creates an in-progress (not yet authenticated) result for a challenge-response scheme,
    /// carrying a continuation token to send back to the client.
    /// </summary>
    /// <param name="continuationToken">The base64-encoded token to return to the client.</param>
    public static RDPGWAuthenticationResult Challenge(string continuationToken) => new RDPGWAuthenticationResult(false, null, continuationToken);

    /// <summary>
    /// Creates a successful authentication result with the specified user ID.
    /// </summary>
    /// <param name="userId">The ID of the authenticated user.</param>
    public static RDPGWAuthenticationResult Success(string userId) => new RDPGWAuthenticationResult(true, userId);
}
