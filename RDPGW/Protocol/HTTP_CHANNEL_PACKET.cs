namespace RDPGW.Protocol;

internal class HTTP_CHANNEL_PACKET : HTTP_PACKET
{
    internal ushort Port { get; set; }
    internal ushort Protocol { get; set; }
    internal string[] Resources { get; set; }
    internal string[] AltResources { get; set; }

    public HTTP_CHANNEL_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        if (data.Count < 6)
            throw new Exception($"HTTP_CHANNEL_PACKET data byte count mismatch. Expected at least 6 bytes, got {data.Count}");
        var numResources = data[0];
        var numAltResources = data[1];

        Port = BitConverter.ToUInt16(data.Skip(2).Take(2).ToArray());
        Protocol = BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());

        int skip = 6;

        var resources = new List<string>();
        for (int i = 0; i < numResources; i++)
        {
            var str = new HTTP_UNICODE_STRING(data.Skip(skip).ToArray());
            resources.Add(str.String);
            skip += str.TotalLength;
        }
        Resources = resources.ToArray();
        
        var altResources = new List<string>();
        for (int i = 0; i < numAltResources; i++)
        {
            var str = new HTTP_UNICODE_STRING(data.Skip(skip).ToArray());
            altResources.Add(str.String);
            skip += str.TotalLength;
        }
        AltResources = altResources.ToArray();
    }

    internal override ArraySegment<byte> DataToBytes()
    {
        List<byte> bytes =
        [
            (byte)Resources.Count(),
            (byte)AltResources.Count(),
            .. BitConverter.GetBytes((ushort)Port),
            .. BitConverter.GetBytes((ushort)Protocol),
        ];

        foreach (var str in Resources)
            bytes.AddRange(new HTTP_UNICODE_STRING(str).GetBytes());

        foreach (var str in AltResources)
            bytes.AddRange(new HTTP_UNICODE_STRING(str).GetBytes());

        return bytes.ToArray();
    }
}
