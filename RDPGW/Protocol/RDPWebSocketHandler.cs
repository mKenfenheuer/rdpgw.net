using System.Net.Sockets;
using System.Net.WebSockets;
using RDPGW.AspNetCore;
using RDPGW.Extensions;

namespace RDPGW.Protocol;

/// <summary>
/// Handles WebSocket connections for the RDP Gateway.
/// </summary>
public class RDPWebSocketHandler : IRRDPGWChannelMember
{
    private readonly WebSocket _socket;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IRDPGWAuthorizationHandler? _authorizationHandler;
    private readonly string? _userId;
    private readonly ILogger<RDPWebSocketHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RDPWebSocketHandler"/> class.
    /// </summary>
    /// <param name="socket">The WebSocket connection.</param>
    /// <param name="userId">The user ID associated with the connection.</param>
    /// <param name="authorizationHandler">The authorization handler for resource access.</param>
    public RDPWebSocketHandler(WebSocket socket, string? userId, IRDPGWAuthorizationHandler? authorizationHandler, ILogger<RDPWebSocketHandler> logger)
    {
        _socket = socket;
        _cancellationTokenSource = new CancellationTokenSource();
        _authorizationHandler = authorizationHandler;
        _userId = userId;
        _logger = logger;
    }

    /// <summary>
    /// Reads a specified number of bytes from the WebSocket connection.
    /// </summary>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>A segment of bytes read from the WebSocket.</returns>
    public async Task<ArraySegment<byte>> ReadBytes(int count)
    {
        List<byte> bytes = new List<byte>();

        // Read data in chunks until the required number of bytes is collected.
        while (bytes.Count < count && _socket.State == WebSocketState.Open)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[count - bytes.Count]);
            var result = await _socket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
            bytes.AddRange(buffer.Take(result.Count));
        }

        return bytes.ToArray();
    }

    /// <summary>
    /// Reads an HTTP packet from the WebSocket connection.
    /// </summary>
    /// <returns>The HTTP packet read from the connection.</returns>
    public async Task<HTTP_PACKET?> ReadPacket()
    {
        // Read the packet header (8 bytes).
        var headerBytes = await ReadBytes(8);
        if(headerBytes.Count < 8)
        {
            return null;
        }
        
        // Parse the header to get the packet length and type.
        var header = new HTTP_PACKET_HEADER(headerBytes);

        // Read the remaining packet data.
        var dataBytes = await ReadBytes((int)header.PacketLength - 8);

        return HTTP_PACKET.FromBytes(headerBytes.Concat(dataBytes).ToArray());
    }

    /// <summary>
    /// Sends an HTTP packet over the WebSocket connection.
    /// </summary>
    /// <param name="packet">The HTTP packet to send.</param>
    public async Task SendPacket(HTTP_PACKET packet)
    {
        await _socket.SendAsync(packet.ToBytes(), WebSocketMessageType.Binary, true, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Handles the WebSocket connection, including handshake, tunnel, and channel setup.
    /// </summary>
    public async Task HandleConnection()
    {
        try
        {
            // Perform the handshake process.
            var packet = await ReadPacket();
            if(packet == null)
            {
                _logger.LogDebug("Unable to read HTTP_HANDSHAKE_REQUEST_PACKET packet during handshake.");
                return;
            }
            var handshakeRequest = (HTTP_HANDSHAKE_REQUEST_PACKET)packet;
            HTTP_HANDSHAKE_RESPONSE_PACKET handshakeResponse = new HTTP_HANDSHAKE_RESPONSE_PACKET
            {
                ServerVersion = handshakeRequest.ClientVersion,
                VersionMajor = 0x1,
                VersionMinor = handshakeRequest.VersionMinor,
                ExtendedAuth = HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_NONE,
                ErrorCode = 0x0
            };
            await SendPacket(handshakeResponse);
            _logger.LogDebug("Handshake completed.");

            // Handle tunnel request and response.
            packet = await ReadPacket();
            if(packet == null)
            {
                _logger.LogDebug("Unable to read HTTP_TUNNEL_PACKET packet during handshake.");
                return;
            }
            var tunnelRequest = (HTTP_TUNNEL_PACKET)packet;
            HTTP_TUNNEL_RESPONSE httpTunnelResponse = new HTTP_TUNNEL_RESPONSE
            {
                ServerVersion = 0x5
            };
            await SendPacket(httpTunnelResponse);
            _logger.LogDebug("Tunnel established.");

            // Handle tunnel authentication.
            packet = await ReadPacket();
            if(packet == null)
            {
                _logger.LogDebug("Unable to read HTTP_TUNNEL_AUTH_PACKET packet during handshake.");
                return;
            }
            var tunnelAuthRequest = (HTTP_TUNNEL_AUTH_PACKET)packet;
            var tunnelAuthResponse = new HTTP_TUNNEL_AUTH_RESPONSE();
            await SendPacket(tunnelAuthResponse);
            _logger.LogDebug("Tunnel authentication completed.");

            // Handle channel request and response.
            packet = await ReadPacket();
            if(packet == null)
            {
                _logger.LogDebug("Unable to read HTTP_CHANNEL_PACKET packet during handshake.");
                return;
            }
            var channelRequest = (HTTP_CHANNEL_PACKET)packet;
            TcpClient? client = null;

            // Attempt to connect to the requested resources.
            foreach (var resource in channelRequest.Resources)
            {
                if (client != null)
                    break;
                if (_authorizationHandler != null && _userId != null && !await _authorizationHandler.HandleUserAuthorization(_userId, resource))
                    break;
                _logger.LogInformation($"Trying resource: {resource} on Port {channelRequest.Port}");
                client = await TryConnectResource(resource, channelRequest.Port);
            }

            if (client == null)
            {
                // Attempt to connect to alternate resources if primary resources fail.
                foreach (var resource in channelRequest.AltResources)
                {
                    if (client != null)
                        break;
                    if (_authorizationHandler != null && _userId != null && !await _authorizationHandler.HandleUserAuthorization(_userId, resource))
                        break;
                    _logger.LogInformation($"Trying alternate resource: {resource} on Port {channelRequest.Port}");
                    client = await TryConnectResource(resource, channelRequest.Port);
                }
            }

            // Send channel response.
            var channelResponse = new HTTP_CHANNEL_PACKET_RESPONSE
            {
                ErrorCode = client == null ? (uint)0x800202 : 0x0,
                ChannelId = 1
            };
            await SendPacket(channelResponse);

            if (client == null)
            {

                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cancellationTokenSource.Token);
                return;
            }

            // Set up channel handlers for data transfer.
            var tcpClientChannelMember = new RDPGWTcpClientChannelMemeber(client);
            var inHandler = new RDPGWChannelHandler(this, tcpClientChannelMember);
            var outHandler = new RDPGWChannelHandler(tcpClientChannelMember, this);

            Console.WriteLine("Handling Channel.");

            // Handle bidirectional channel communication.
            await Task.WhenAny([inHandler.HandleChannel(), outHandler.HandleChannel()]);
        }
        catch (Exception ex)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message, _cancellationTokenSource.Token);
            return;
        }

        try
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Client unexpectedly closing WebSocket connection.");
        }
    }

    /// <summary>
    /// Attempts to connect to a resource using the specified port.
    /// </summary>
    /// <param name="resource">The resource to connect to.</param>
    /// <param name="port">The port to use for the connection.</param>
    /// <returns>A connected <see cref="TcpClient"/> if successful; otherwise, null.</returns>
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
        {
            // Ignore connection errors.
        }

        return null;
    }

    /// <summary>
    /// Reads an HTTP data packet from the WebSocket connection.
    /// </summary>
    /// <returns>The HTTP data packet read from the connection.</returns>
    /// <exception cref="Exception">Thrown if the packet is not an HTTP data packet.</exception>
    public async Task<HTTP_DATA_PACKET?> ReadDataPacket()
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
        _logger.LogDebug("Unable to read HTTP_DATA_PACKET packet during channel handling.");
        return null;
    }

    /// <summary>
    /// Sends an HTTP data packet over the WebSocket connection.
    /// </summary>
    /// <param name="packet">The HTTP data packet to send.</param>
    public async Task SendDataPacket(HTTP_DATA_PACKET packet)
    {
        await SendPacket(packet);
    }
}