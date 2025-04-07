namespace RDPGW.Protocol;

/// <summary>
/// Flags indicating which fields are present in an HTTP tunnel response.
/// </summary>
internal enum HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS : ushort
{
    /// <summary>
    /// Indicates that the Tunnel ID field is present.
    /// </summary>
    HTTP_TUNNEL_RESPONSE_FIELD_TUNNEL_ID = 0x01,

    /// <summary>
    /// Indicates that the capabilities field is present.
    /// </summary>
    HTTP_TUNNEL_RESPONSE_FIELD_CAPS = 0x02,

    /// <summary>
    /// Indicates that the SOH (Statement of Health) request field is present.
    /// </summary>
    HTTP_TUNNEL_RESPONSE_FIELD_SOH_REQ = 0x04,

    /// <summary>
    /// Indicates that the consent message field is present.
    /// </summary>
    HTTP_TUNNEL_RESPONSE_FIELD_CONSENT_MSG = 0x10,
}
