using System.Threading.Channels;

namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP channel packet response.
/// </summary>
internal class HTTP_CHANNEL_PACKET_RESPONSE : HTTP_PACKET
{
    /// <summary>
    /// Gets or sets the error code of the response.
    /// </summary>
    internal uint ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the flags indicating which fields are present in the response.
    /// </summary>
    internal HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }

    /// <summary>
    /// Gets or sets the optional Channel ID.
    /// </summary>
    internal uint? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the optional UDP port.
    /// </summary>
    internal ushort? UDPPort { get; set; }

    /// <summary>
    /// Gets or sets the optional authentication cookie.
    /// </summary>
    internal HTTP_BYTE_BLOB? AuthnCookie { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_CHANNEL_PACKET_RESPONSE"/> class with a header and data.
    /// </summary>
    public HTTP_CHANNEL_PACKET_RESPONSE(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        // Validate the minimum data length.
        if (data.Count < 16)
            throw new Exception($"HTTP_CHANNEL_PACKET_RESPONSE data byte count mismatch. Expected at least 16 bytes, got {data.Count}");

        // Parse the error code and fields present flags.
        ErrorCode = BitConverter.ToUInt32(data.Take(4).ToArray());
        FieldsPresent = (HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());

        int skip = 16;

        // Parse the optional Channel ID if present.
        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_CHANNELID) != 0)
        {
            ChannelId = BitConverter.ToUInt32(data.Skip(skip).Take(4).ToArray());
            skip += 4;
        }

        // Parse the optional UDP port if present.
        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_UDPPORT) != 0)
        {
            UDPPort = BitConverter.ToUInt16(data.Skip(skip).Take(2).ToArray());
            skip += 2;
        }

        // Parse the optional authentication cookie if present.
        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_AUTHNCOOKIE) != 0)
        {
            AuthnCookie = new HTTP_BYTE_BLOB(data.Skip(skip).ToArray());
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_CHANNEL_PACKET_RESPONSE"/> class with default values.
    /// </summary>
    public HTTP_CHANNEL_PACKET_RESPONSE() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_CHANNEL_RESPONSE))
    {
    }

    /// <summary>
    /// Converts the channel packet response to a byte array segment.
    /// </summary>
    internal override ArraySegment<byte> DataToBytes()
    {
        // Initialize the byte list with the error code and fields present flags.
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(ErrorCode),
            .. BitConverter.GetBytes((ushort)FieldsPresent),
            0,
            0
        ];

        // Add the optional Channel ID to the byte list if present.
        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_CHANNELID) != 0 && ChannelId != null)
        {
            bytes.AddRange(BitConverter.GetBytes((uint)ChannelId));
        }

        // Add the optional UDP port to the byte list if present.
        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_UDPPORT) != 0 && UDPPort != null)
        {
            bytes.AddRange(BitConverter.GetBytes((ushort)UDPPort));
        }

        // Add the optional authentication cookie to the byte list if present.
        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_AUTHNCOOKIE) != 0 && AuthnCookie != null)
        {
            bytes.AddRange(AuthnCookie.GetBytes());
        }

        return bytes.ToArray();
    }
}
