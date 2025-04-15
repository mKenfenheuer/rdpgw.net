namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP keep-alive packet.
/// </summary>
public class HTTP_KEEPALIVE_PACKET : HTTP_PACKET
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
    public HTTP_KEEPALIVE_PACKET() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_KEEPALIVE))
    {
        // No additional initialization required for keep-alive packets.
    }

    /// <summary>
    /// Converts the keep-alive packet to a byte array segment.
    /// </summary>
    public override ArraySegment<byte> DataToBytes() => new byte[0]; // No data in keep-alive packets, return an empty byte array.
}
