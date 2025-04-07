namespace RDPGW.Protocol;

internal class HTTP_BYTE_BLOB
{
    internal ushort Length { get; set; }
    internal byte[] Data { get; set; }
    internal int TotalLength => Length + 4;
    public HTTP_BYTE_BLOB(ArraySegment<byte> data)
    {
        Length = BitConverter.ToUInt16(data.Take(2).ToArray(), 0);
        Data = data.Skip(2).Take(Length).ToArray();
    }

    public HTTP_BYTE_BLOB()
    {
        Data = [];
        Length = 0;
    }

    internal ArraySegment<byte> GetBytes()
    {
        Length = (ushort)Data.Count();
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(Length),
            .. Data,
        ];
        return bytes.ToArray();
    }
}