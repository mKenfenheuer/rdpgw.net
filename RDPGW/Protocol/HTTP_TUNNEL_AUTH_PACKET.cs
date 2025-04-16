namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP tunnel authentication packet.
/// </summary>
public class HTTP_TUNNEL_AUTH_PACKET : HTTP_PACKET
{
    /// <summary>
    /// Flags indicating which fields are present in the packet.
    /// </summary>
    public HTTP_TUNNEL_AUTH_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }

    /// <summary>
    /// The client name in the packet.
    /// </summary>
    public HTTP_UNICODE_STRING ClientName { get; set; }

    /// <summary>
    /// Optional Statement of Health (SOH) field.
    /// </summary>
    public HTTP_BYTE_BLOB? StatementOfHealth { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_TUNNEL_AUTH_PACKET"/> class.
    /// </summary>
    /// <param name="header">The packet header.</param>
    /// <param name="data">The packet data.</param>
    public HTTP_TUNNEL_AUTH_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        // Ensure the data contains at least the minimum required bytes.
        if (data.Count < 13)
            throw new ArgumentException($"HTTP_TUNNEL_AUTH_PACKET data byte count mismatch. Expected at least 13 bytes, got {data.Count}");

        // Parse the fields present flags.
        FieldsPresent = (HTTP_TUNNEL_AUTH_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Take(2).ToArray());

        // Parse the client name.
        ClientName = new HTTP_UNICODE_STRING(data.Skip(2).ToArray());

        // Calculate the offset for optional fields.
        int skip = ClientName.TotalLength + 2;

        // Parse the Statement of Health (SOH) field if present.
        if ((FieldsPresent & HTTP_TUNNEL_AUTH_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_FIELD_SOH) != 0)
        {
            StatementOfHealth = new HTTP_BYTE_BLOB(data.Skip(skip).ToArray());
        }
    }

    /// <summary>
    /// Converts the packet data to a byte array.
    /// </summary>
    /// <returns>A byte array representing the packet data.</returns>
    public override ArraySegment<byte> DataToBytes()
    {
        // Set the FieldsPresent flags to include the SOH field if present.
        FieldsPresent = 0;
        if (StatementOfHealth != null)
        {
            FieldsPresent |= HTTP_TUNNEL_AUTH_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_FIELD_SOH;
        }

        // Initialize a list to hold the byte data.
        List<byte> bytes =
        [
            // Add the fields present flags.
            .. BitConverter.GetBytes((ushort)FieldsPresent),
            // Add the client name.
            .. ClientName.GetBytes(),
        ];

        // Add the Statement of Health (SOH) field if present.
        if (StatementOfHealth != null)
        {
            bytes.AddRange(StatementOfHealth.GetBytes());
        }

        return bytes.ToArray();
    }
}
