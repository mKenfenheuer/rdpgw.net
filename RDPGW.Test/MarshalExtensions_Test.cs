using RDPGW.AspNetCore;
using RDPGW.Extensions;

namespace RDPGW.Test;

[TestClass]
public sealed class MarshalExtensions_Test
{
    [TestMethod]
    public void TestFromBytes()
    {
        var bytes = MarshalExtensions.StructToArraySegment<TestContext?>(null);
        Assert.IsTrue(bytes.Count == 0, "Expected bytes to be empty.");
    }
}