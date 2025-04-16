using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed class RDPProtocolException_Test
{
    [TestMethod]
    public void TestCreation()
    {
        var exception = new RDPProtocolException("Test");
        Assert.IsNotNull(exception, "Exception should not be null");
        Assert.AreEqual("Test", exception.Message, "Message mismatch");

        exception = new RDPProtocolException("Test", new Exception("Inner exception"));
        Assert.IsNotNull(exception, "Exception should not be null");
        Assert.AreEqual("Test", exception.Message, "Message mismatch");
        Assert.IsNotNull(exception.InnerException, "Inner exception should not be null");
        Assert.AreEqual("Inner exception", exception.InnerException?.Message, "Inner exception message mismatch");

        exception = new RDPProtocolException();
        Assert.IsNotNull(exception, "Exception should not be null");
        Assert.AreEqual("Exception of type 'RDPProtocolException' was thrown.", exception.Message, $"Message mismatch. Should be \"Exception of type 'RDPProtocolException' was thrown.\" string was \"{exception.Message}\"");
        Assert.IsNull(exception.InnerException, "Inner exception should be null");
    }
}
