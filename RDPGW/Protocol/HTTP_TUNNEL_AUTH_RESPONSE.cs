namespace RDPGW.Protocol;

internal class HTTP_TUNNEL_AUTH_RESPONSE : HTTP_PACKET
{
    internal uint ErrorCode { get; set; }
    internal HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }
    internal HTTP_TUNNEL_REDIR_FLAGS? RedirectionFlags { get; set; }
    internal uint? IdleTimeout { get; set; }
    internal HTTP_BYTE_BLOB? StatementOfHealthResponse { get; set; }

    public HTTP_TUNNEL_AUTH_RESPONSE(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count < 8)
            throw new Exception($"HTTP_TUNNEL_AUTH_RESPONSE data byte count mismatch. Expected at least 8 bytes, got {data.Count}");
        ErrorCode = BitConverter.ToUInt32(data.Take(4).ToArray());
        FieldsPresent = (HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());

        int skip = 8;

        if ((FieldsPresent & HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_REDIR_FLAGS) != 0)
        {
            RedirectionFlags = (HTTP_TUNNEL_REDIR_FLAGS)BitConverter.ToUInt32(data.Skip(skip).Take(4).ToArray());
            skip += 4;
        }
        if ((FieldsPresent & HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_IDLE_TIMEOUT) != 0)
        {
            IdleTimeout = BitConverter.ToUInt32(data.Skip(skip).Take(4).ToArray());
            skip += 4;
        }
        if ((FieldsPresent & HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_SOH_RESPONSE) != 0)
        {
            StatementOfHealthResponse = new HTTP_BYTE_BLOB(data.Skip(skip).ToArray());
        }
    }

    public HTTP_TUNNEL_AUTH_RESPONSE() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_TUNNEL_AUTH_RESPONSE))
    {
    }

    internal override ArraySegment<byte> DataToBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(ErrorCode),
            .. BitConverter.GetBytes((ushort)FieldsPresent),
            0,
            0
        ];

        if ((FieldsPresent & HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_REDIR_FLAGS) != 0 && RedirectionFlags != null)
        {
            bytes.AddRange(BitConverter.GetBytes((uint)RedirectionFlags));
        }
        if ((FieldsPresent & HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_IDLE_TIMEOUT) != 0 && IdleTimeout != null)
        {
            bytes.AddRange(BitConverter.GetBytes((uint)IdleTimeout));
        }
        if ((FieldsPresent & HTTP_TUNNEL_AUTH_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_AUTH_RESPONSE_FIELD_SOH_RESPONSE) != 0 && StatementOfHealthResponse != null)
        {
            bytes.AddRange(StatementOfHealthResponse.GetBytes());
        }
        
        return bytes.ToArray();
    }
}
