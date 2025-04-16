namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP handshake response packet.
/// </summary>
public class HTTP_HANDSHAKE_RESPONSE_PACKET : HTTP_PACKET
{
    /// <summary>Gets or sets the error code of the response.</summary>
    public uint ErrorCode { get; set; }

    /// <summary>Gets or sets the major version of the protocol.</summary>
    public byte VersionMajor { get; set; }

    /// <summary>Gets or sets the minor version of the protocol.</summary>
    public byte VersionMinor { get; set; }

    /// <summary>Gets or sets the server version.</summary>
    public ushort ServerVersion { get; set; }

    /// <summary>Gets or sets the extended authentication method.</summary>
    public HTTP_EXTENDED_AUTH ExtendedAuth { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_HANDSHAKE_RESPONSE_PACKET"/> class from raw data.
    /// </summary>
    /// <param name="header">The packet header.</param>
    /// <param name="data">The raw data containing the packet body.</param>
    public HTTP_HANDSHAKE_RESPONSE_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count != 10)
            throw new ArgumentException($"HTTP_HANDSHAKE_RESPONSE_PACKET data byte count mismatch. Expected 10 bytes, got {data.Count}");

        // Parse the fields from the raw data.
        ErrorCode = BitConverter.ToUInt32(data.Take(4).ToArray());
        VersionMajor = data[4];
        VersionMinor = data[5];
        ServerVersion = BitConverter.ToUInt16(data.Skip(6).Take(2).ToArray());
        ExtendedAuth = (HTTP_EXTENDED_AUTH)BitConverter.ToUInt16(data.Skip(8).ToArray());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_HANDSHAKE_RESPONSE_PACKET"/> class with default values.
    /// </summary>
    public HTTP_HANDSHAKE_RESPONSE_PACKET() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_HANDSHAKE_RESPONSE))
    {
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
            .. BitConverter.GetBytes(ErrorCode),
            VersionMajor,
            VersionMinor,
            .. BitConverter.GetBytes(ServerVersion),
            .. BitConverter.GetBytes((ushort)ExtendedAuth),
        ];
        return bytes.ToArray();
    }
}
