using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging.Abstractions;
using RDPGW.AspNetCore;
using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public sealed class RDPWebSocketHandler_Test
{
    /// <summary>
    /// An in-memory WebSocket that serves a queued sequence of inbound binary frames (the bytes a
    /// client would send) and records every outbound frame the handler writes. When the inbound
    /// queue is exhausted it reports a Close frame, which unblocks the handler's read loop.
    /// </summary>
    private sealed class FakeWebSocket : WebSocket
    {
        private readonly byte[] _inbound;
        private int _position;
        public List<byte[]> Sent { get; } = new();
        private WebSocketState _state = WebSocketState.Open;

        public FakeWebSocket(IEnumerable<byte[]> inbound)
            => _inbound = inbound.SelectMany(b => b).ToArray();

        public override WebSocketState State => _state;
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override string? SubProtocol => null;
        public override void Abort() => _state = WebSocketState.Aborted;
        public override void Dispose() { }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            // Model the WebSocket as a byte stream so partial reads (e.g. the handler reading the
            // 8-byte header, then the body) are served correctly. When drained, report a Close
            // frame to unblock the handler's read loop.
            var remaining = _inbound.Length - _position;
            if (remaining <= 0)
            {
                _state = WebSocketState.CloseReceived;
                return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
            }

            var count = Math.Min(buffer.Count, remaining);
            Array.Copy(_inbound, _position, buffer.Array!, buffer.Offset, count);
            _position += count;
            return Task.FromResult(new WebSocketReceiveResult(count, WebSocketMessageType.Binary, true));
        }

        /// <summary>When set, the Nth (0-based) call to <see cref="SendAsync"/> throws, modelling a mid-handshake transport failure.</summary>
        public int? ThrowOnSendIndex { get; init; }
        private int _sendCount;

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (ThrowOnSendIndex == _sendCount++)
                throw new IOException("Simulated send failure.");
            Sent.Add(buffer.ToArray());
            return Task.CompletedTask;
        }
    }

    private sealed class AllowAllAuthorization : IRDPGWAuthorizationHandler
    {
        public Task<bool> HandleUserAuthorization(string userId, string resource) => Task.FromResult(true);
    }

    private sealed class DenyAllAuthorization : IRDPGWAuthorizationHandler
    {
        public Task<bool> HandleUserAuthorization(string userId, string resource) => Task.FromResult(false);
    }

    /// <summary>
    /// Authentication handler whose PAA-cookie validation result is fixed by the constructor,
    /// recording the cookie bytes it was handed so tests can assert what the handler forwarded.
    /// </summary>
    private sealed class StubAuthentication : IRDPGWAuthenticationHandler
    {
        private readonly RDPGWAuthenticationResult _paaResult;
        public byte[]? ReceivedCookie { get; private set; }

        public StubAuthentication(RDPGWAuthenticationResult paaResult) => _paaResult = paaResult;

        public Task<RDPGWAuthenticationResult> HandleBasicAuth(string auth) => Task.FromResult(RDPGWAuthenticationResult.Failed());
        public Task<RDPGWAuthenticationResult> HandleDigestAuth(string auth) => Task.FromResult(RDPGWAuthenticationResult.Failed());
        public Task<RDPGWAuthenticationResult> HandleNegotiateAuth(string auth) => Task.FromResult(RDPGWAuthenticationResult.Failed());

        public Task<RDPGWAuthenticationResult> HandlePAACookieAuth(byte[] paaCookie)
        {
            ReceivedCookie = paaCookie;
            return Task.FromResult(_paaResult);
        }
    }

    private static byte[] Hex(string h) => Convert.FromHexString(h);

    // Handshake -> tunnel -> tunnel-auth -> channel-create request frames, taken from packets.json.
    private static readonly byte[] HandshakeReq = Hex("010000000E000000010000000000");
    private static readonly byte[] TunnelReq = Hex("040000001E0000000E000000030000000102030405060708040001020304");
    private static readonly byte[] TunnelAuthReq = Hex("06000000380000000100260061006E0079006400650076006900630065002E0061006E007900770068006500720065000000040001020304");

    /// <summary>
    /// Builds a handshake request advertising the given extended-auth method.
    /// </summary>
    private static byte[] HandshakeReqWithAuth(HTTP_EXTENDED_AUTH auth)
    {
        var pkt = new HTTP_HANDSHAKE_REQUEST_PACKET(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_HANDSHAKE_REQUEST), new byte[6])
        {
            VersionMajor = 1,
            VersionMinor = 0,
            ClientVersion = 0,
            ExtendedAuth = auth
        };
        return pkt.ToBytes().ToArray();
    }

    /// <summary>
    /// Builds a tunnel-create request carrying the given PAA cookie bytes.
    /// </summary>
    private static byte[] TunnelReqWithPAACookie(byte[] cookie)
    {
        var pkt = new HTTP_TUNNEL_PACKET(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_TUNNEL_CREATE), new byte[8])
        {
            CapabilityFlags = 0,
            PAACookie = new HTTP_BYTE_BLOB { Data = cookie }
        };
        return pkt.ToBytes().ToArray();
    }

    /// <summary>
    /// Builds a channel-create request packet that targets the given host/port.
    /// </summary>
    private static byte[] ChannelRequest(string host, ushort port)
    {
        var pkt = new HTTP_CHANNEL_PACKET(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_CHANNEL_CREATE), new byte[6])
        {
            Port = port,
            Protocol = 3,
            Resources = new[] { host },
            AltResources = Array.Empty<string>()
        };
        return pkt.ToBytes().ToArray();
    }

    /// <summary>
    /// Extracts the typed packets the handler sent back to the client.
    /// </summary>
    private static List<HTTP_PACKET> ParseSent(FakeWebSocket socket)
        => socket.Sent.Select(b => HTTP_PACKET.FromBytes(b)).ToList();

    [TestMethod]
    public async Task ConnectsToResourceAndReportsSuccess()
    {
        // Start a real loopback TCP server to act as the target resource.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        var acceptTask = listener.AcceptTcpClientAsync();

        var socket = new FakeWebSocket(new[]
        {
            HandshakeReq,
            TunnelReq,
            TunnelAuthReq,
            ChannelRequest("127.0.0.1", port),
            // No data frames: the inbound queue then yields Close, ending the data loop.
        });

        var handler = new RDPWebSocketHandler(socket, "user1", new AllowAllAuthorization(), NullLogger<RDPWebSocketHandler>.Instance);
        await handler.HandleConnection();

        listener.Stop();

        var sent = ParseSent(socket);
        Assert.IsInstanceOfType(sent[0], typeof(HTTP_HANDSHAKE_RESPONSE_PACKET));
        Assert.IsInstanceOfType(sent[1], typeof(HTTP_TUNNEL_RESPONSE));
        Assert.IsInstanceOfType(sent[2], typeof(HTTP_TUNNEL_AUTH_RESPONSE));

        var channelResponse = sent.OfType<HTTP_CHANNEL_PACKET_RESPONSE>().Single();
        Assert.AreEqual(HTTP_ERROR_CODE.S_OK, channelResponse.ErrorCode, "Expected a successful channel response.");
        Assert.AreEqual((uint)1, channelResponse.ChannelId);
    }

    [TestMethod]
    public async Task UnauthorizedResourceReturnsAccessDenied()
    {
        var socket = new FakeWebSocket(new[]
        {
            HandshakeReq,
            TunnelReq,
            TunnelAuthReq,
            ChannelRequest("denied.example", 3389),
        });

        var handler = new RDPWebSocketHandler(socket, "user1", new DenyAllAuthorization(), NullLogger<RDPWebSocketHandler>.Instance);
        await handler.HandleConnection();

        var channelResponse = ParseSent(socket).OfType<HTTP_CHANNEL_PACKET_RESPONSE>().Single();
        Assert.AreEqual(HTTP_ERROR_CODE.E_PROXY_RAP_ACCESSDENIED, channelResponse.ErrorCode,
            "An unauthorized resource must yield E_PROXY_RAP_ACCESSDENIED.");
        Assert.IsNull(channelResponse.ChannelId, "No channel should be opened for a denied resource.");
    }

    [TestMethod]
    public async Task UnreachableResourceReturnsConnectFailed()
    {
        // Port 1 on loopback is essentially never listening, so the connect attempt fails fast.
        var socket = new FakeWebSocket(new[]
        {
            HandshakeReq,
            TunnelReq,
            TunnelAuthReq,
            ChannelRequest("127.0.0.1", 1),
        });

        var handler = new RDPWebSocketHandler(socket, "user1", new AllowAllAuthorization(), NullLogger<RDPWebSocketHandler>.Instance);
        await handler.HandleConnection();

        var channelResponse = ParseSent(socket).OfType<HTTP_CHANNEL_PACKET_RESPONSE>().Single();
        Assert.AreEqual(HTTP_ERROR_CODE.E_PROXY_TS_CONNECTFAILED, channelResponse.ErrorCode,
            "A reachable-but-unconnectable resource must yield E_PROXY_TS_CONNECTFAILED.");
        Assert.IsNull(channelResponse.ChannelId);
    }

    [TestMethod]
    public async Task PaaRequestedWithoutAuthHandlerNegotiatesNone()
    {
        // The client asks for PAA, but no authentication handler is configured, so the server must
        // fall back to NONE rather than claiming PAA support it cannot service.
        var socket = new FakeWebSocket(new[]
        {
            HandshakeReqWithAuth(HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_PAA),
            TunnelReq,
            TunnelAuthReq,
            ChannelRequest("127.0.0.1", 1),
        });

        var handler = new RDPWebSocketHandler(socket, "user1", new AllowAllAuthorization(), NullLogger<RDPWebSocketHandler>.Instance);
        await handler.HandleConnection();

        var handshakeResponse = ParseSent(socket).OfType<HTTP_HANDSHAKE_RESPONSE_PACKET>().Single();
        Assert.AreEqual(HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_NONE, handshakeResponse.ExtendedAuth,
            "Without an authentication handler, PAA must not be negotiated.");
    }

    [TestMethod]
    public async Task PaaCookieAuthSuccessNegotiatesPaaAndIdentifiesUser()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        var acceptTask = listener.AcceptTcpClientAsync();

        var cookie = new byte[] { 0xAA, 0xBB, 0xCC };
        var auth = new StubAuthentication(RDPGWAuthenticationResult.Success("paa-user"));

        var socket = new FakeWebSocket(new[]
        {
            HandshakeReqWithAuth(HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_PAA),
            TunnelReqWithPAACookie(cookie),
            TunnelAuthReq,
            // No userId is supplied to the handler; the PAA cookie must establish it so the
            // AllowAll authorization handler (which requires a non-null user) is consulted.
            ChannelRequest("127.0.0.1", port),
        });

        var handler = new RDPWebSocketHandler(socket, null, new AllowAllAuthorization(), NullLogger<RDPWebSocketHandler>.Instance, auth);
        await handler.HandleConnection();

        listener.Stop();

        CollectionAssert.AreEqual(cookie, auth.ReceivedCookie, "The handler must forward the PAA cookie bytes for validation.");

        var sent = ParseSent(socket);
        var handshakeResponse = sent.OfType<HTTP_HANDSHAKE_RESPONSE_PACKET>().Single();
        Assert.AreEqual(HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_PAA, handshakeResponse.ExtendedAuth,
            "PAA must be negotiated when the client offers it and an auth handler is present.");

        var tunnelResponse = sent.OfType<HTTP_TUNNEL_RESPONSE>().Single();
        Assert.AreEqual(HTTP_ERROR_CODE.S_OK, tunnelResponse.StatusCode, "A valid PAA cookie must yield S_OK.");

        var channelResponse = sent.OfType<HTTP_CHANNEL_PACKET_RESPONSE>().Single();
        Assert.AreEqual(HTTP_ERROR_CODE.S_OK, channelResponse.ErrorCode,
            "An authorized, reachable resource must succeed once the PAA cookie identifies the user.");
    }

    [TestMethod]
    public async Task PaaCookieAuthFailureAbortsWithNapAccessDenied()
    {
        var auth = new StubAuthentication(RDPGWAuthenticationResult.Failed());

        var socket = new FakeWebSocket(new[]
        {
            HandshakeReqWithAuth(HTTP_EXTENDED_AUTH.HTTP_EXTENDED_AUTH_PAA),
            TunnelReqWithPAACookie(new byte[] { 0x01 }),
            // The connection must abort right after the tunnel response; these frames are never read.
            TunnelAuthReq,
            ChannelRequest("127.0.0.1", 1),
        });

        var handler = new RDPWebSocketHandler(socket, null, new AllowAllAuthorization(), NullLogger<RDPWebSocketHandler>.Instance, auth);
        await handler.HandleConnection();

        var sent = ParseSent(socket);
        var tunnelResponse = sent.OfType<HTTP_TUNNEL_RESPONSE>().Single();
        Assert.AreEqual(HTTP_ERROR_CODE.E_PROXY_NAP_ACCESSDENIED, tunnelResponse.StatusCode,
            "A rejected PAA cookie must yield E_PROXY_NAP_ACCESSDENIED.");

        Assert.IsFalse(sent.OfType<HTTP_TUNNEL_AUTH_RESPONSE>().Any(),
            "The connection must abort after a failed PAA validation, before tunnel authentication.");
        Assert.IsFalse(sent.OfType<HTTP_CHANNEL_PACKET_RESPONSE>().Any(),
            "No channel must be opened after a failed PAA validation.");
    }

    [TestMethod]
    public async Task TransportFailureDuringHandshakeClosesGracefully()
    {
        // A send failure mid-handshake must be caught and the socket closed, not propagated.
        var socket = new FakeWebSocket(new[] { HandshakeReq })
        {
            ThrowOnSendIndex = 0,
        };

        var handler = new RDPWebSocketHandler(socket, "user1", new AllowAllAuthorization(), NullLogger<RDPWebSocketHandler>.Instance);
        await handler.HandleConnection();

        Assert.AreEqual(WebSocketState.Closed, socket.State,
            "A transport failure during the handshake must leave the socket closed.");
    }

    [TestMethod]
    public async Task ConnectionClosedMidHandshakeIsHandled()
    {
        // The client disconnects after the handshake response, before sending the tunnel request.
        // The read of the next packet fails; the handler must catch it and close cleanly.
        var socket = new FakeWebSocket(new[] { HandshakeReq });

        var handler = new RDPWebSocketHandler(socket, "user1", new AllowAllAuthorization(), NullLogger<RDPWebSocketHandler>.Instance);
        await handler.HandleConnection();

        Assert.IsTrue(socket.State == WebSocketState.Closed || socket.State == WebSocketState.CloseReceived,
            "An early disconnect must be handled without throwing.");
    }
}
