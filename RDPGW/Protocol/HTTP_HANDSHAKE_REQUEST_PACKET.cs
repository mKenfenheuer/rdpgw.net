namespace RDPGW.Protocol;

internal class HTTP_HANDSHAKE_REQUEST_PACKET : HTTP_PACKET
{
    internal byte VersionMajor { get; set; }
    internal byte VersionMinor { get; set; }
    internal ushort ClientVersion { get; set; }
    internal HTTP_EXTENDED_AUTH ExtendedAuth { get; set; }

    public HTTP_HANDSHAKE_REQUEST_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count != 6)
            throw new Exception($"HTTP_HANDSHAKE_REQUEST_PACKET data byte count mismatch. Expected 6 bytes, got {data.Count}");
        VersionMajor = data[0];
        VersionMinor = data[1];
        ClientVersion = BitConverter.ToUInt16(data.Skip(2).Take(2).ToArray());
        ExtendedAuth = (HTTP_EXTENDED_AUTH)BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());
    }

    internal override ArraySegment<byte> DataToBytes()
    {
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
