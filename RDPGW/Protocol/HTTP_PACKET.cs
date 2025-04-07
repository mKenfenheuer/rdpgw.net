namespace RDPGW.Protocol;

internal abstract class HTTP_PACKET
{
    internal HTTP_PACKET_HEADER Header { get; set; }

    internal HTTP_PACKET(HTTP_PACKET_HEADER header)
    {
        Header = header;
    }

    internal abstract ArraySegment<byte> DataToBytes();

    internal ArraySegment<byte> ToBytes()
    {
        var data = DataToBytes();
        Header.PacketLength = (uint)(8 + data.Count);
        return Header.ToBytes().Concat(data).ToArray();
    }

    public static HTTP_PACKET FromBytes(ArraySegment<byte> data)
    {
        var header = new HTTP_PACKET_HEADER(data.Take(8).ToArray());
        var packetBytes = data.Skip(8).ToArray();
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
                return new HTTP_TUNNEL_AUTH_RESPONSE(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_DATA:
                return new HTTP_DATA_PACKET(header, packetBytes);
            case HTTP_PACKET_TYPE.PKT_TYPE_KEEPALIVE:
                return new HTTP_KEEPALIVE_PACKET(header, packetBytes);
        }
        throw new Exception($"Unknown packet type in header: 0x{header.PacketType:X}");
    }
}
