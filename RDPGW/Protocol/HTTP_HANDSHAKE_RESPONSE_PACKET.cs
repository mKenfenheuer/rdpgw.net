namespace RDPGW.Protocol;

internal class HTTP_HANDSHAKE_RESPONSE_PACKET : HTTP_PACKET
{
    internal uint ErrorCode { get; set; }
    internal byte VersionMajor { get; set; }
    internal byte VersionMinor { get; set; }
    internal ushort ServerVersion { get; set; }
    internal HTTP_EXTENDED_AUTH ExtendedAuth { get; set; }

    public HTTP_HANDSHAKE_RESPONSE_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count != 10)
            throw new Exception($"HTTP_HANDSHAKE_RESPONSE_PACKET data byte count mismatch. Expected 10 bytes, got {data.Count}");
        ErrorCode = BitConverter.ToUInt32(data.Take(4).ToArray());
        VersionMajor = data[4];
        VersionMinor = data[5];
        ServerVersion = BitConverter.ToUInt16(data.Skip(6).Take(2).ToArray());
        ExtendedAuth = (HTTP_EXTENDED_AUTH)BitConverter.ToUInt16(data.Skip(8).ToArray());
    }

    public HTTP_HANDSHAKE_RESPONSE_PACKET() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_HANDSHAKE_RESPONSE))
    {
    }

    internal override ArraySegment<byte> DataToBytes()
    {
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
