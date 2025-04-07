namespace RDPGW.Protocol;

/// <summary>
/// Flags indicating which fields are present in the HTTP tunnel authentication packet.
/// </summary>
internal enum HTTP_TUNNEL_AUTH_FIELDS_PRESENT_FLAGS : ushort
{
    /// <summary>
    /// Indicates that the Statement of Health (SOH) field is present.
    /// </summary>
    HTTP_TUNNEL_AUTH_FIELD_SOH = 0x1
}
