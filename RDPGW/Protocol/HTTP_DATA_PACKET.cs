namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP data packet.
/// </summary>
internal class HTTP_DATA_PACKET : HTTP_PACKET
{
    /// <summary>
    /// Gets or sets the data blob contained in the packet.
    /// </summary>
    internal HTTP_BYTE_BLOB Data { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_DATA_PACKET"/> class with a header and data.
    /// </summary>
    public HTTP_DATA_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        // Initialize the data blob with the provided data.
        Data = new HTTP_BYTE_BLOB(data);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_DATA_PACKET"/> class with data only.
    /// </summary>
    public HTTP_DATA_PACKET(ArraySegment<byte> data) : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_DATA))
    {
        // Create a new data blob and populate it with the provided data.
        Data = new HTTP_BYTE_BLOB();
        Data.Data = data.ToArray();
        Data.Length = (ushort)data.Count();
    }

    /// <summary>
    /// Converts the data blob to a byte array segment.
    /// </summary>
    internal override ArraySegment<byte> DataToBytes() => Data.GetBytes();
}

/// <summary>
/// Represents an HTTP keep-alive packet.
/// </summary>
internal class HTTP_KEEPALIVE_PACKET : HTTP_PACKET
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_KEEPALIVE_PACKET"/> class with a header and data.
    /// </summary>
    public HTTP_KEEPALIVE_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        // No additional initialization required for keep-alive packets.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_KEEPALIVE_PACKET"/> class with a default header.
    /// </summary>
    public HTTP_KEEPALIVE_PACKET() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_DATA))
    {
        // No additional initialization required for keep-alive packets.
    }

    /// <summary>
    /// Converts the keep-alive packet to a byte array segment.
    /// </summary>
    internal override ArraySegment<byte> DataToBytes() => [];
}
