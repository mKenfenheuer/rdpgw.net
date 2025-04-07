using System.Net.Sockets;

namespace RDPGW;

internal class RDPGWTcpClientChannelMemeber : RDPGWStreamChannelMemeber
{
    private readonly TcpClient _client;

    public RDPGWTcpClientChannelMemeber(TcpClient client) : base(client.GetStream())
    {
        _client = client;
    }
}
