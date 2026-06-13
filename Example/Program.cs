using System.Text;
using RDPGW.AspNetCore;
using RDPGW.Extensions;
using RDPGW.Protocol;

namespace Example;


public class Program
{
    public static void Main(string[] args)
    {
        var packet = (HTTP_TUNNEL_RESPONSE)HTTP_PACKET.FromBytes(Convert.FromHexString("050000001200000005000000000000000000"));

        packet.TunnelId = 0x00000001;
        packet.CapabilityFlags = HTTP_CAPABILITY_TYPE.HTTP_CAPABILITY_TYPE_QUAR_SOH;
        packet.Nonce = Guid.NewGuid();
        packet.ServerCertificate = new HTTP_UNICODE_STRING("ServerCertificate");
        packet.ConsentMessage = new HTTP_UNICODE_STRING("ConsentMessage");

        var bytes = Convert.ToHexString(packet.ToBytes()).Replace("-", "");

        var packet2 = (HTTP_TUNNEL_RESPONSE)HTTP_PACKET.FromBytes(packet.ToBytes());



        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddRDPGW();

        builder.Services.AddSingleton<IRDPGWAuthenticationHandler, AuthHandler>();
        builder.Services.AddSingleton<IRDPGWAuthorizationHandler, AuthorizationHandler>();

        var app = builder.Build();

        app.UseRDPGW();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }


        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.Run();
    }
}

public class AuthorizationHandler : IRDPGWAuthorizationHandler
{
    public Task<bool> HandleUserAuthorization(string userId, string resource)
    {
        return Task.FromResult(true);
    }
}

public class AuthHandler : IRDPGWAuthenticationHandler
{
    public Task<RDPGWAuthenticationResult> HandleBasicAuth(string auth)
    {
        var challenge = Encoding.UTF8.GetString(Convert.FromBase64String(auth));
        var split = challenge.Split(":");

        return Task.FromResult(RDPGWAuthenticationResult.Success(split[0]));
    }

    public Task<RDPGWAuthenticationResult> HandleDigestAuth(string auth)
    {
        return Task.FromResult(RDPGWAuthenticationResult.Failed());
    }

    public Task<RDPGWAuthenticationResult> HandleNegotiateAuth(string auth)
    {
        return Task.FromResult(RDPGWAuthenticationResult.Failed());
    }
}
