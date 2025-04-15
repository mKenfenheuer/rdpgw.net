namespace RDPGW.Protocol;

/// <summary>
/// Represents the header of an HTTP packet.
/// </summary>
public class HTTP_PACKET_HEADER
{
    /// <summary>Gets or sets the type of the packet.</summary>
    public HTTP_PACKET_TYPE PacketType { get; set; }

    /// <summary>Gets or sets the length of the packet.</summary>
    public uint PacketLength { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_PACKET_HEADER"/> class from raw data.
    /// </summary>
    /// <param name="data">The raw data containing the packet header.</param>
    public HTTP_PACKET_HEADER(ArraySegment<byte> data)
    {
        // Extract the packet type from the first 2 bytes.
        PacketType = (HTTP_PACKET_TYPE)BitConverter.ToUInt16(data.Take(2).ToArray());
        // Extract the packet length from bytes 4 to 7.
        PacketLength = BitConverter.ToUInt32(data.Skip(4).ToArray());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_PACKET_HEADER"/> class with a specified packet type.
    /// </summary>
    /// <param name="type">The type of the packet.</param>
    public HTTP_PACKET_HEADER(HTTP_PACKET_TYPE type)
    {
        PacketType = type;
        PacketLength = 0; // Default length is 0.
    }

    /// <summary>
    /// Converts the packet header to a byte array.
    /// </summary>
    /// <returns>A byte array representing the packet header.</returns>
    public ArraySegment<byte> ToBytes()
    {
        // Construct the byte array for the header.
        List<byte> bytes =
        [
            // Add the packet type as a 2-byte value.
            .. BitConverter.GetBytes((ushort)PacketType),
            0, 0, // Reserved bytes.
            // Add the packet length as a 4-byte value.
            .. BitConverter.GetBytes(PacketLength),
        ];
        return bytes.ToArray();
    }
}
