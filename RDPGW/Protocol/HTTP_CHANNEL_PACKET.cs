namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP channel packet.
/// </summary>
public class HTTP_CHANNEL_PACKET : HTTP_PACKET
{
    /// <summary>
    /// Gets or sets the port associated with the channel.
    /// </summary>
    public ushort Port { get; set; }

    /// <summary>
    /// Gets or sets the protocol associated with the channel.
    /// </summary>
    public ushort Protocol { get; set; }

    /// <summary>
    /// Gets or sets the list of resources associated with the channel.
    /// </summary>
    public string[] Resources { get; set; }

    /// <summary>
    /// Gets or sets the list of alternate resources associated with the channel.
    /// </summary>
    public string[] AltResources { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_CHANNEL_PACKET"/> class with a header and data.
    /// </summary>
    public HTTP_CHANNEL_PACKET(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        // Validate the minimum data length.
        if (data.Count < 6)
            throw new ArgumentException($"HTTP_CHANNEL_PACKET data byte count mismatch. Expected at least 6 bytes, got {data.Count}");

        // Parse the number of resources and alternate resources.
        var numResources = data[0];
        var numAltResources = data[1];

        // Parse the port and protocol.
        Port = BitConverter.ToUInt16(data.Skip(2).Take(2).ToArray());
        Protocol = BitConverter.ToUInt16(data.Skip(4).Take(2).ToArray());

        int skip = 6;

        // Parse the resource strings.
        var resources = new List<string>();
        for (int i = 0; i < numResources; i++)
        {
            var str = new HTTP_UNICODE_STRING(data.Skip(skip).ToArray());
            resources.Add(str.String);
            skip += str.TotalLength;
        }
        Resources = resources.ToArray();

        // Parse the alternate resource strings.
        var altResources = new List<string>();
        for (int i = 0; i < numAltResources; i++)
        {
            var str = new HTTP_UNICODE_STRING(data.Skip(skip).ToArray());
            altResources.Add(str.String);
            skip += str.TotalLength;
        }
        AltResources = altResources.ToArray();
    }

    /// <summary>
    /// Converts the channel packet to a byte array segment.
    /// </summary>
    public override ArraySegment<byte> DataToBytes()
    {
        // Initialize the byte list with resource and alternate resource counts, port, and protocol.
        List<byte> bytes =
        [
            (byte)Resources.Count(),
            (byte)AltResources.Count(),
            .. BitConverter.GetBytes((ushort)Port),
            .. BitConverter.GetBytes((ushort)Protocol),
        ];

        // Add the resource strings to the byte list.
        foreach (var str in Resources)
            bytes.AddRange(new HTTP_UNICODE_STRING(str).GetBytes());

        // Add the alternate resource strings to the byte list.
        foreach (var str in AltResources)
            bytes.AddRange(new HTTP_UNICODE_STRING(str).GetBytes());

        return bytes.ToArray();
    }
}
