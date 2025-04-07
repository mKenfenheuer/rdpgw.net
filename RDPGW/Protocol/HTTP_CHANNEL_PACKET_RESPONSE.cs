using System.Threading.Channels;

namespace RDPGW.Protocol;

internal class HTTP_CHANNEL_PACKET_RESPONSE : HTTP_PACKET
{
    internal uint ErrorCode { get; set; }
    internal HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }
    internal uint? ChannelId { get; set; }
    internal ushort? UDPPort { get; set; }
    internal HTTP_BYTE_BLOB? AuthnCookie { get; set; }

    public HTTP_CHANNEL_PACKET_RESPONSE(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count < 16)
            throw new Exception($"HTTP_CHANNEL_PACKET_RESPONSE data byte count mismatch. Expected at least 16 bytes, got {data.Count}");
        
        ErrorCode = BitConverter.ToUInt32(data.Take(4).ToArray());
        FieldsPresent = (HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());

        int skip = 16;

        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_CHANNELID) != 0)
        {
            ChannelId = BitConverter.ToUInt32(data.Skip(skip).Take(4).ToArray());
            skip +=4;
        }

        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_UDPPORT) != 0)
        {
            UDPPort = BitConverter.ToUInt16(data.Skip(skip).Take(2).ToArray());
            skip +=2;
        }

        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_AUTHNCOOKIE) != 0)
        {
            AuthnCookie = new HTTP_BYTE_BLOB(data.Skip(skip).ToArray());
        }
    }

    public HTTP_CHANNEL_PACKET_RESPONSE() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_CHANNEL_RESPONSE))
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

        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_CHANNELID) != 0 && ChannelId != null)
        {
            bytes.AddRange(BitConverter.GetBytes((uint)ChannelId));
        }

        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_UDPPORT) != 0 && UDPPort != null)
        {
            bytes.AddRange(BitConverter.GetBytes((ushort)UDPPort));
        }

        if ((FieldsPresent & HTTP_CHANNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_CHANNEL_RESPONSE_FIELD_AUTHNCOOKIE) != 0 && AuthnCookie != null)
        {
            bytes.AddRange(AuthnCookie.GetBytes());
        }

        return bytes.ToArray();
    }
}
