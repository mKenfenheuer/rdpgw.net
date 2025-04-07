namespace RDPGW.Protocol;

internal class HTTP_TUNNEL_PACKET : HTTP_PACKET
{
    internal HTTP_CAPABILITY_TYPE CapabilityFlags { get; set; }
    internal HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }
    internal byte[]? ReauthContext { get; set; }
    internal HTTP_BYTE_BLOB? PAACookie { get; set; }

    public HTTP_TUNNEL_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count < 8)
            throw new Exception($"HTTP_TUNNEL_PACKET data byte count mismatch. Expected at least 8 bytes, got {data.Count}");
        CapabilityFlags = (HTTP_CAPABILITY_TYPE)BitConverter.ToUInt32(data.Take(4).ToArray());
        FieldsPresent = (HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());

        int skip = 8;
        if ((FieldsPresent & HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_PACKET_FIELD_REAUTH) != 0)
        {
            ReauthContext = data.Skip(skip).Take(8).ToArray();
            skip += 8;
        }
        if ((FieldsPresent & HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_PACKET_FIELD_PAA_COOKIE) != 0)
        {
            PAACookie = new HTTP_BYTE_BLOB(data.Skip(skip).ToArray());
        }
    }

    internal override ArraySegment<byte> DataToBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes((uint)CapabilityFlags),
            .. BitConverter.GetBytes((ushort)FieldsPresent),
            0,
            0,
        ];

        if ((FieldsPresent & HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_PACKET_FIELD_REAUTH) != 0 && ReauthContext != null)
        {
            bytes.AddRange(ReauthContext);
        }
        if ((FieldsPresent & HTTP_TUNNEL_PACKET_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_PACKET_FIELD_PAA_COOKIE) != 0 && PAACookie != null)
        {
            bytes.AddRange(PAACookie.GetBytes());
        }
        
        return bytes.ToArray();
    }
}
