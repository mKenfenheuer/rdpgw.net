using RDPGW.AspNetCore;

namespace RDPGW.Extensions;

public static class RDPGWExtensions
{
    /// <summary>
    /// Configures the application to use RDPGW by enabling WebSockets and adding the RDPGW middleware.
    /// </summary>
    /// <param name="app">The WebApplication instance to configure.</param>
    public static void UseRDPGW(this WebApplication app)
    {
        app.UseWebSockets(); // Enable WebSocket support
        app.UseMiddleware<RDPGWMiddleware>(); // Add the RDPGW middleware
    }

    /// <summary>
    /// Adds the necessary services for RDPGW to the service collection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    public static void AddRDPGW(this IServiceCollection services)
    {
        // Placeholder for adding RDPGW-related services
        // Example: services.AddSingleton<SomeService>();
    }
}
