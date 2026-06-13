namespace RDPGW.AspNetCore;

/// <summary>
/// Optional hook that maps the resource string a client asks to reach (carried in the .rdp
/// "full address", e.g. a stable resource id rather than a literal host) to the actual host and
/// port the gateway should open the tunnel to, and that is notified of the connection lifecycle.
///
/// This lets a consumer decouple a resource's stable identity from its current network address
/// (for example, resolve an id to a VM's current IP and start that VM on demand before connecting),
/// and track active sessions per resource. It is entirely optional: when no resolver is registered,
/// the gateway connects to the client-supplied resource verbatim, exactly as before.
/// </summary>
public interface IRDPGWResourceResolver
{
    /// <summary>
    /// Resolves a client-requested resource to the concrete host/port to connect to. May perform
    /// side effects such as powering on a backend and waiting until it is reachable.
    /// </summary>
    /// <param name="userId">The authenticated user id (as returned by the authentication handler).</param>
    /// <param name="resource">The resource string supplied by the client (the .rdp full address).</param>
    /// <param name="requestedPort">The port the client requested (from the channel-create request).</param>
    /// <returns>
    /// The host and port to connect to, or <c>null</c> to fall back to connecting to
    /// <paramref name="resource"/> on <paramref name="requestedPort"/> unchanged.
    /// </returns>
    Task<(string Host, ushort Port)?> ResolveAsync(string userId, string resource, ushort requestedPort);

    /// <summary>
    /// Invoked once a tunnel to <paramref name="resource"/> has been established for the user.
    /// </summary>
    Task OnConnectedAsync(string userId, string resource) => Task.CompletedTask;

    /// <summary>
    /// Invoked once a tunnel to <paramref name="resource"/> has been torn down for the user.
    /// </summary>
    Task OnDisconnectedAsync(string userId, string resource) => Task.CompletedTask;
}
