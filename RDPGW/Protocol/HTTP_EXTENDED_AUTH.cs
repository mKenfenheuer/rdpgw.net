namespace RDPGW.Protocol;

/// <summary>
/// Represents the extended authentication methods supported by the protocol.
/// </summary>
internal enum HTTP_EXTENDED_AUTH : ushort
{
    /// <summary>No extended authentication.</summary>
    HTTP_EXTENDED_AUTH_NONE = 0x00,

    /// <summary>Smart card authentication.</summary>
    HTTP_EXTENDED_AUTH_SC = 0x01,

    /// <summary>Pluggable authentication architecture.</summary>
    HTTP_EXTENDED_AUTH_PAA = 0x02,

    /// <summary>SSPI NTLM authentication.</summary>
    HTTP_EXTENDED_AUTH_SSPI_NTLM = 0x04,
}
