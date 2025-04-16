
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using RDPGW.AspNetCore;
using RDPGW.Extensions;
using RDPGW.Protocol;

namespace RDPGW.Test;

[TestClass]
public class AspNetCore_Test
{
     [TestMethod]
    public async Task TestWebServerAuthFail()
    {
        var builder = WebApplication.CreateBuilder([]); ;

        builder.WebHost.UseKestrel(options =>
        {
            options.ListenLocalhost(50000);
        });

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddRDPGW();

        builder.Services.AddSingleton<IRDPGWAuthenticationHandler, AuthHandler>();
        builder.Services.AddSingleton<IRDPGWAuthorizationHandler, SuccessAuthorizationHandler>();

        var app = builder.Build();

        app.UseRDPGW();


        app.UseHttpsRedirection();

        app.UseAuthorization();

        await app.StartAsync();

        await Task.Delay(1000);

        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:50000/");

        Assert.IsFalse(response.IsSuccessStatusCode, "Response was successful. Expected failure.");
        Assert.AreEqual(404, (int)response.StatusCode, "Expected 404 Not Found status code.");

        ClientWebSocket ws = new ClientWebSocket();
        try
        {
            await ws.ConnectAsync(new Uri("ws://localhost:50000/remoteDesktopGateway"), CancellationToken.None);
        }
        catch (Exception e)
        {
            Assert.IsTrue(e.Message.Contains("401"), "Expected 401 Unauthorized error.");
            return;
        }

        await app.StopAsync();
    }

