namespace RDPGW.Protocol;

/// <summary>
/// Represents the HTTP capability types supported by the protocol.
/// </summary>
public enum HTTP_CAPABILITY_TYPE : uint
{
    /// <summary>
    /// Capability for quarantine state of health (SOH).
    /// </summary>
    HTTP_CAPABILITY_TYPE_QUAR_SOH = 0x1,

    /// <summary>
    /// Capability for idle timeout configuration.
    /// </summary>
    HTTP_CAPABILITY_IDLE_TIMEOUT = 0x2,

    /// <summary>
    /// Capability for messaging consent signature.
    /// </summary>
    HTTP_CAPABILITY_MESSAGING_CONSENT_SIGN = 0x4,

    /// <summary>
    /// Capability for messaging service messages.
    /// </summary>
    HTTP_CAPABILITY_MESSAGING_SERVICE_MSG = 0x8,

    /// <summary>
    /// Capability for reauthentication.
    /// </summary>
    HTTP_CAPABILITY_REAUTH = 0x10,

    /// <summary>
    /// Capability for UDP transport support.
    /// </summary>
    HTTP_CAPABILITY_UDP_TRANSPORT = 0x20
}