namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP handshake request packet.
/// </summary>
internal class HTTP_HANDSHAKE_REQUEST_PACKET : HTTP_PACKET
{
    /// <summary>Gets or sets the major version of the protocol.</summary>
    internal byte VersionMajor { get; set; }

    /// <summary>Gets or sets the minor version of the protocol.</summary>
    internal byte VersionMinor { get; set; }

    /// <summary>Gets or sets the client version.</summary>
    internal ushort ClientVersion { get; set; }

    /// <summary>Gets or sets the extended authentication method.</summary>
    internal HTTP_EXTENDED_AUTH ExtendedAuth { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_HANDSHAKE_REQUEST_PACKET"/> class from raw data.
    /// </summary>
    /// <param name="header">The packet header.</param>
    /// <param name="data">The raw data containing the packet body.</param>
    public HTTP_HANDSHAKE_REQUEST_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count != 6)
            throw new Exception($"HTTP_HANDSHAKE_REQUEST_PACKET data byte count mismatch. Expected 6 bytes, got {data.Count}");

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
    internal override ArraySegment<byte> DataToBytes()
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
