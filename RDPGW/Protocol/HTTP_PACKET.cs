namespace RDPGW.Protocol;

/// <summary>
/// Represents an abstract HTTP packet with a header and data.
/// </summary>
public abstract class HTTP_PACKET
{
    /// <summary>
    /// Gets or sets the header of the HTTP packet.
    /// </summary>
    public HTTP_PACKET_HEADER Header { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_PACKET"/> class.
    /// </summary>
    /// <param name="header">The HTTP packet header.</param>
    public HTTP_PACKET(HTTP_PACKET_HEADER header)
    {
        Header = header;
    }

    /// <summary>
    /// Converts the packet data to a byte array segment.
    /// </summary>
    /// <returns>A byte array segment representing the packet data.</returns>
    public abstract ArraySegment<byte> DataToBytes();

    /// <summary>
    /// Converts the entire packet (header + data) to a byte array segment.
    /// </summary>
    /// <returns>A byte array segment representing the entire packet.</returns>
    public ArraySegment<byte> ToBytes()
    {
        // Convert the data portion of the packet to bytes.
        var data = DataToBytes();

        // Update the packet length in the header (8 bytes for the header + data length).
        Header.PacketLength = (uint)(8 + data.Count);

        // Combine the header bytes and data bytes into a single array.
        return Header.ToBytes().Concat(data).ToArray();
    }

    /// <summary>
    /// Creates an HTTP packet instance from a byte array segment.
    /// </summary>
    /// <param name="data">The byte array segment containing the packet data.</param>
    /// <returns>An instance of <see cref="HTTP_PACKET"/>.</returns>
    /// <exception cref="Exception">Thrown when the packet type is unknown.</exception>
    public static HTTP_PACKET FromBytes(ArraySegment<byte> data)
    {
        // Extract the header from the first 8 bytes of the data.
        var header = new HTTP_PACKET_HEADER(data.Take(8).ToArray());

        // Extract the remaining bytes as the packet data.
        var packetBytes = data.Skip(8).ToArray();

        // Determine the packet type and create the corresponding packet instance.
        switch (header.PacketType)
        {
            case HTTP_PACKET_TYPE.PKT_TYPE_CHANNEL_CREATE:
                return new HTTP_CHANNEL_PACKET(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_CHANNEL_RESPONSE:
                return new HTTP_CHANNEL_PACKET_RESPONSE(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_HANDSHAKE_REQUEST:
                return new HTTP_HANDSHAKE_REQUEST_PACKET(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_HANDSHAKE_RESPONSE:
                return new HTTP_HANDSHAKE_RESPONSE_PACKET(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_TUNNEL_AUTH:
                return new HTTP_TUNNEL_AUTH_PACKET(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_TUNNEL_AUTH_RESPONSE:
                return new HTTP_TUNNEL_AUTH_RESPONSE(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_TUNNEL_CREATE:
                return new HTTP_TUNNEL_PACKET(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_TUNNEL_RESPONSE:
                return new HTTP_TUNNEL_RESPONSE(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_DATA:
                return new HTTP_DATA_PACKET(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_KEEPALIVE:
                return new HTTP_KEEPALIVE_PACKET(header, packetBytes);
        }

        // Throw an exception if the packet type is not recognized.
        throw new Exception($"Unknown packet type in header: 0x{header.PacketType:X}");
    }
}
