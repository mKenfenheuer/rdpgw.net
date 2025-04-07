namespace RDPGW;

internal class RDPGWChannelHandler
{
    private readonly IRRDPGWChannelMember _in;
    private readonly IRRDPGWChannelMember _out;

    public RDPGWChannelHandler(IRRDPGWChannelMember @in, IRRDPGWChannelMember @out)
    {
        _in = @in;
        _out = @out;
    }

    internal async Task HandleChannel()
    {
        while(true)
        {
            var packet = await _in.ReadDataPacket();
            await _out.SendDataPacket(packet);
        }
    }
}