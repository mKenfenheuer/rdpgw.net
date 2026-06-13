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

    /// <summary>
    /// Handles validation of a Pluggable Authentication (PAA) cookie carried in the tunnel
    /// creation request (HTTP_EXTENDED_AUTH_PAA). This is invoked during the tunnel phase rather
    /// than the HTTP authentication phase. The default implementation rejects the cookie; override
    /// it to support cookie/token-based pre-authentication.
    /// </summary>
    /// <param name="paaCookie">The raw PAA cookie bytes sent by the client.</param>
    /// <returns>The authentication result for the supplied cookie.</returns>
    Task<RDPGWAuthenticationResult> HandlePAACookieAuth(byte[] paaCookie)
        => Task.FromResult(RDPGWAuthenticationResult.Failed());
}
