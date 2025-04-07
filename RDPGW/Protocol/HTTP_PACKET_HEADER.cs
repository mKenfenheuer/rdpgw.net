namespace RDPGW.Protocol;

internal class HTTP_PACKET_HEADER
{
    internal HTTP_PACKET_TYPE PacketType { get; set; }
    internal uint PacketLength { get; set; }

    public HTTP_PACKET_HEADER(ArraySegment<byte> data)
    {
        PacketType = (HTTP_PACKET_TYPE)BitConverter.ToUInt16(data.Take(2).ToArray());
        PacketLength = BitConverter.ToUInt32(data.Skip(4).ToArray());
    }

    public HTTP_PACKET_HEADER(HTTP_PACKET_TYPE type)
    {
        PacketType = type;
        PacketLength = 0;
    }

    internal ArraySegment<byte> ToBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes((ushort)PacketType),
            0,
            0,
            .. BitConverter.GetBytes(PacketLength),
        ];
        return bytes.ToArray();
    }
}
