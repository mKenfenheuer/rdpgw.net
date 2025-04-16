namespace RDPGW.Protocol;

/// <summary>
/// Represents an HTTP byte blob with a length and data.
/// </summary>
public class HTTP_BYTE_BLOB
{
    /// <summary>
    /// Gets or sets the length of the data in bytes.
    /// </summary>
    public ushort Length { get; set; }

    /// <summary>
    /// Gets or sets the data as a byte array.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Gets the total length of the blob, including the length field.
    /// </summary>
    public int TotalLength => Length + 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_BYTE_BLOB"/> class
    /// from a given byte array segment.
    /// </summary>
    /// <param name="data">The byte array segment containing the blob data.</param>
    public HTTP_BYTE_BLOB(ArraySegment<byte> data)
    {
        // Extract the first 2 bytes as the length.
        Length = BitConverter.ToUInt16(data.Take(2).ToArray(), 0);

        // Extract the remaining bytes as the data.
        Data = data.Skip(2).Take(Length).ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_BYTE_BLOB"/> class
    /// with an empty data array.
    /// </summary>
    public HTTP_BYTE_BLOB()
    {
        // Initialize with an empty data array and zero length.
        Data = Array.Empty<byte>();
        Length = 0;
    }

    /// <summary>
    /// Converts the blob into a byte array segment.
    /// </summary>
    /// <returns>A byte array segment representing the blob.</returns>
    public ArraySegment<byte> GetBytes()
    {
        // Update the length based on the current data size.
        Length = (ushort)Data.Count();

        // Create a list of bytes containing the length and data.
        List<byte> bytes = 
        [
            // Add the length as the first 2 bytes.
            .. BitConverter.GetBytes(Length),

            // Add the data bytes.
            .. Data,
        ];

        // Return the byte array as a segment.
        return bytes.ToArray();
    }
}