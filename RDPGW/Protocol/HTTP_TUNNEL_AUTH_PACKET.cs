namespace RDPGW.Protocol;

internal class HTTP_TUNNEL_AUTH_PACKET : HTTP_PACKET
{
    internal HTTP_TUNNEL_AUTH_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }
    internal HTTP_UNICODE_STRING ClientName { get; set; }
    internal HTTP_BYTE_BLOB? StatementOfHealth { get; set; }

    public HTTP_TUNNEL_AUTH_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count < 13)
            throw new Exception($"HTTP_TUNNEL_AUTH_PACKET data byte count mismatch. Expected at least 13 bytes, got {data.Count}");
        FieldsPresent = (HTTP_TUNNEL_AUTH_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Take(2).ToArray());
        ClientName = new HTTP_UNICODE_STRING(data.Skip(2).ToArray());

        int skip = ClientName.TotalLength + 2;

        if ((FieldsPresent & HTTP_TUNNEL_AUTH_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_FIELD_SOH) != 0)
        {
            StatementOfHealth = new HTTP_BYTE_BLOB(data.Skip(skip).ToArray());
        }
    }

    internal override ArraySegment<byte> DataToBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes((ushort)FieldsPresent),
            .. ClientName.GetBytes(),
        ];

        if ((FieldsPresent & HTTP_TUNNEL_AUTH_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_FIELD_SOH) != 0 && StatementOfHealth != null)
        {
            bytes.AddRange(StatementOfHealth.GetBytes());
        }

        return bytes.ToArray();
    }
}
