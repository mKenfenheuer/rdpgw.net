using Newtonsoft.Json;
using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed class HTTP_PACKET_Test
{
    private class TestPackets
    {
        [JsonProperty("packet")]
        public string TypeName { get; set; } = string.Empty;
        [JsonProperty("data")]
        public string DataHex { get; set; } = string.Empty;

        [JsonIgnore]
        public byte[] Data => Convert.FromHexString(DataHex.Replace("0x", ""));
    }

    [TestMethod]
    public void TestPacketSerilalizationAndDeserialization()
    {
        var packets = JsonConvert.DeserializeObject<TestPackets[]>(File.ReadAllText("packets.json"));
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
}
