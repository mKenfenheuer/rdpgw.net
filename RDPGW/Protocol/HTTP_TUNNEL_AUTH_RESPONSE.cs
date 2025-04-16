namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP tunnel authentication response packet.
/// </summary>
public class HTTP_TUNNEL_AUTH_RESPONSE : HTTP_PACKET
{
    /// <summary>
    /// The error code of the response.
    /// </summary>
    public uint ErrorCode { get; set; }

    /// <summary>
    /// Flags indicating which fields are present in the response.
    /// </summary>
    public HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }

    /// <summary>
    /// Optional redirection flags.
    /// </summary>
    public HTTP_TUNNEL_REDIR_FLAGS? RedirectionFlags { get; set; }

    /// <summary>
    /// Optional idle timeout value.
    /// </summary>
    public uint? IdleTimeout { get; set; }

    /// <summary>
    /// Optional Statement of Health (SOH) response.
    /// </summary>
    public HTTP_BYTE_BLOB? StatementOfHealthResponse { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_TUNNEL_AUTH_RESPONSE"/> class.
    /// </summary>
    /// <param name="header">The packet header.</param>
    /// <param name="data">The packet data.</param>
    public HTTP_TUNNEL_AUTH_RESPONSE(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        // Ensure the data contains at least the minimum required bytes.
        if (data.Count < 8)
            throw new ArgumentException($"HTTP_TUNNEL_AUTH_RESPONSE data byte count mismatch. Expected at least 8 bytes, got {data.Count}");

        // Parse the error code.
        ErrorCode = BitConverter.ToUInt32(data.Take(4).ToArray());

        // Parse the fields present flags.
        FieldsPresent = (HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());

        // Initialize the offset for optional fields.
        int skip = 8;

        // Parse the redirection flags if present.
        if ((FieldsPresent & HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_REDIR_FLAGS) != 0)
        {
            RedirectionFlags = (HTTP_TUNNEL_REDIR_FLAGS)BitConverter.ToUInt32(data.Skip(skip).Take(4).ToArray());
            skip += 4;
        }

        // Parse the idle timeout if present.
        if ((FieldsPresent & HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_IDLE_TIMEOUT) != 0)
        {
            IdleTimeout = BitConverter.ToUInt32(data.Skip(skip).Take(4).ToArray());
            skip += 4;
        }

        // Parse the Statement of Health (SOH) response if present.
        if ((FieldsPresent & HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_SOH_RESPONSE) != 0)
        {
            StatementOfHealthResponse = new HTTP_BYTE_BLOB(data.Skip(skip).ToArray());
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_TUNNEL_AUTH_RESPONSE"/> class with default values.
    /// </summary>
    public HTTP_TUNNEL_AUTH_RESPONSE() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_TUNNEL_AUTH_RESPONSE))
    {
    }

    /// <summary>
    /// Converts the packet data to a byte array.
    /// </summary>
    /// <returns>A byte array representing the packet data.</returns>
    public override ArraySegment<byte> DataToBytes()
    {
        // Set FieldsPresent according to the properties that are set.
        FieldsPresent = 0;
        if (RedirectionFlags != null)
            FieldsPresent |= HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_REDIR_FLAGS;
        if (IdleTimeout != null)
            FieldsPresent |= HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_IDLE_TIMEOUT;
        if (StatementOfHealthResponse != null)
            FieldsPresent |= HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_SOH_RESPONSE;

        // Initialize a list to hold the byte data.
        List<byte> bytes =
        [
            // Add the error code.
            .. BitConverter.GetBytes(ErrorCode),
            // Add the fields present flags.
            .. BitConverter.GetBytes((ushort)FieldsPresent),
            0,
            0
        ];

        // Add the redirection flags if present.
        if (RedirectionFlags != null)
        {
            bytes.AddRange(BitConverter.GetBytes((uint)RedirectionFlags));
        }

        // Add the idle timeout if present.
        if (IdleTimeout != null)
        {
            bytes.AddRange(BitConverter.GetBytes((uint)IdleTimeout));
        }

        // Add the Statement of Health (SOH) response if present.
        if (StatementOfHealthResponse != null)
        {
            bytes.AddRange(StatementOfHealthResponse.GetBytes());
        }

        return bytes.ToArray();
    }
}
