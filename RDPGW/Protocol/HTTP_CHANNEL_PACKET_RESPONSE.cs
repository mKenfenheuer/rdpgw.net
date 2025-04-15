using System.Threading.Channels;

namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP channel packet response.
/// </summary>
public class HTTP_CHANNEL_PACKET_RESPONSE : HTTP_PACKET
{
    /// <summary>
    /// Gets or sets the error code of the response.
    /// </summary>
    public uint ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the flags indicating which fields are present in the response.
    /// </summary>
    public HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }

    /// <summary>
    /// Gets or sets the optional Channel ID.
    /// </summary>
    public uint? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the optional UDP port.
    /// </summary>
    public ushort? UDPPort { get; set; }

    /// <summary>
    /// Gets or sets the optional authentication cookie.
    /// </summary>
    public HTTP_BYTE_BLOB? AuthnCookie { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_CHANNEL_PACKET_RESPONSE"/> class with a header and data.
    /// </summary>
    public HTTP_CHANNEL_PACKET_RESPONSE(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        // Validate the minimum data length.
        if (data.Count < 8)
            throw new Exception($"HTTP_CHANNEL_PACKET_RESPONSE data byte count mismatch. Expected at least 8 bytes, got {data.Count}");

        // Parse the error code and fields present flags.
        ErrorCode = BitConverter.ToUInt32(data.Take(4).ToArray());
        FieldsPresent = (HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());

        int skip = 8;

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
    public override ArraySegment<byte> DataToBytes()
    {
        // Calculate FieldsPresent based on the optional fields.
        FieldsPresent = 0;
        if (ChannelId != null)
            FieldsPresent |= HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_CHANNELID;
        if (UDPPort != null)
            FieldsPresent |= HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_UDPPORT;
        if (AuthnCookie != null)
            FieldsPresent |= HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_AUTHNCOOKIE;

        // Initialize the byte list with the error code and fields present flags.
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(ErrorCode),
            .. BitConverter.GetBytes((ushort)FieldsPresent),
            0,
            0
        ];

        // Add the optional Channel ID to the byte list if present.
        if (ChannelId != null)
        {
            bytes.AddRange(BitConverter.GetBytes((uint)ChannelId));
        }

        // Add the optional UDP port to the byte list if present.
        if (UDPPort != null)
        {
            bytes.AddRange(BitConverter.GetBytes((ushort)UDPPort));
        }

        // Add the optional authentication cookie to the byte list if present.
        if (AuthnCookie != null)
        {
            bytes.AddRange(AuthnCookie.GetBytes());
        }

        return bytes.ToArray();
    }
}
