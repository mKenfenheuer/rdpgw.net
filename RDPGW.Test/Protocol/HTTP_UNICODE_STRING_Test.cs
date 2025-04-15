using System.Text;
using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed class HTTP_UNICODE_STRING_Test
{
    [TestMethod]
    public void TestFromBytes()
    {
        string data = "This is a sample string. It is used for testing.";
        byte[] dataBytes = Encoding.Unicode.GetBytes(data);
        byte[] length = BitConverter.GetBytes((ushort)dataBytes.Length);

        ArraySegment<byte> segment = new ArraySegment<byte>(length.Concat(dataBytes).ToArray());
        HTTP_UNICODE_STRING str = new HTTP_UNICODE_STRING(segment);    
        
        Assert.AreEqual(data.Length, str.String.Length, "Length mismatch");
        Assert.AreEqual(data, str.String, "Data mismatch");
        Assert.AreEqual(dataBytes.Length, str.Length, "Data length mismatch");
    }

    [TestMethod]
    public void TestToBytes()
    {
        string data = "This is a sample string. It is used for testing.";
        HTTP_UNICODE_STRING str = new HTTP_UNICODE_STRING(data);    
        var sequence = str.GetBytes();

        var length = BitConverter.ToUInt16(sequence.Take(2).ToArray());
        var dataBytes = sequence.Skip(2).ToArray();
        var dataString = Encoding.Unicode.GetString(dataBytes);
        
        Assert.AreEqual(data.TrimEnd('\0').Length, dataString.TrimEnd('\0').Length, "Length mismatch");
        Assert.AreEqual(data.TrimEnd('\0').Length, str.String.TrimEnd('\0').Length, "Length mismatch");
        Assert.AreEqual(data.TrimEnd('\0'), str.String.TrimEnd('\0'), "Data mismatch");
        Assert.AreEqual(dataString.TrimEnd('\0'), data.TrimEnd('\0'), "Data mismatch");
        Assert.AreEqual(dataBytes.Length, str.Length, "Data length mismatch");
        Assert.AreEqual(length, str.Length, "Data length mismatch");
    }
}
