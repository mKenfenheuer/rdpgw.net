namespace RDPGW.AspNetCore;

/// <summary>
/// Represents the result of an authentication attempt.
/// </summary>
public class RDPGWAuthenticationResult
{
    private RDPGWAuthenticationResult(bool isAuthenticated, string? userId = null)
    {
        IsAuthenticated = isAuthenticated;
        UserId = userId;
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
    /// Creates a failed authentication result.
    /// </summary>
    public static RDPGWAuthenticationResult Failed() => new RDPGWAuthenticationResult(false);

    /// <summary>
    /// Creates a successful authentication result with the specified user ID.
    /// </summary>
    /// <param name="userId">The ID of the authenticated user.</param>
    public static RDPGWAuthenticationResult Success(string userId) => new RDPGWAuthenticationResult(true, userId);
}
