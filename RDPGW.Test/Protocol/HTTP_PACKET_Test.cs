using Newtonsoft.Json;
using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed partial class HTTP_PACKET_Test
{

    [TestMethod]
    public void TestPacketSerilalizationAndDeserialization()
    {
        var packets = JsonConvert.DeserializeObject<TestPacket[]>(File.ReadAllText("packets.json"));
        Assert.IsNotNull(packets, "Failed to deserialize packets.json");
        foreach (var packet in packets)
        {
            var parsedPacket = HTTP_PACKET.FromBytes(packet.Data);
            var typeName = parsedPacket.GetType().Name;
            Assert.AreEqual(packet.TypeName, typeName, $"Type mismatch for packet: {packet.TypeName} != {typeName}");
            var data = parsedPacket.ToBytes();
            Assert.IsTrue(data.Count == packet.Data.Length, $"Length mismatch for packet: {packet.TypeName}");
            Assert.IsTrue(data.SequenceEqual(packet.Data), $"Data mismatch for packet: {packet.TypeName}");
        }
    }

    [TestMethod]
    public void TestInvalidPackets()
    {
        var packets = JsonConvert.DeserializeObject<TestPacket[]>(File.ReadAllText("packets.json"));
        Assert.IsNotNull(packets, "Failed to deserialize packets.json");
        foreach (var packet in packets)
            if (packet.Data.Length > 8)
            {
                try
                {
                    var parsedPacket = HTTP_PACKET.FromBytes(packet.Data.Take(9).ToArray());
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(ex is ArgumentException, $"Expected ArgumentException for invalid packet: {packet.TypeName}");
                    continue;
                }
                Assert.Fail($"Invalid packet parsing should throw! Packet: {packet.TypeName}");
            }
    }

    [TestMethod]
    public void TestKeepalivePacket()
    {
        var keepalivePacket = new HTTP_KEEPALIVE_PACKET();
        var data = keepalivePacket.ToBytes();
        Assert.IsTrue(data.Count == 8, "Keepalive packet should have 8 bytes of data.");
        var parsedPacket = HTTP_PACKET.FromBytes(data);
        Assert.IsInstanceOfType(parsedPacket, typeof(HTTP_KEEPALIVE_PACKET), "Parsed packet should be of type HTTP_KEEPALIVE_PACKET.");
    }

    [TestMethod]
    public void TestInvalidPacket()
    {
        var data = new byte[8];
        try
        {
            var parsedPacket = HTTP_PACKET.FromBytes(data);
        }
        catch (Exception ex)
        {
            Assert.IsTrue(ex is ArgumentException, "Expected ArgumentException for invalid packet.");
            return;
        }
        Assert.Fail("Invalid packet parsing should throw!");
    }
}
