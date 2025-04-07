using RDPGW.Protocol;

namespace RDPGW;

internal class RDPGWStreamChannelMemeber : IRRDPGWChannelMember {
    private readonly Stream _stream;

    public RDPGWStreamChannelMemeber(Stream stream)
    {
        _stream = stream;
    }


    public async Task<HTTP_DATA_PACKET> ReadDataPacket()
    {
        byte[] buffer = new byte[8*1024];
        var len = await _stream.ReadAsync(buffer, CancellationToken.None);
        if(len > 0)
        {
            return new HTTP_DATA_PACKET(buffer.Take(len).ToArray());
        }

        throw new Exception("Read zero bytes from stream!");
    }

    public async Task SendDataPacket(HTTP_DATA_PACKET packet)
    {
        await _stream.WriteAsync(packet.Data.Data, CancellationToken.None);
        await _stream.FlushAsync();
    }
}
