using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed class RDPGWStreamChannelMember_Test
{
    [TestMethod]
    public async Task TestFromBytes()
    {
        var packet = new HTTP_DATA_PACKET(new byte[10]);
        var stream = new MemoryStream();

        var channelMember = new RDPGWStreamChannelMember(stream);
        Assert.IsNotNull(channelMember, "Expected channel member to be created successfully.");

        // Test sending data
        await channelMember.SendDataPacket(packet);
        Assert.AreEqual(packet.Data.Length, stream.Length, $"Expected stream length to match packet data.");
        stream.Position = 0; // Reset stream position for reading
        var readPacket = await channelMember.ReadDataPacket();
        Assert.IsNotNull(readPacket, "Expected read packet to be not null.");
        Assert.AreEqual(packet.Data.Length, readPacket.Data.Length, "Expected read packet data length to be 10.");
        Assert.IsTrue(readPacket.Data.Data.SequenceEqual(packet.Data.Data), "Expected read packet data to match sent data.");
    }
}