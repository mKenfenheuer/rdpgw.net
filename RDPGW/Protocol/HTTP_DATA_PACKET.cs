namespace RDPGW.Protocol;

internal class HTTP_DATA_PACKET : HTTP_PACKET
{
    internal HTTP_BYTE_BLOB Data { get; set; }
    public HTTP_DATA_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        Data = new HTTP_BYTE_BLOB(data);
    }

    public HTTP_DATA_PACKET(ArraySegment<byte> data) : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_DATA))
    {
        Data = new HTTP_BYTE_BLOB();
        Data.Data = data.ToArray();
        Data.Length = (ushort)data.Count();
    }

    internal override ArraySegment<byte> DataToBytes() => Data.GetBytes();
}

internal class HTTP_KEEPALIVE_PACKET : HTTP_PACKET
{
    public HTTP_KEEPALIVE_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
    }

    public HTTP_KEEPALIVE_PACKET() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_DATA))
    {
    }

    internal override ArraySegment<byte> DataToBytes() => [];
}
