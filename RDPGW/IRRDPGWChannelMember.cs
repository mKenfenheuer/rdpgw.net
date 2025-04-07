using RDPGW.Protocol;

namespace RDPGW;

internal interface IRRDPGWChannelMember
{
    Task<HTTP_DATA_PACKET> ReadDataPacket();
    Task SendDataPacket(HTTP_DATA_PACKET packet);
}