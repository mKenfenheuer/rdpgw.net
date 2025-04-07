using Microsoft.AspNetCore.WebSockets;
using RDPGW.Protocol;

namespace RDPGW.AspNetCore;



public class RDPGWMiddleware
{
    private readonly IRDPGWAuthenticationHandler? _authenticationHandler;
    private readonly IRDPGWAuthorizationHandler? _authorizationHandler;
    private readonly RequestDelegate _next;
    private readonly ILogger<RDPGWMiddleware> _logger;
    public RDPGWMiddleware(RequestDelegate next, ILogger<RDPGWMiddleware> logger, IRDPGWAuthenticationHandler? authenticationHandler = null, IRDPGWAuthorizationHandler? authorizationHandler = null)
    {
        _next = next;
        _logger = logger;
        _authenticationHandler = authenticationHandler;
        _authorizationHandler = authorizationHandler;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var isConnectionRequest = (context.Request.Method == "RDG_OUT_DATA" || context.Request.Method == "RDG_IN_DATA") && context.Request.Headers.Connection == "Upgrade" && context.Request.Headers.Upgrade == "websocket";

        _logger.LogInformation($"Handling incoming Request {context.Request.Method}");

        if (isConnectionRequest)
            return HandleConnectionRequest(context);

        _logger.LogWarning($"Unhandled Request: {context.Request.Method} {context.Request.Path}");
        return _next.Invoke(context);
    }

    private async Task HandleConnectionRequest(HttpContext context)
    {
        context.Request.Method = "GET";

        string? userId = null;

        if (_authenticationHandler != null)
        {
            var authHeader = context.Request.Headers.FirstOrDefault(h => h.Key == "Authorization");

            if (authHeader.Key == null)
            {
                await FailRequestUnauthenticated(context);
                return;
            }
            else
            {
                var data = authHeader.Value.ToString().Split(" ");
                var authType = data[0];
                switch (authType)
                {
                    case "Basic":
                        var basicResult = await _authenticationHandler.HandleBasicAuth(String.Join(" ", data.Skip(1)));
                        if(!basicResult.IsAuthenticated)
                        {
                            await FailRequestUnauthenticated(context);
                            return;
                        }
                        userId = basicResult.UserId;
                        break;
                    case "Digest":
                        var digestResult = await _authenticationHandler.HandleDigestAuth(String.Join(" ", data.Skip(1)));
                        if(!digestResult.IsAuthenticated)
                        {
                            await FailRequestUnauthenticated(context);
                            return;
                        }
                        userId = digestResult.UserId;
                        break;
                    case "Negotiate":
                        var negotiateResult = await _authenticationHandler.HandleNegotiateAuth(String.Join(" ", data.Skip(1)));
                        if(!negotiateResult.IsAuthenticated)
                        {
                            await FailRequestUnauthenticated(context);
                            return;
                        }
                        userId = negotiateResult.UserId;
                        break;
                }
            }
        }

        var socket = await context.WebSockets.AcceptWebSocketAsync();

        RDPWebSocketHandler handler = new RDPWebSocketHandler(socket, userId, _authorizationHandler);
        await handler.HandleConnection();
    }

    private async Task FailRequestUnauthenticated(HttpContext context)
    {
        context.Response.StatusCode = 401;
        context.Response.Headers.Append("WWW-Authenticate", "Basic");
        context.Response.Headers.Append("WWW-Authenticate", "Digest");
        context.Response.Headers.Append("WWW-Authenticate", "Negotiate");
        await context.Response.StartAsync();
        return;
    }
}