namespace RDPGW.Protocol;

/// <summary>
/// Flags indicating which fields are present in an HTTP tunnel packet.
/// </summary>
internal enum HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS : ushort
{
    /// <summary>
    /// Indicates that the PAA cookie field is present.
    /// </summary>
    HTTP_TUNNEL_PACKET_FIELD_PAA_COOKIE = 0x1,

    /// <summary>
    /// Indicates that the reauthentication context field is present.
    /// </summary>
    HTTP_TUNNEL_PACKET_FIELD_REAUTH = 0x2
}
