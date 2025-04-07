namespace RDPGW.AspNetCore;

/// <summary>
/// Defines methods for handling user authorization.
/// </summary>
public interface IRDPGWAuthorizationHandler
{
    /// <summary>
    /// Authorizes a user for a specific resource.
    /// </summary>
    /// <param name="userId">The ID of the user to authorize.</param>
    /// <param name="resource">The resource to authorize access to.</param>
    Task<bool> HandleUserAuthorization(string userId, string resource);
}