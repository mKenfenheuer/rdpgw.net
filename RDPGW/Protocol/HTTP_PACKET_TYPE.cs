namespace RDPGW.Protocol;

/// <summary>
/// Represents the types of HTTP packets used in the protocol.
/// </summary>
internal enum HTTP_PACKET_TYPE : ushort
{
    /// <summary>Handshake request packet.</summary>
    PKT_TYPE_HANDSHAKE_REQUEST = 0x1,
    /// <summary>Handshake response packet.</summary>
    PKT_TYPE_HANDSHAKE_RESPONSE = 0x2,
    /// <summary>Extended authentication message packet.</summary>
    PKT_TYPE_EXTENDED_AUTH_MSG = 0x3,
    /// <summary>Tunnel creation request packet.</summary>
    PKT_TYPE_TUNNEL_CREATE = 0x4,
    /// <summary>Tunnel creation response packet.</summary>
    PKT_TYPE_TUNNEL_RESPONSE = 0x5,
    /// <summary>Tunnel authentication request packet.</summary>
    PKT_TYPE_TUNNEL_AUTH = 0x6,
    /// <summary>Tunnel authentication response packet.</summary>
    PKT_TYPE_TUNNEL_AUTH_RESPONSE = 0x7,
    /// <summary>Channel creation request packet.</summary>
    PKT_TYPE_CHANNEL_CREATE = 0x8,
    /// <summary>Channel creation response packet.</summary>
    PKT_TYPE_CHANNEL_RESPONSE = 0x9,
    /// <summary>Data packet.</summary>
    PKT_TYPE_DATA = 0xA,
    /// <summary>Service message packet.</summary>
    PKT_TYPE_SERVICE_MESSAGE = 0xB,
    /// <summary>Reauthentication message packet.</summary>
    PKT_TYPE_REAUTH_MESSAGE = 0xC,
    /// <summary>Keep-alive packet.</summary>
    PKT_TYPE_KEEPALIVE = 0xD,
    /// <summary>Close channel request packet.</summary>
    PKT_TYPE_CLOSE_CHANNEL = 0x10,
    /// <summary>Close channel response packet.</summary>
    PKT_TYPE_CLOSE_CHANNEL_RESPONSE = 0x11
}
