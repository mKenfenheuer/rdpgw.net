using System.Text;

namespace RDPGW.Protocol;

/// <summary>
/// Represents a Unicode string used in HTTP communication.
/// </summary>
public class HTTP_UNICODE_STRING
{
    /// <summary>
    /// Gets or sets the length of the string in bytes.
    /// </summary>
    public ushort Length { get; set; }

    /// <summary>
    /// Gets or sets the string value.
    /// </summary>
    public string String { get; set; }

    /// <summary>
    /// Gets the total length of the structure, including the length field.
    /// </summary>
    public int TotalLength => Length + 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_UNICODE_STRING"/> class from a byte array.
    /// </summary>
    /// <param name="data">The byte array containing the Unicode string.</param>
    public HTTP_UNICODE_STRING(ArraySegment<byte> data)
    {
        // Extract the length of the string.
        Length = BitConverter.ToUInt16(data.Take(2).ToArray(), 0);

        // Decode the string using Unicode encoding.
        String = Encoding.Unicode.GetString(data.Skip(2).Take(Length).ToArray());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HTTP_UNICODE_STRING"/> class from a string.
    /// </summary>
    /// <param name="str">The string value.</param>
    public HTTP_UNICODE_STRING(string str)
    {
        String = str;

        // Calculate the byte count of the string in Unicode encoding.
        Length = (ushort)Encoding.Unicode.GetByteCount(str);
    }

    /// <summary>
    /// Converts the Unicode string to a byte array.
    /// </summary>
    /// <returns>A byte array representing the Unicode string.</returns>
    public ArraySegment<byte> GetBytes()
    {
        // Calculate the byte count of the string in Unicode encoding.
        Length = (ushort)Encoding.Unicode.GetByteCount(String);
        // Combine the length and string bytes into a single array.
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(Length),
            .. Encoding.Unicode.GetBytes(String),
        ];
        return bytes.ToArray();
    }
}
