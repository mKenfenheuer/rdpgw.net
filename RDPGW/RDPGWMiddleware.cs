using Microsoft.AspNetCore.WebSockets;
using RDPGW.Protocol;

namespace RDPGW.AspNetCore;

/// <summary>
/// Middleware for handling RDP Gateway WebSocket connections.
/// </summary>
public class RDPGWMiddleware
{
    private readonly IRDPGWAuthenticationHandler? _authenticationHandler;
    private readonly IRDPGWAuthorizationHandler? _authorizationHandler;
    private readonly RequestDelegate _next;
    private readonly ILogger<RDPGWMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RDPGWMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance for logging.</param>
    /// <param name="authenticationHandler">Optional authentication handler.</param>
    /// <param name="authorizationHandler">Optional authorization handler.</param>
    public RDPGWMiddleware(RequestDelegate next, ILogger<RDPGWMiddleware> logger, IRDPGWAuthenticationHandler? authenticationHandler = null, IRDPGWAuthorizationHandler? authorizationHandler = null)
    {
        _next = next;
        _logger = logger;
        _authenticationHandler = authenticationHandler;
        _authorizationHandler = authorizationHandler;
    }

    /// <summary>
    /// Middleware entry point for handling HTTP requests.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        // Check if the request is a WebSocket connection request for RDP Gateway.
        var isConnectionRequest = (context.Request.Method == "RDG_OUT_DATA" || context.Request.Method == "RDG_IN_DATA") 
                                  && context.Request.Headers.Connection == "Upgrade" 
                                  && context.Request.Headers.Upgrade == "websocket";

        if (isConnectionRequest)
        {
            // Log the incoming request method for debugging purposes.
            _logger.LogDebug($"Handling incoming Request {context.Request.Method}");
            // If the request is a WebSocket connection request, handle it.
            return HandleConnectionRequest(context);
        }

        // Log a warning for unhandled requests.
        _logger.LogWarning($"Unhandled Request: {context.Request.Method} {context.Request.Path}");

        // Pass the request to the next middleware in the pipeline.
        return _next.Invoke(context);
    }

    /// <summary>
    /// Handles WebSocket connection requests.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleConnectionRequest(HttpContext context)
    {
        // Change the HTTP method to GET as required for WebSocket connections.
        context.Request.Method = "GET";

        string? userId = null;

        // Check if an authentication handler is provided.
        if (_authenticationHandler != null)
        {
            // Retrieve the Authorization header from the request. There may be more than
            // one (the client can offer several schemes); use the first usable one.
            var authHeaderValues = context.Request.Headers.Authorization;

            if (authHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(authHeaderValues[0]))
            {
                // If no Authorization header is present, fail the request as unauthenticated.
                await FailRequestUnauthenticated(context);
                return;
            }

            // Split the Authorization header value to extract the authentication type and credentials.
            var headerValue = authHeaderValues[0]!.Trim();
            var separatorIndex = headerValue.IndexOf(' ');
            var authType = separatorIndex < 0 ? headerValue : headerValue.Substring(0, separatorIndex);
            var credentials = separatorIndex < 0 ? string.Empty : headerValue.Substring(separatorIndex + 1).Trim();

            RDPGWAuthenticationResult result;

            // Handle authentication based on the type specified in the Authorization header.
            switch (authType)
            {
                case "Basic":
                    result = await _authenticationHandler.HandleBasicAuth(credentials);
                    break;

                case "Digest":
                    result = await _authenticationHandler.HandleDigestAuth(credentials);
                    break;

                case "Negotiate":
                case "NTLM":
                    // Negotiate (SPNEGO/Kerberos) and NTLM are challenge-response schemes that
                    // may require multiple round trips. The handler can return a continuation
                    // token that we echo back in a 401 so the client sends the next leg.
                    result = await _authenticationHandler.HandleNegotiateAuth(credentials);
                    break;

                default:
                    // Unknown / unsupported scheme: never treat as authenticated.
                    await FailRequestUnauthenticated(context);
                    return;
            }

            if (!result.IsAuthenticated)
            {
                // If the handler produced a continuation token (e.g. an NTLM Type 2 challenge),
                // send it back so the client can continue the handshake; otherwise plain 401.
                await FailRequestUnauthenticated(context, authType, result.ContinuationToken);
                return;
            }

            userId = result.UserId;
        }

        // Accept the WebSocket connection.
        var socket = await context.WebSockets.AcceptWebSocketAsync();

        // Create a handler for managing the WebSocket connection.
        RDPWebSocketHandler handler = new RDPWebSocketHandler(socket, userId, _authorizationHandler, _authenticationHandler);

        // Start handling the WebSocket connection.
        await handler.HandleConnection();
    }

    /// <summary>
    /// Fails the request with a 401 Unauthorized response.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="scheme">
    /// The authentication scheme being negotiated, when known. For challenge-response schemes
    /// (Negotiate/NTLM) the WWW-Authenticate header for that scheme carries the continuation token.
    /// </param>
    /// <param name="continuationToken">
    /// An optional base64 continuation token (e.g. an NTLM Type 2 challenge) to send back to the
    /// client so it can perform the next leg of the authentication handshake.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task FailRequestUnauthenticated(HttpContext context, string? scheme = null, string? continuationToken = null)
    {
        // If the response has already started we cannot change status/headers; bail out.
        if (context.Response.HasStarted)
            return;

        // Set the response status code to 401 Unauthorized.
        context.Response.StatusCode = 401;

        if (scheme != null && !string.IsNullOrEmpty(continuationToken))
        {
            // Mid-handshake: echo the scheme-specific continuation token back to the client.
            context.Response.Headers.Append("WWW-Authenticate", $"{scheme} {continuationToken}");
        }
        else
        {
            // Initial challenge: advertise the schemes the handler can process.
            context.Response.Headers.Append("WWW-Authenticate", "Negotiate");
            context.Response.Headers.Append("WWW-Authenticate", "Digest");
            context.Response.Headers.Append("WWW-Authenticate", "Basic");
        }

        // Start the response.
        await context.Response.StartAsync();
        return;
    }
}