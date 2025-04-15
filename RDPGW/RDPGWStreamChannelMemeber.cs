using RDPGW.Protocol;

namespace RDPGW;

/// <summary>
/// Represents a channel member that communicates over a stream.
/// </summary>
public class RDPGWStreamChannelMemeber : IRRDPGWChannelMember {
    private readonly Stream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="RDPGWStreamChannelMemeber"/> class.
    /// </summary>
    /// <param name="stream">The stream used for communication.</param>
    public RDPGWStreamChannelMemeber(Stream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Reads a data packet from the stream.
    /// </summary>
    /// <returns>A <see cref="HTTP_DATA_PACKET"/> containing the data read from the stream.</returns>
    /// <exception cref="Exception">Thrown when no data is read from the stream.</exception>
    public async Task<HTTP_DATA_PACKET> ReadDataPacket()
    {
        // Allocate a buffer to read data from the stream.
        byte[] buffer = new byte[8 * 1024];
        var len = await _stream.ReadAsync(buffer, CancellationToken.None);

        // Check if any data was read.
        if (len > 0)
        {
            // Return the data as an HTTP_DATA_PACKET.
            return new HTTP_DATA_PACKET(buffer.Take(len).ToArray());
        }

        // Throw an exception if no data was read.
        throw new Exception("Read zero bytes from stream!");
    }

    /// <summary>
    /// Sends a data packet to the stream.
    /// </summary>
    /// <param name="packet">The data packet to send.</param>
    public async Task SendDataPacket(HTTP_DATA_PACKET packet)
    {
        // Write the packet data to the stream.
        await _stream.WriteAsync(packet.Data.Data, CancellationToken.None);

        // Ensure the data is flushed to the stream.
        await _stream.FlushAsync();
    }
}
