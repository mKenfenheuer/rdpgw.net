using System.Net.Sockets;

namespace RDPGW;

/// <summary>
/// Represents a channel member that communicates over a TCP client.
/// </summary>
public class RDPGWTcpClientChannelMemeber : RDPGWStreamChannelMemeber
{
    private readonly TcpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="RDPGWTcpClientChannelMemeber"/> class.
    /// </summary>
    /// <param name="client">The TCP client used for communication.</param>
    public RDPGWTcpClientChannelMemeber(TcpClient client) : base(client.GetStream())
    {
        _client = client;
    }
}
