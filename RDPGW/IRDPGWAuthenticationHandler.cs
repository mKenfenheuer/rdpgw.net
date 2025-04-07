namespace RDPGW.AspNetCore;

public class RDPGWAuthenticationResult
{
    private RDPGWAuthenticationResult(bool isAuthenticated, string? userId = null)
    {
        IsAuthenticated = isAuthenticated;
        UserId = userId;
    }

    public bool IsAuthenticated { get; private set; }
    public string? UserId { get; private set; }

    public static RDPGWAuthenticationResult Failed() => new RDPGWAuthenticationResult(false);
    public static RDPGWAuthenticationResult Success(string userId) => new RDPGWAuthenticationResult(true, userId);
}

public interface IRDPGWAuthenticationHandler
{
    Task<RDPGWAuthenticationResult> HandleBasicAuth(string auth);
    Task<RDPGWAuthenticationResult> HandleDigestAuth(string auth);
    Task<RDPGWAuthenticationResult> HandleNegotiateAuth(string auth);
}

public interface IRDPGWAuthorizationHandler
{
    Task<bool> HandleUserAuthorization(string userId, string resource);
}