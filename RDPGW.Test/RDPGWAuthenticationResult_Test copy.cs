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
}