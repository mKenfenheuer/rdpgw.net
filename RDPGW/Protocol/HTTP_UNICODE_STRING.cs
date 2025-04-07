using System.Text;

namespace RDPGW.Protocol;

internal class HTTP_UNICODE_STRING
{
    internal ushort Length { get; set; }
    internal string String { get; set; }
    internal int TotalLength => Length + 4;
    public HTTP_UNICODE_STRING(ArraySegment<byte> data)
    {
        Length = BitConverter.ToUInt16(data.Take(2).ToArray(), 0);
        String = Encoding.Unicode.GetString(data.Skip(2).Take(Length).ToArray());
    }

    public HTTP_UNICODE_STRING(string str)
    {
        String = str;
        Length = (ushort)Encoding.Unicode.GetByteCount(str);
    }

    internal ArraySegment<byte> GetBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(Length),
            .. Encoding.Unicode.GetBytes(String),
        ];
        return bytes.ToArray();
    }
}
