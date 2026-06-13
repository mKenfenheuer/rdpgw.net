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
    private readonly IRDPGWAuthenticationHandler? _authenticationHandler;
    private string? _userId;
    private readonly ILogger<RDPWebSocketHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RDPWebSocketHandler"/> class.
    /// </summary>
    /// <param name="socket">The WebSocket connection.</param>
    /// <param name="userId">The user ID associated with the connection.</param>
    /// <param name="authorizationHandler">The authorization handler for resource access.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="authenticationHandler">The authentication handler, used for extended (PAA) auth.</param>
    public RDPWebSocketHandler(WebSocket socket, string? userId, IRDPGWAuthorizationHandler? authorizationHandler, ILogger<RDPWebSocketHandler> logger, IRDPGWAuthenticationHandler? authenticationHandler = null)
    {
        _socket = socket;
        _cancellationTokenSource = new CancellationTokenSource();
        _authorizationHandler = authorizationHandler;
        _authenticationHandler = authenticationHandler;
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

            // A Close frame or a zero-length read on a non-open socket means the peer has gone
            // away. Without this guard the loop would spin forever consuming CPU.
            if (result.MessageType == WebSocketMessageType.Close
                || (result.Count == 0 && _socket.State != WebSocketState.Open))
            {
                throw new IOException("WebSocket connection closed while reading packet.");
            }

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
            // Perform the handshake process. Negotiate an extended-auth method that both the client
            // offered and that we can service. We only accept PAA (cookie/token pre-auth) when an
            // authentication handler is present to validate the cookie; otherwise fall back to NONE.
            var packet = await ReadPacket();
            if (packet == null)
            {
                _logger.LogDebug("Unable to read HTTP_HANDSHAKE_REQUEST_PACKET packet during handshake.");
                return;
            }
            var handshakeRequest = (HTTP_HANDSHAKE_REQUEST_PACKET)packet;

            var negotiatedAuth = HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_NONE;
            if (_authenticationHandler != null
                && (handshakeRequest.ExtendedAuth & HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_PAA) != 0)
            {
                negotiatedAuth = HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_PAA;
            }

            HTTP_HANDSHAKE_RESPONSE_PACKET handshakeResponse = new HTTP_HANDSHAKE_RESPONSE_PACKET
            {
                ServerVersion = handshakeRequest.ClientVersion,
                VersionMajor = 0x1,
                VersionMinor = handshakeRequest.VersionMinor,
                ExtendedAuth = negotiatedAuth,
                ErrorCode = HTTP_ERROR_CODE.S_OK
            };
            await SendPacket(handshakeResponse);
            _logger.LogDebug("Handshake completed.");

            // Handle tunnel request and response. Only advertise capabilities the client also
            // offered, so we never claim support for something the client did not request.
            packet = await ReadPacket();
            if (packet == null)
            {
                _logger.LogDebug("Unable to read HTTP_TUNNEL_PACKET packet during handshake.");
                return;
            }
            var tunnelRequest = (HTTP_TUNNEL_PACKET)packet;

            // If extended PAA authentication was negotiated, validate the cookie carried in the
            // tunnel-create request before granting the tunnel.
            var tunnelStatus = HTTP_ERROR_CODE.S_OK;
            if (negotiatedAuth == HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_PAA && _authenticationHandler != null)
            {
                var cookie = tunnelRequest.PAACookie?.Data ?? Array.Empty<byte>();
                var paaResult = await _authenticationHandler.HandlePAACookieAuth(cookie);
                if (!paaResult.IsAuthenticated)
                {
                    tunnelStatus = HTTP_ERROR_CODE.E_PROXY_NAP_ACCESSDENIED;
                }
                else if (paaResult.UserId != null)
                {
                    // The PAA cookie identifies the user when no HTTP-level auth supplied one.
                    _userId ??= paaResult.UserId;
                }
            }

            HTTP_TUNNEL_RESPONSE httpTunnelResponse = new HTTP_TUNNEL_RESPONSE
            {
                ServerVersion = 0x5,
                StatusCode = tunnelStatus,
                // Echo back the intersection of the client's capabilities with what we support.
                CapabilityFlags = tunnelRequest.CapabilityFlags & ServerCapabilities,
                TunnelId = 1
            };
            await SendPacket(httpTunnelResponse);
            _logger.LogDebug("Tunnel established.");

            // Abort the connection if PAA validation failed.
            if (tunnelStatus != HTTP_ERROR_CODE.S_OK)
            {
                _logger.LogDebug("PAA authentication failed; aborting connection.");
                return;
            }

            // Handle tunnel authentication.
            packet = await ReadPacket();
            if (packet == null)
            {
                _logger.LogDebug("Unable to read HTTP_TUNNEL_AUTH_PACKET packet during handshake.");
                return;
            }
            var tunnelAuthRequest = (HTTP_TUNNEL_AUTH_PACKET)packet;
            var tunnelAuthResponse = new HTTP_TUNNEL_AUTH_RESPONSE
            {
                ErrorCode = HTTP_ERROR_CODE.S_OK,
                // Permit all redirections by default; consumers can tighten this later.
                RedirectionFlags = HTTP_TUNNEL_REDIR_FLAGS.HTTP_TUNNEL_REDIR_ENABLE_ALL
            };
            await SendPacket(tunnelAuthResponse);
            _logger.LogDebug("Tunnel authentication completed.");

            // Handle channel request and response.
            packet = await ReadPacket();
            if (packet == null)
            {
                _logger.LogDebug("Unable to read HTTP_CHANNEL_PACKET packet during handshake.");
                return;
            }
            var channelRequest = (HTTP_CHANNEL_PACKET)packet;
            TcpClient? client = null;
            uint errorCode = HTTP_ERROR_CODE.E_PROXY_TS_CONNECTFAILED;

            // Authorize and attempt to connect to each requested resource (primary then alternate).
            // Authorization is enforced for every candidate, including alternate resources.
            foreach (var resource in channelRequest.Resources.Concat(channelRequest.AltResources))
            {
                // Deny resources the user is not authorized to reach.
                if (_authorizationHandler != null && _userId != null
                    && !await _authorizationHandler.HandleUserAuthorization(_userId, resource))
                {
                    // Record access-denied, but keep checking other resources the user may reach.
                    errorCode = HTTP_ERROR_CODE.E_PROXY_RAP_ACCESSDENIED;
                    continue;
                }

                _logger.LogInformation($"Trying resource: {resource} on Port {channelRequest.Port}");
                client = await TryConnectResource(resource, channelRequest.Port);
                if (client != null)
                {
                    errorCode = HTTP_ERROR_CODE.S_OK;
                    break;
                }

                // We were allowed to reach this resource but could not connect to it.
                errorCode = HTTP_ERROR_CODE.E_PROXY_TS_CONNECTFAILED;
            }

            // Send channel response.
            var channelResponse = new HTTP_CHANNEL_PACKET_RESPONSE
            {
                ErrorCode = errorCode,
                ChannelId = client == null ? null : (uint)1
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

            _logger.LogDebug("Handling Channel.");

            // Handle bidirectional channel communication. When either direction ends (connection
            // closed or error) tear everything down so we don't leak the TCP/WebSocket connections.
            try
            {
                await Task.WhenAny(inHandler.HandleChannel(), outHandler.HandleChannel());
            }
            finally
            {
                _cancellationTokenSource.Cancel();
                client.Close();
            }
        }
        catch (Exception ex)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message, _cancellationTokenSource.Token);
            return;
        }

        try
        {
            // Use a fresh token here: the channel-cleanup above cancels _cancellationTokenSource,
            // and the close handshake must still be allowed to complete.
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Client unexpectedly closing WebSocket connection.");
        }
    }

    /// <summary>
    /// The set of RDG capabilities this gateway implementation supports. Used to compute the
    /// capability set negotiated with the client in the tunnel response.
    /// </summary>
    private const HTTP_CAPABILITY_TYPE ServerCapabilities =
        HTTP_CAPABILITY_TYPE.HTTP_CAPABILITY_IDLE_TIMEOUT
        | HTTP_CAPABILITY_TYPE.HTTP_CAPABILITY_MESSAGING_SERVICE_MSG
        | HTTP_CAPABILITY_TYPE.HTTP_CAPABILITY_REAUTH;

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