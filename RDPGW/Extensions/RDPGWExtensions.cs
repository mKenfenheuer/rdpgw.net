using RDPGW.AspNetCore;

namespace RDPGW.Extensions;

public static class RDPGWExtensions
{
    public static void UseRDPGW(this WebApplication app)
    {
        app.UseWebSockets();
        app.UseMiddleware<RDPGWMiddleware>();
    }

    public static void AddRDPGW(this IServiceCollection services)
    {
        
    }
}
