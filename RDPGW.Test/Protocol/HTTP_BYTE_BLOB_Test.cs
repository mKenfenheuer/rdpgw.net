using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed class HTTP_BYTE_BLOB_Test
{
    [TestMethod]
    public void TestFromBytes()
    {
        byte[] data = new byte[10];
        byte[] length = BitConverter.GetBytes((ushort)data.Length);
        ArraySegment<byte> segment = new ArraySegment<byte>(length.Concat(data).ToArray());
        HTTP_BYTE_BLOB blob = new HTTP_BYTE_BLOB(segment);    
        Assert.AreEqual(data.Length, blob.Length, "Length mismatch");
        Assert.AreEqual(data.Length, blob.Data.Length, "Data length mismatch");
        Assert.IsTrue(data.SequenceEqual(blob.Data), "Data mismatch");
    }

    [TestMethod]
    public void TestToBytes()
    {
        HTTP_BYTE_BLOB blob = new HTTP_BYTE_BLOB();    
        blob.Data = new byte[10];

        var sequence = blob.GetBytes();

        var length = BitConverter.ToUInt16(sequence.Take(2).ToArray());
        var data = sequence.Skip(2).ToArray();
        
        Assert.AreEqual(data.Length, blob.Length, "Length mismatch");
        Assert.AreEqual(data.Length, blob.Data.Length, "Data length mismatch");
        Assert.IsTrue(data.SequenceEqual(blob.Data), "Data mismatch");
        Assert.AreEqual(length, blob.Length, "Length mismatch");
    }
}
