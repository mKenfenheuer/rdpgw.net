using System.Net.Sockets;
using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed class RDPGWChannelHandler_Test
{
    [TestMethod]
    public async Task TestCreation()
    {
        var stream1 = new MemoryStream();
        var stream2 = new MemoryStream();
        var channelMember1 = new RDPGWStreamChannelMemeber(stream1);
        var channelMember2 = new RDPGWStreamChannelMemeber(stream2);

        var packet = new HTTP_DATA_PACKET(new byte[10]);
        Assert.AreEqual(packet.Data.Length, 10, $"Expected packet data of 10 bytes.");

        await channelMember1.SendDataPacket(packet);
        Assert.AreEqual(packet.Data.Length, stream1.Length, $"Expected stream length to match packet data.");
        stream1.Position = 0; // Reset stream position for reading

        var channelHandler = new RDPGWChannelHandler(channelMember1, channelMember2);
        Assert.IsNotNull(channelHandler, "Expected channel handler to be created successfully.");

        await channelHandler.HandleChannel();
                
        var data1 = stream1.ToArray();
        Assert.IsNotNull(data1, "Expected data1 to be not null.");
        var data2 = stream2.ToArray();
        Assert.IsNotNull(data2, "Expected data2 to be not null.");

        Assert.AreEqual(data1.Length, data2.Length, "Expected data lengths to match.");
        Assert.IsTrue(data1.SequenceEqual(data2), "Expected data to match.");

        Assert.AreEqual(data1.Length, packet.Data.Data.Length, "Expected data length to match packet.");
        Assert.AreEqual(data2.Length, packet.Data.Data.Length, "Expected data length to match packet.");
        Assert.IsTrue(data1.SequenceEqual(packet.Data.Data), "Expected data to match packet.");
        Assert.IsTrue(data2.SequenceEqual(packet.Data.Data), "Expected data to match packet.");
    }
}