namespace RDPGW.AspNetCore;

/// <summary>
/// Defines methods for handling different types of authentication.
/// </summary>
public interface IRDPGWAuthenticationHandler
{
    /// <summary>
    /// Handles Basic authentication.
    /// </summary>
    /// <param name="auth">The authentication string.</param>
    Task<RDPGWAuthenticationResult> HandleBasicAuth(string auth);

    /// <summary>
    /// Handles Digest authentication.
    /// </summary>
    /// <param name="auth">The authentication string.</param>
    Task<RDPGWAuthenticationResult> HandleDigestAuth(string auth);

    /// <summary>
    /// Handles Negotiate authentication.
    /// </summary>
    /// <param name="auth">The authentication string.</param>
    Task<RDPGWAuthenticationResult> HandleNegotiateAuth(string auth);
}
