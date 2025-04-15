namespace RDPGW.Protocol;

/// <summary>
/// Flags indicating which fields are present in an HTTP channel response.
/// </summary>
public enum HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS : ushort
{
    /// <summary>
    /// Indicates that the Channel ID field is present.
    /// </summary>
    HTTP_CHANNEL_RESPONSE_FIELD_CHANNELID = 0x01,

    /// <summary>
    /// Indicates that the Authentication Cookie field is present.
    /// </summary>
    HTTP_CHANNEL_RESPONSE_FIELD_AUTHNCOOKIE = 0x02,

    /// <summary>
    /// Indicates that the UDP Port field is present.
    /// </summary>
    HTTP_CHANNEL_RESPONSE_FIELD_UDPPORT = 0x04,
}
