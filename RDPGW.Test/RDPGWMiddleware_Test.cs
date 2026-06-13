using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using RDPGW.AspNetCore;
using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed class RDPGWMiddleware_Test
{
    /// <summary>
    /// Stub authentication handler whose behavior is driven per-test.
    /// </summary>
    private class StubAuthHandler : IRDPGWAuthenticationHandler
    {
        public Func<string, RDPGWAuthenticationResult> Basic { get; set; } = _ => RDPGWAuthenticationResult.Failed();
        public Func<string, RDPGWAuthenticationResult> Digest { get; set; } = _ => RDPGWAuthenticationResult.Failed();
        public Func<string, RDPGWAuthenticationResult> Negotiate { get; set; } = _ => RDPGWAuthenticationResult.Failed();

        public Task<RDPGWAuthenticationResult> HandleBasicAuth(string auth) => Task.FromResult(Basic(auth));
        public Task<RDPGWAuthenticationResult> HandleDigestAuth(string auth) => Task.FromResult(Digest(auth));
        public Task<RDPGWAuthenticationResult> HandleNegotiateAuth(string auth) => Task.FromResult(Negotiate(auth));
    }

    private static HttpContext MakeConnectionRequest(string? authHeader)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "RDG_OUT_DATA";
        context.Request.Path = "/remoteDesktopGateway";
        context.Request.Headers.Connection = "Upgrade";
        context.Request.Headers.Upgrade = "websocket";
        if (authHeader != null)
            context.Request.Headers.Authorization = authHeader;
        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>
    /// Builds the middleware, recording whether the next delegate in the pipeline was invoked.
    /// </summary>
    private static RDPGWMiddleware MakeMiddleware(IRDPGWAuthenticationHandler? auth, bool[] nextCalled)
    {
        return new RDPGWMiddleware(
            next: _ => { nextCalled[0] = true; return Task.CompletedTask; },
            logger: NullLogger<RDPGWMiddleware>.Instance,
            wslogger: NullLogger<RDPWebSocketHandler>.Instance,
            authenticationHandler: auth);
    }

    [TestMethod]
    public async Task NonConnectionRequestPassesThrough()
    {
        var nextCalled = new bool[1];
        var middleware = MakeMiddleware(null, nextCalled);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        await middleware.InvokeAsync(context);

        Assert.IsTrue(nextCalled[0], "Non-RDG requests must be forwarded to the next middleware.");
    }

    [TestMethod]
    public async Task MissingAuthorizationHeaderReturns401WithChallenges()
    {
        var nextCalled = new bool[1];
        var middleware = MakeMiddleware(new StubAuthHandler(), nextCalled);
        var context = MakeConnectionRequest(authHeader: null);

        await middleware.InvokeAsync(context);

        Assert.AreEqual(401, context.Response.StatusCode);
        var schemes = context.Response.Headers["WWW-Authenticate"].ToString();
        StringAssert.Contains(schemes, "Basic");
        StringAssert.Contains(schemes, "Negotiate");
        Assert.IsFalse(nextCalled[0], "Unauthenticated requests must not continue.");
    }

    [TestMethod]
    public async Task UnknownSchemeIsRejected()
    {
        // An unknown scheme must never be treated as authenticated.
        var nextCalled = new bool[1];
        var middleware = MakeMiddleware(new StubAuthHandler(), nextCalled);
        var context = MakeConnectionRequest("Bearer sometoken");

        await middleware.InvokeAsync(context);

        Assert.AreEqual(401, context.Response.StatusCode, "Unknown auth schemes must be rejected with 401.");
        Assert.IsFalse(nextCalled[0], "Unknown schemes must not continue the pipeline.");
    }

    [TestMethod]
    public async Task FailedBasicAuthReturns401()
    {
        var auth = new StubAuthHandler { Basic = _ => RDPGWAuthenticationResult.Failed() };
        var nextCalled = new bool[1];
        var middleware = MakeMiddleware(auth, nextCalled);
        var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:wrong"));
        var context = MakeConnectionRequest($"Basic {creds}");

        await middleware.InvokeAsync(context);

        Assert.AreEqual(401, context.Response.StatusCode);
    }

    [TestMethod]
    public async Task NegotiateChallengeEchoesContinuationToken()
    {
        // A challenge result must echo its token back in a scheme-specific WWW-Authenticate header.
        var auth = new StubAuthHandler { Negotiate = _ => RDPGWAuthenticationResult.Challenge("TlRMTVNTUAAC") };
        var nextCalled = new bool[1];
        var middleware = MakeMiddleware(auth, nextCalled);
        var context = MakeConnectionRequest("Negotiate TlRMTVNTUAAB");

        await middleware.InvokeAsync(context);

        Assert.AreEqual(401, context.Response.StatusCode);
        var header = context.Response.Headers["WWW-Authenticate"].ToString();
        StringAssert.Contains(header, "Negotiate TlRMTVNTUAAC", "Expected the continuation token to be returned to the client.");
    }
}
