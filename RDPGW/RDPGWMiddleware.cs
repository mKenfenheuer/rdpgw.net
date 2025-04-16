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
        var isConnectionRequest = (context.Request.Method == "RDG_OUT_DATA" || context.Request.Method == "RDG_IN_DATA" || context.Request.Method == "GET") 
                                  && context.Request.Path.StartsWithSegments("/remoteDesktopGateway")
                                  && context.Request.Headers.Connection == "Upgrade" 
                                  && context.Request.Headers.Upgrade == "websocket";

        if (isConnectionRequest)
        {
            // Log the incoming request method for debugging purposes.
            _logger.LogDebug($"Handling incoming Request {context.Request.Method} {context.Request.Path}");
            // If the request is a WebSocket connection request, handle it.
            return HandleConnectionRequest(context);
        }

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
            // Retrieve the Authorization header from the request.
            var authHeader = context.Request.Headers.FirstOrDefault(h => h.Key == "Authorization");

            if (authHeader.Key == null)
            {
                // If no Authorization header is present, fail the request as unauthenticated.
                await FailRequestUnauthenticated(context);
                return;
            }
            else
            {
                // Split the Authorization header value to extract the authentication type and credentials.
                var data = authHeader.Value.ToString().Split(" ");
                var authType = data[0];

                // Handle authentication based on the type specified in the Authorization header.
                switch (authType)
                {
                    case "Basic":
                        // Handle Basic authentication.
                        var basicResult = await _authenticationHandler.HandleBasicAuth(String.Join(" ", data.Skip(1)));
                        if (!basicResult.IsAuthenticated)
                        {
                            // If authentication fails, respond with 401 Unauthorized.
                            await FailRequestUnauthenticated(context);
                            return;
                        }
                        userId = basicResult.UserId;
                        break;

                    case "Digest":
                        // Handle Digest authentication.
                        var digestResult = await _authenticationHandler.HandleDigestAuth(String.Join(" ", data.Skip(1)));
                        if (!digestResult.IsAuthenticated)
                        {
                            // If authentication fails, respond with 401 Unauthorized.
                            await FailRequestUnauthenticated(context);
                            return;
                        }
                        userId = digestResult.UserId;
                        break;

                    case "Negotiate":
                        // Handle Negotiate (Kerberos/NTLM) authentication.
                        var negotiateResult = await _authenticationHandler.HandleNegotiateAuth(String.Join(" ", data.Skip(1)));
                        if (!negotiateResult.IsAuthenticated)
                        {
                            // If authentication fails, respond with 401 Unauthorized.
                            await FailRequestUnauthenticated(context);
                            return;
                        }
                        userId = negotiateResult.UserId;
                        break;
                }
            }
        }

        // Accept the WebSocket connection.
        var socket = await context.WebSockets.AcceptWebSocketAsync();

        // Create a handler for managing the WebSocket connection.
        RDPWebSocketHandler handler = new RDPWebSocketHandler(socket, userId, _authorizationHandler);

        // Start handling the WebSocket connection.
        await handler.HandleConnection();
    }

    /// <summary>
    /// Fails the request with a 401 Unauthorized response.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task FailRequestUnauthenticated(HttpContext context)
    {
        // Set the response status code to 401 Unauthorized.
        context.Response.StatusCode = 401;

        // Add WWW-Authenticate headers for supported authentication schemes.
        context.Response.Headers.Append("WWW-Authenticate", "Basic");
        context.Response.Headers.Append("WWW-Authenticate", "Digest");
        context.Response.Headers.Append("WWW-Authenticate", "Negotiate");

        // Start the response.
        await context.Response.StartAsync();
        return;
    }
}