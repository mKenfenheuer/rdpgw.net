using RDPGW.AspNetCore;

namespace RDPGW.Test;

[TestClass]
public sealed class RDPGWAuthenticationResult_Test
{
    [TestMethod]
    public void TestFromBytes()
    {
        var result = RDPGWAuthenticationResult.Failed();
        Assert.IsFalse(result.IsAuthenticated, "Expected authentication to fail.");
        Assert.IsNull(result.UserId, "Expected user ID to be null.");
        var successResult = RDPGWAuthenticationResult.Success("testUser");
        Assert.IsTrue(successResult.IsAuthenticated, "Expected authentication to succeed.");
        Assert.IsNotNull(successResult.UserId, "Expected user ID to be not null.");
        Assert.AreEqual("testUser", successResult.UserId, "Expected user ID to match.");
    }

    [TestMethod]
    public void TestChallenge()
    {
        var result = RDPGWAuthenticationResult.Challenge("TlRMTVNTUAAC");
        Assert.IsFalse(result.IsAuthenticated, "A challenge is not yet authenticated.");
        Assert.IsNull(result.UserId, "A challenge has no user ID yet.");
        Assert.AreEqual("TlRMTVNTUAAC", result.ContinuationToken, "Expected the continuation token to be preserved.");
    }

    /// <summary>
    /// Handler that implements only the required members, relying on the default PAA implementation.
    /// </summary>
    private class MinimalAuthHandler : IRDPGWAuthenticationHandler
    {
        public Task<RDPGWAuthenticationResult> HandleBasicAuth(string auth) => Task.FromResult(RDPGWAuthenticationResult.Failed());
        public Task<RDPGWAuthenticationResult> HandleDigestAuth(string auth) => Task.FromResult(RDPGWAuthenticationResult.Failed());
        public Task<RDPGWAuthenticationResult> HandleNegotiateAuth(string auth) => Task.FromResult(RDPGWAuthenticationResult.Failed());
    }

    [TestMethod]
    public async Task TestDefaultPAACookieRejected()
    {
        // The default PAA implementation must reject cookies so handlers that don't opt in are secure.
        IRDPGWAuthenticationHandler handler = new MinimalAuthHandler();
        var result = await handler.HandlePAACookieAuth(new byte[] { 1, 2, 3 });
        Assert.IsFalse(result.IsAuthenticated, "Default PAA cookie handling must reject the cookie.");
    }
}