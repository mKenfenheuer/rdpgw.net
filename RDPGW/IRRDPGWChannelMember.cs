using RDPGW.Protocol;

namespace RDPGW;

/// <summary>
/// Represents a member of an RDPGW channel that can read and send data packets.
/// </summary>
public interface IRRDPGWChannelMember
{
    /// <summary>
    /// Reads a data packet from the channel.
    /// </summary>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the data packet.</returns>
    Task<HTTP_DATA_PACKET?> ReadDataPacket();

    /// <summary>
    /// Sends a data packet to the channel.
    /// </summary>
    /// <param name="packet">The data packet to send.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    Task SendDataPacket(HTTP_DATA_PACKET packet);
}