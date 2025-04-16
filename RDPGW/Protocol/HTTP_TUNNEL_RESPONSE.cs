using RDPGW.Extensions;

namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP tunnel response packet.
/// </summary>
public class HTTP_TUNNEL_RESPONSE : HTTP_PACKET
{
    /// <summary>
    /// Gets or sets the server version.
    /// </summary>
    public ushort ServerVersion { get; set; }

    /// <summary>
    /// Gets or sets the status code of the response.
    /// </summary>
    public uint StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the flags indicating which fields are present.
    /// </summary>
    public HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS FieldsPresent { get; set; }

    /// <summary>
    /// Gets or sets the tunnel ID, if present.
    /// </summary>
    public uint? TunnelId { get; set; }

    /// <summary>
    /// Gets or sets the capability flags, if present.
    /// </summary>
    public HTTP_CAPABILITY_TYPE? CapabilityFlags { get; set; }

    /// <summary>
    /// Gets or sets the nonce, if present.
    /// </summary>
    public Guid? Nonce { get; set; }

    /// <summary>
    /// Gets or sets the server certificate, if present.
    /// </summary>
    public HTTP_UNICODE_STRING? ServerCertificate { get; set; }

    /// <summary>
    /// Gets or sets the consent message, if present.
    /// </summary>
    public HTTP_UNICODE_STRING? ConsentMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_TUNNEL_RESPONSE"/> class from a header and data.
    /// </summary>
    /// <param name="header">The packet header.</param>
    /// <param name="data">The packet data.</param>
    public HTTP_TUNNEL_RESPONSE(HTTP_PACKET_HEADER header, ArraySegment<byte> data) : base(header)
    {
        // Validate the minimum data length.
        if (data.Count < 10)
            throw new ArgumentException($"HTTP_TUNNEL_RESPONSE data byte count mismatch. Expected at least 10 bytes, got {data.Count}");

        // Parse the fixed fields.
        ServerVersion = BitConverter.ToUInt16(data.Take(2).ToArray());
        StatusCode = BitConverter.ToUInt32(data.Skip(2).Take(4).ToArray());
        FieldsPresent = (HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS)BitConverter.ToUInt16(data.Skip(6).Take(2).ToArray());

        int skip = 10;

        // Parse optional fields based on the FieldsPresent flags.
        if ((FieldsPresent & HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_RESPONSE_FIELD_TUNNEL_ID) != 0)
        {
            TunnelId = BitConverter.ToUInt32(data.Skip(skip).Take(4).ToArray());
            skip += 4;
        }
        if ((FieldsPresent & HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_RESPONSE_FIELD_CAPS) != 0)
        {
            CapabilityFlags = (HTTP_CAPABILITY_TYPE)BitConverter.ToUInt32(data.Skip(skip).Take(4).ToArray());
            skip += 4;
        }
        if ((FieldsPresent & HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_RESPONSE_FIELD_SOH_REQ) != 0)
        {
            Nonce = MarshalExtensions.StructFromArraySegment<Guid>(data.Skip(skip).Take(16).ToArray());
            skip += 16;
        }
        if ((FieldsPresent & HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_RESPONSE_FIELD_SOH_REQ) != 0)
        {
            ServerCertificate = new HTTP_UNICODE_STRING(data.Skip(skip).ToArray());
            skip += ServerCertificate.TotalLength;
        }
        if ((FieldsPresent & HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_RESPONSE_FIELD_CONSENT_MSG) != 0)
        {
            ConsentMessage = new HTTP_UNICODE_STRING(data.Skip(skip).ToArray());
            skip += ConsentMessage.TotalLength;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_TUNNEL_RESPONSE"/> class with default values.
    /// </summary>
    public HTTP_TUNNEL_RESPONSE() : base(new HTTP_PACKET_HEADER(HTTP_PACKET_TYPE.PKT_TYPE_TUNNEL_RESPONSE))
    {
    }

    /// <summary>
    /// Converts the response data to a byte array.
    /// </summary>
    /// <returns>A byte array representing the response data.</returns>
    public override ArraySegment<byte> DataToBytes()
    {
        // Set FieldsPresent flags based on the optional fields.
        FieldsPresent = 0;
        if (TunnelId != null)
            FieldsPresent |= HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_RESPONSE_FIELD_TUNNEL_ID;
        if (CapabilityFlags != null)
            FieldsPresent |= HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_RESPONSE_FIELD_CAPS;
        if (Nonce != null && ServerCertificate != null)
            FieldsPresent |= HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_RESPONSE_FIELD_SOH_REQ;
        if (ConsentMessage != null)
            FieldsPresent |= HTTP_TUNNEL_RESPONSE_FIELDS_PRESENT_FLAGS.HTTP_TUNNEL_RESPONSE_FIELD_CONSENT_MSG;

        // Combine the fixed fields into a byte array.
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(ServerVersion),
            .. BitConverter.GetBytes(StatusCode),
            .. BitConverter.GetBytes((ushort)FieldsPresent),
            0,
            0,
        ];

        // Add optional fields based on the FieldsPresent flags.
        if (TunnelId != null)
        {
            bytes.AddRange(BitConverter.GetBytes(TunnelId.Value));
        }
        if (CapabilityFlags != null)
        {
            bytes.AddRange(BitConverter.GetBytes((uint)CapabilityFlags));
        }
        if (ServerCertificate != null && Nonce != null)
        {
            bytes.AddRange(MarshalExtensions.StructToArraySegment(Nonce));
            bytes.AddRange(ServerCertificate.GetBytes());
        }
        if (ConsentMessage != null)
        {
            bytes.AddRange(ConsentMessage.GetBytes());
        }

        return bytes.ToArray();
    }
}
