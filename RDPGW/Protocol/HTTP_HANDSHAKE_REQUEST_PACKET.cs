namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP handshake request packet.
/// </summary>
public class HTTP_HANDSHAKE_REQUEST_PACKET : HTTP_PACKET
{
    /// <summary>Gets or sets the major version of the protocol.</summary>
    public byte VersionMajor { get; set; }

    /// <summary>Gets or sets the minor version of the protocol.</summary>
    public byte VersionMinor { get; set; }

    /// <summary>Gets or sets the client version.</summary>
    public ushort ClientVersion { get; set; }

    /// <summary>Gets or sets the extended authentication method.</summary>
    public HTTP_EXTENDED_AUTH ExtendedAuth { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_HANDSHAKE_REQUEST_PACKET"/> class from raw data.
    /// </summary>
    /// <param name="header">The packet header.</param>
    /// <param name="data">The raw data containing the packet body.</param>
    public HTTP_HANDSHAKE_REQUEST_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count != 6)
            throw new ArgumentException($"HTTP_HANDSHAKE_REQUEST_PACKET data byte count mismatch. Expected 6 bytes, got {data.Count}");

        // Parse the fields from the raw data.
        VersionMajor = data[0];
        VersionMinor = data[1];
        ClientVersion = BitConverter.ToUInt16(data.Skip(2).Take(2).ToArray());
        ExtendedAuth = (HTTP_EXTENDED_AUTH)BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());
    }

    /// <summary>
    /// Converts the packet data to a byte array.
    /// </summary>
    /// <returns>A byte array representing the packet data.</returns>
    public override ArraySegment<byte> DataToBytes()
    {
        // Construct the byte array for the packet data.
        List<byte> bytes =
        [
            VersionMajor,
            VersionMinor,
            .. BitConverter.GetBytes(ClientVersion),
            .. BitConverter.GetBytes((ushort)ExtendedAuth),
        ];
        return bytes.ToArray();
    }
}
