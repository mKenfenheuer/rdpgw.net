using System.Net.Sockets;
using System.Net.WebSockets;
using RDPGW.AspNetCore;
using RDPGW.Extensions;

namespace RDPGW.Protocol;

internal class RDPWebSocketHandler : IRRDPGWChannelMember
{
    private readonly WebSocket _socket;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IRDPGWAuthorizationHandler? _authorizationHandler;
    private readonly string? _userId;

    public RDPWebSocketHandler(WebSocket socket, string? userId, IRDPGWAuthorizationHandler? authorizationHandler)
    {
        _socket = socket;
        _cancellationTokenSource = new CancellationTokenSource();
        _authorizationHandler = authorizationHandler;
        _userId = userId;
    }

    internal async Task<ArraySegment<byte>> ReadBytes(int count)
    {
        List<byte> bytes = new List<byte>();

        while (bytes.Count < count)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[count - bytes.Count]);
            var result = await _socket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
            bytes.AddRange(buffer.Take(result.Count));
        }

        return bytes.ToArray();
    }

    internal async Task<HTTP_PACKET> ReadPacket()
    {
        var headerBytes = await ReadBytes(8);
        var header = new HTTP_PACKET_HEADER(headerBytes);

        var dataBytes = await ReadBytes((int)header.PacketLength - 8);

        return HTTP_PACKET.FromBytes(headerBytes.Concat(dataBytes).ToArray());
    }

    internal async Task SendPacket(HTTP_PACKET packet)
    {
        await _socket.SendAsync(packet.ToBytes(), WebSocketMessageType.Binary, true, _cancellationTokenSource.Token);
    }

    internal async Task HandleConnection()
    {
        var handshakeRequest = (HTTP_HANDSHAKE_REQUEST_PACKET)await ReadPacket();
        HTTP_HANDSHAKE_RESPONSE_PACKET handshakeResponse = new HTTP_HANDSHAKE_RESPONSE_PACKET();
        handshakeResponse.ServerVersion = handshakeRequest.ClientVersion;
        handshakeResponse.VersionMajor = 0x1;
        handshakeResponse.VersionMinor = handshakeRequest.VersionMinor;
        handshakeResponse.ExtendedAuth = HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_NONE;
        handshakeResponse.ErrorCode = 0x0;
        await SendPacket(handshakeResponse);

        var tunnelRequest = (HTTP_TUNNEL_PACKET)await ReadPacket();

        HTTP_TUNNEL_RESPONSE httpTunnelResponse = new HTTP_TUNNEL_RESPONSE();
        httpTunnelResponse.ServerVersion = 0x5;
        await SendPacket(httpTunnelResponse);

        var tunnelAuthRequest = (HTTP_TUNNEL_AUTH_PACKET)await ReadPacket();

        var tunnelAuthResponse = new HTTP_TUNNEL_AUTH_RESPONSE();

        await SendPacket(tunnelAuthResponse);

        var channelRequest = (HTTP_CHANNEL_PACKET)await ReadPacket();

        TcpClient? client = null;

        foreach (var resource in channelRequest.Resources)
        {
            if (_authorizationHandler != null && _userId != null && !await _authorizationHandler.HandleUserAuthorization(_userId, resource))
                break;
            client = await TryConnectResource(resource, channelRequest.Port);
            if (client != null)
                break;
        }

        foreach (var resource in channelRequest.AltResources)
        {
            client = await TryConnectResource(resource, channelRequest.Port);
            if (client != null)
                break;
        }


        var channelResponse = new HTTP_CHANNEL_PACKET_RESPONSE();
        channelResponse.ErrorCode = client == null ? (uint)0x800202 : 0x0;
        channelResponse.ChannelId = 1;

        await SendPacket(channelResponse);

        if (client == null)
            return;

        var tcpClientChannelMember = new RDPGWTcpClientChannelMemeber(client);

        var inHandler = new RDPGWChannelHandler(this, tcpClientChannelMember);
        var outHandler = new RDPGWChannelHandler(tcpClientChannelMember, this);

        await Task.WhenAny([inHandler.HandleChannel(), outHandler.HandleChannel()]);
    }

    private async Task<TcpClient?> TryConnectResource(string resource, ushort port)
    {
        try
        {
            TcpClient client = new TcpClient();
            await client.ConnectAsync(resource, port);
            if (client.Connected)
            {
                return client;
            }
        }
        catch
        { }

        return null;
    }

    public async Task<HTTP_DATA_PACKET> ReadDataPacket()
    {
    retry:
        var message = await ReadPacket();
        if (message is HTTP_DATA_PACKET)
        {
            return (HTTP_DATA_PACKET)message;
        }
        if (message is HTTP_KEEPALIVE_PACKET)
        {
            goto retry;
        }
        throw new Exception("Packet Read was not a HTTP_DATA_PACKET.");
    }

    public async Task SendDataPacket(HTTP_DATA_PACKET packet)
    {
        await SendPacket(packet);
    }
}