    [TestMethod]
    public async Task TestWebServerNegotiateAuth()
    {
        var builder = WebApplication.CreateBuilder([]); ;

        builder.WebHost.UseKestrel(options =>
        {
            options.ListenLocalhost(50000);
        });

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddRDPGW();

        builder.Services.AddSingleton<IRDPGWAuthenticationHandler, AuthHandler>();
        builder.Services.AddSingleton<IRDPGWAuthorizationHandler, SuccessAuthorizationHandler>();

        var app = builder.Build();

        app.UseRDPGW();


        app.UseHttpsRedirection();

        app.UseAuthorization();

        var appTask = Task.Run(app.Run);

        await Task.Delay(1000);

        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:50000/");

        Assert.IsFalse(response.IsSuccessStatusCode, "Response was successful. Expected failure.");
        Assert.AreEqual(404, (int)response.StatusCode, "Expected 404 Not Found status code.");

        ClientWebSocket ws = new ClientWebSocket();
        try
        {
            ws.Options.SetRequestHeader("Authorization", "Negotiate " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password")));
            await ws.ConnectAsync(new Uri("ws://localhost:50000/remoteDesktopGateway"), CancellationToken.None);
        }
        catch 
        {
            Assert.Fail("Expected WebSocket to be accepted.");
            return;
        }

        Assert.IsTrue(ws.State == WebSocketState.Open, "WebSocket is not open.");

        var packets = JsonConvert.DeserializeObject<TestPacket[]>(File.ReadAllText("packets.json"));
        Assert.IsNotNull(packets, "Failed to deserialize packets.json");


        var clientPackets = packets!.Where(p => p.Type == "client" && p.TypeName != "HTTP_DATA_PACKET").ToArray();

        foreach(var packet in clientPackets)
        {
            Console.WriteLine($"Sending packet: {packet.TypeName}");
            await ws.SendAsync(packet.Data, WebSocketMessageType.Binary, true, CancellationToken.None);
            var buffer = new byte[10240];
            Console.WriteLine($"Waiting for packet: {packet.Expected}");
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.IsTrue(result.MessageType == WebSocketMessageType.Binary, "Expected binary message type.");
            Assert.IsTrue(result.EndOfMessage, "Expected end of message.");
            var data = buffer.Take(result.Count).ToArray();
        
            var packetMessage = HTTP_PACKET.FromBytes(data);
            Assert.IsTrue(packetMessage.GetType().Name == packet.Expected, $"Type mismatch for packet: {packetMessage.GetType().Name} != {packet.Expected}");
        }

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        Assert.IsTrue(ws.State == WebSocketState.Closed, "WebSocket is not closed.");
    }

    [TestMethod]
    public async Task TestWebServerBasicAuth()
    {
        var builder = WebApplication.CreateBuilder([]); ;

        builder.WebHost.UseKestrel(options =>
        {
            options.ListenLocalhost(50000);
        });

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddRDPGW();

        builder.Services.AddSingleton<IRDPGWAuthenticationHandler, AuthHandler>();
        builder.Services.AddSingleton<IRDPGWAuthorizationHandler, SuccessAuthorizationHandler>();

        var app = builder.Build();

        app.UseRDPGW();


        app.UseHttpsRedirection();

        app.UseAuthorization();

        var appTask = Task.Run(app.Run);

        await Task.Delay(1000);

        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:50000/");

        Assert.IsFalse(response.IsSuccessStatusCode, "Response was successful. Expected failure.");
        Assert.AreEqual(404, (int)response.StatusCode, "Expected 404 Not Found status code.");

        ClientWebSocket ws = new ClientWebSocket();
        try
        {
            ws.Options.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password")));
            await ws.ConnectAsync(new Uri("ws://localhost:50000/remoteDesktopGateway"), CancellationToken.None);
        }
        catch 
        {
            Assert.Fail("Expected WebSocket to be accepted.");
            return;
        }

        Assert.IsTrue(ws.State == WebSocketState.Open, "WebSocket is not open.");

        var packets = JsonConvert.DeserializeObject<TestPacket[]>(File.ReadAllText("packets.json"));
        Assert.IsNotNull(packets, "Failed to deserialize packets.json");


        var clientPackets = packets!.Where(p => p.Type == "client" && p.TypeName != "HTTP_DATA_PACKET").ToArray();

        foreach(var packet in clientPackets)
        {
            Console.WriteLine($"Sending packet: {packet.TypeName}");
            await ws.SendAsync(packet.Data, WebSocketMessageType.Binary, true, CancellationToken.None);
            var buffer = new byte[10240];
            Console.WriteLine($"Waiting for packet: {packet.Expected}");
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.IsTrue(result.MessageType == WebSocketMessageType.Binary, "Expected binary message type.");
            Assert.IsTrue(result.EndOfMessage, "Expected end of message.");
            var data = buffer.Take(result.Count).ToArray();
        
            var packetMessage = HTTP_PACKET.FromBytes(data);
            Assert.IsTrue(packetMessage.GetType().Name == packet.Expected, $"Type mismatch for packet: {packetMessage.GetType().Name} != {packet.Expected}");
        }

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        Assert.IsTrue(ws.State == WebSocketState.Closed, "WebSocket is not closed.");
    }

    [TestMethod]
    public async Task TestWebServerDigestAuth()
    {
        var builder = WebApplication.CreateBuilder([]); ;

        builder.WebHost.UseKestrel(options =>
        {
            options.ListenLocalhost(50000);
        });

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddRDPGW();

        builder.Services.AddSingleton<IRDPGWAuthenticationHandler, AuthHandler>();
        builder.Services.AddSingleton<IRDPGWAuthorizationHandler, SuccessAuthorizationHandler>();

        var app = builder.Build();

        app.UseRDPGW();


        app.UseHttpsRedirection();

        app.UseAuthorization();

        var appTask = Task.Run(app.Run);

        await Task.Delay(1000);

        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:50000/");

        Assert.IsFalse(response.IsSuccessStatusCode, "Response was successful. Expected failure.");
        Assert.AreEqual(404, (int)response.StatusCode, "Expected 404 Not Found status code.");

        ClientWebSocket ws = new ClientWebSocket();
        try
        {
            ws.Options.SetRequestHeader("Authorization", "Digest " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password")));
            await ws.ConnectAsync(new Uri("ws://localhost:50000/remoteDesktopGateway"), CancellationToken.None);
        }
        catch 
        {
            Assert.Fail("Expected WebSocket to be accepted.");
            return;
        }

        Assert.IsTrue(ws.State == WebSocketState.Open, "WebSocket is not open.");

        var packets = JsonConvert.DeserializeObject<TestPacket[]>(File.ReadAllText("packets.json"));
        Assert.IsNotNull(packets, "Failed to deserialize packets.json");


        var clientPackets = packets!.Where(p => p.Type == "client" && p.TypeName != "HTTP_DATA_PACKET").ToArray();

        foreach(var packet in clientPackets)
        {
            Console.WriteLine($"Sending packet: {packet.TypeName}");
            await ws.SendAsync(packet.Data, WebSocketMessageType.Binary, true, CancellationToken.None);
            var buffer = new byte[10240];
            Console.WriteLine($"Waiting for packet: {packet.Expected}");
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.IsTrue(result.MessageType == WebSocketMessageType.Binary, "Expected binary message type.");
            Assert.IsTrue(result.EndOfMessage, "Expected end of message.");
            var data = buffer.Take(result.Count).ToArray();
        
            var packetMessage = HTTP_PACKET.FromBytes(data);
            Assert.IsTrue(packetMessage.GetType().Name == packet.Expected, $"Type mismatch for packet: {packetMessage.GetType().Name} != {packet.Expected}");
        }

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        Assert.IsTrue(ws.State == WebSocketState.Closed, "WebSocket is not closed.");
    }

    [TestMethod]
    public async Task TestWebServerResourceAuthFail()
    {
        var builder = WebApplication.CreateBuilder([]); ;

        builder.WebHost.UseKestrel(options =>
        {
            options.ListenLocalhost(50000);
        });

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddRDPGW();

        builder.Services.AddSingleton<IRDPGWAuthenticationHandler, AuthHandler>();
        builder.Services.AddSingleton<IRDPGWAuthorizationHandler, FailAuthorizationHandler>();

        var app = builder.Build();

        app.UseRDPGW();


        app.UseHttpsRedirection();

        app.UseAuthorization();

        var appTask = Task.Run(app.Run);

        await Task.Delay(1000);

        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:50000/");

        Assert.IsFalse(response.IsSuccessStatusCode, "Response was successful. Expected failure.");
        Assert.AreEqual(404, (int)response.StatusCode, "Expected 404 Not Found status code.");

        ClientWebSocket ws = new ClientWebSocket();
        try
        {
            ws.Options.SetRequestHeader("Authorization", "Digest " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password")));
            await ws.ConnectAsync(new Uri("ws://localhost:50000/remoteDesktopGateway"), CancellationToken.None);
        }
        catch 
        {
            Assert.Fail("Expected WebSocket to be accepted.");
            return;
        }

        Assert.IsTrue(ws.State == WebSocketState.Open, "WebSocket is not open.");

        var packets = JsonConvert.DeserializeObject<TestPacket[]>(File.ReadAllText("packets.json"));
        Assert.IsNotNull(packets, "Failed to deserialize packets.json");


        var clientPackets = packets!.Where(p => p.Type == "client" && p.TypeName != "HTTP_DATA_PACKET").ToArray();

        foreach(var packet in clientPackets)
        {
            Console.WriteLine($"Sending packet: {packet.TypeName}");
            await ws.SendAsync(packet.Data, WebSocketMessageType.Binary, true, CancellationToken.None);
            var buffer = new byte[10240];
            Console.WriteLine($"Waiting for packet: {packet.Expected}");
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.IsTrue(result.MessageType == WebSocketMessageType.Binary, "Expected binary message type.");
            Assert.IsTrue(result.EndOfMessage, "Expected end of message.");
            var data = buffer.Take(result.Count).ToArray();
        
            var packetMessage = HTTP_PACKET.FromBytes(data);
            Assert.IsTrue(packetMessage.GetType().Name == packet.Expected, $"Type mismatch for packet: {packetMessage.GetType().Name} != {packet.Expected}");
        }

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        Assert.IsTrue(ws.State == WebSocketState.Closed, "WebSocket is not closed.");
    }
}

public class SuccessAuthorizationHandler : IRDPGWAuthorizationHandler
{
    public Task<bool> HandleUserAuthorization(string userId, string resource)
    {
        return Task.FromResult(true);
    }
}

public class FailAuthorizationHandler : IRDPGWAuthorizationHandler
{
    public Task<bool> HandleUserAuthorization(string userId, string resource)
    {
        return Task.FromResult(false);
    }
}

public class AuthHandler : IRDPGWAuthenticationHandler
{
    public Task<RDPGWAuthenticationResult> HandleBasicAuth(string auth)
    {
        return Task.FromResult(RDPGWAuthenticationResult.Success(auth));
    }

    public Task<RDPGWAuthenticationResult> HandleDigestAuth(string auth)
    {
        return Task.FromResult(RDPGWAuthenticationResult.Success(auth));
    }

    public Task<RDPGWAuthenticationResult> HandleNegotiateAuth(string auth)
    {
        return Task.FromResult(RDPGWAuthenticationResult.Success(auth));
    }
}
