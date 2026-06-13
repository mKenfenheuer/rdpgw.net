namespace RDPGW;

/// <summary>
/// Handles the communication between two channel members.
/// </summary>
public class RDPGWChannelHandler
{
    private readonly IRRDPGWChannelMember _in;
    private readonly IRRDPGWChannelMember _out;

    /// <summary>
    /// Initializes a new instance of the <see cref="RDPGWChannelHandler"/> class.
    /// </summary>
    /// <param name="in">The input channel member.</param>
    /// <param name="out">The output channel member.</param>
    public RDPGWChannelHandler(IRRDPGWChannelMember @in, IRRDPGWChannelMember @out)
    {
        _in = @in;
        _out = @out;
    }

    /// <summary>
    /// Continuously handles data packets from the input channel and sends them to the output channel.
    /// </summary>
    public async Task HandleChannel()
    {
        while (true)
        {
            try
            {
                // Read a data packet from the input channel.
                var packet = await _in.ReadDataPacket();
                if(packet == null)
                {
                    break;
                }

                // Send the data packet to the output channel.
                await _out.SendDataPacket(packet);
            }
            catch
            {
                break;
            }
        }
    }
}