using System.Net.Sockets;
using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed class RDPGWTcpClientChannelMemeber_Test
{
    [TestMethod]
    public void TestCreation()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 65200);
        listener.Start();
        using var client = new TcpClient();
        client.Connect(System.Net.IPAddress.Loopback, 65200);
        var connected = listener.AcceptTcpClient();
        Assert.IsTrue(client.Connected, "Expected TCP client to be connected successfully.");
        
        var channelMember = new RDPGWTcpClientChannelMemeber(client);
        Assert.IsNotNull(channelMember, "Expected channel member to be created successfully.");
    }
}