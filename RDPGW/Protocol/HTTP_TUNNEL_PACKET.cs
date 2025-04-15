namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP tunnel packet, which includes capability flags, fields, and optional data.
/// </summary>
public class HTTP_TUNNEL_PACKET : HTTP_PACKET
{
    /// <summary>
    /// Gets or sets the capability flags for the tunnel.
    /// </summary>
    public HTTP_CAPABILITY_TYPE CapabilityFlags { get; set; }

    /// <summary>
    /// Gets or sets the flags indicating which fields are present in the packet.
    /// </summary>
    public HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }

    /// <summary>
    /// Gets or sets the reauthentication context, if present.
    /// </summary>
    public byte[]? ReauthContext { get; set; }

    /// <summary>
    /// Gets or sets the PAA (Pre-Authentication Access) cookie, if present.
    /// </summary>
    public HTTP_BYTE_BLOB? PAACookie { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_TUNNEL_PACKET"/> class.
    /// </summary>
    /// <param name="header">The HTTP packet header.</param>
    /// <param name="data">The raw data of the packet.</param>
    /// <exception cref="Exception">Thrown if the data length is insufficient.</exception>
    public HTTP_TUNNEL_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        // Ensure the data contains at least 8 bytes for the mandatory fields.
        if (data.Count < 8)
            throw new Exception($"HTTP_TUNNEL_PACKET data byte count mismatch. Expected at least 8 bytes, got {data.Count}");

        // Parse the capability flags (4 bytes).
        CapabilityFlags = (HTTP_CAPABILITY_TYPE)BitConverter.ToUInt32(data.Take(4).ToArray());

        // Parse the fields present flags (2 bytes).
        FieldsPresent = (HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());

        int skip = 8;

        // Parse the reauthentication context if the corresponding flag is set.
        if ((FieldsPresent & HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_PACKET_FIELD_REAUTH) != 0)
        {
            ReauthContext = data.Skip(skip).Take(8).ToArray();
            skip += 8;
        }

        // Parse the PAA cookie if the corresponding flag is set.
        if ((FieldsPresent & HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_PACKET_FIELD_PAA_COOKIE) != 0)
        {
            PAACookie = new HTTP_BYTE_BLOB(data.Skip(skip).ToArray());
        }
    }

    /// <summary>
    /// Converts the packet data to a byte array.
    /// </summary>
    /// <returns>A byte array representing the packet data.</returns>
    public override ArraySegment<byte> DataToBytes()
    {
        // Initialize a list to hold the serialized data.
        List<byte> bytes =
        [
            // Add the capability flags (4 bytes).
            .. BitConverter.GetBytes((uint)CapabilityFlags),

            // Add the fields present flags (2 bytes).
            .. BitConverter.GetBytes((ushort)FieldsPresent),

            // Reserved bytes (2 bytes).
            0,
            0,
        ];

        // Add the reauthentication context if present.
        if ((FieldsPresent & HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_PACKET_FIELD_REAUTH) != 0 && ReauthContext != null)
        {
            bytes.AddRange(ReauthContext);
        }

        // Add the PAA cookie if present.
        if ((FieldsPresent & HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_PACKET_FIELD_PAA_COOKIE) != 0 && PAACookie != null)
        {
            bytes.AddRange(PAACookie.GetBytes());
        }
        
        return bytes.ToArray();
    }
}
