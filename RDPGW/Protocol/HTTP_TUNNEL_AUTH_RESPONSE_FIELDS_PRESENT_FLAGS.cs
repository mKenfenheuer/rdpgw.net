namespace RDPGW.Protocol;

/// <summary>
/// Flags indicating which fields are present in the HTTP tunnel authentication response packet.
/// </summary>
public enum HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS : ushort
{
    /// <summary>
    /// Indicates that the redirection flags field is present.
    /// </summary>
    HTTP_TUNNEL_AUTH_RESPONSE_FIELD_REDIR_FLAGS = 0x01,

    /// <summary>
    /// Indicates that the idle timeout field is present.
    /// </summary>
    HTTP_TUNNEL_AUTH_RESPONSE_FIELD_IDLE_TIMEOUT = 0x02,

    /// <summary>
    /// Indicates that the Statement of Health (SOH) response field is present.
    /// </summary>
    HTTP_TUNNEL_AUTH_RESPONSE_FIELD_SOH_RESPONSE = 0x04,
}
