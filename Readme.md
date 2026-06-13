# RDP Gateway .Net (RDPGW.Net)
![GitHub License](https://img.shields.io/github/license/mkenfenheuer/rdpgw.net)
 ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/mkenfenheuer/rdpgw.net/dotnet.yml) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/mkenfenheuer/rdpgw.net/main) ![NuGet Version](https://img.shields.io/nuget/v/RDPGW) ![NuGet Downloads](https://img.shields.io/nuget/dt/RDPGW) [![codecov](https://codecov.io/github/mKenfenheuer/rdpgw.net/branch/main/graph/badge.svg?token=ZON2D3YG89)](https://codecov.io/github/mKenfenheuer/rdpgw.net)





**RDPGW.Net** is a lightweight and extensible ASP.NET library that brings **Remote Desktop Gateway (RDP Gateway)** functionality directly into your .NET web application. With easy plug-and-play integration, you can securely expose RDP services through your existing web app, with support for custom authentication and authorization flows.


## ✨ Features

- 🖥️ Seamlessly integrate RDP Gateway into any ASP.NET Core app.
- 🔐 Customizable authentication (Basic, Digest, Negotiate/NTLM, and extended PAA cookies).
- 🛡️ Fine-grained resource-based authorization.
- ⚙️ Simple service registration and middleware configuration.


## 🚀 Getting Started

### 1. Add the Library

Add the package to your project:

```bash
dotnet add package RDPGW --version 
```

### 2. Register the Services

In your `Program.cs` or wherever you're building your service container, add:

```csharp
builder.Services.AddRDPGW();
```

#### Optional: Add Custom Handlers for Authentication and/or Authorization

```csharp
builder.Services.AddSingleton<IRDPGWAuthenticationHandler, AuthHandler>();
builder.Services.AddSingleton<IRDPGWAuthorizationHandler, AuthorizationHandler>();
```

### 3. Use the Middleware

Place this as the **first middleware** in the pipeline:

```csharp
var app = builder.Build();

app.UseRDPGW(); // Must be first

// Other middlewares
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

## 🔐 Authentication & Authorization

### Custom Authentication

To authenticate users, implement `IRDPGWAuthenticationHandler`. The interface provides hooks for Basic, Digest, and Negotiate/NTLM methods, plus an optional hook for extended PAA (cookie/token) pre-authentication.

The `auth` argument passed to each hook is the credential portion of the `Authorization` header (everything after the scheme name).

```csharp
public class AuthHandler : IRDPGWAuthenticationHandler
{
    public Task<RDPGWAuthenticationResult> HandleBasicAuth(string auth)
    {
        var challenge = Encoding.UTF8.GetString(Convert.FromBase64String(auth));
        var split = challenge.Split(":");

        var userName = split[0];
        var userPassword = split[1];

        // Your authentication logic here

        return Task.FromResult(RDPGWAuthenticationResult.Success(userName));
    }

    public Task<RDPGWAuthenticationResult> HandleDigestAuth(string auth)
    {
        return Task.FromResult(RDPGWAuthenticationResult.Failed()); // Not used in this example.
    }

    public Task<RDPGWAuthenticationResult> HandleNegotiateAuth(string auth)
    {
        return Task.FromResult(RDPGWAuthenticationResult.Failed()); // Not used in this example.
    }
}
```

Each hook returns an `RDPGWAuthenticationResult`:

- `RDPGWAuthenticationResult.Success(userId)` — authentication succeeded; `userId` is forwarded to the authorization handler.
- `RDPGWAuthenticationResult.Failed()` — authentication failed; the client receives a `401` with a `WWW-Authenticate` challenge.
- `RDPGWAuthenticationResult.Challenge(token)` — used by **challenge-response schemes (Negotiate / NTLM)** that need multiple round trips. The middleware returns the base64 `token` to the client in a scheme-specific `WWW-Authenticate` header (e.g. `Negotiate <token>`), and the client sends the next leg of the handshake on the following request.

> **Note:** An unknown or unsupported authentication scheme is always rejected with `401` — it is never treated as authenticated.

#### Extended (PAA) Authentication

If the client negotiates extended `HTTP_EXTENDED_AUTH_PAA` during the handshake, the tunnel-creation request carries a PAA cookie that is validated via the optional `HandlePAACookieAuth` hook. The default implementation **rejects** the cookie, so PAA is only enabled if you override it:

```csharp
public Task<RDPGWAuthenticationResult> HandlePAACookieAuth(byte[] paaCookie)
{
    // Validate the cookie/token; return Success(userId) to identify the user.
    return Task.FromResult(RDPGWAuthenticationResult.Success("user-from-cookie"));
}
```

A failed PAA validation aborts the tunnel with `E_PROXY_NAP_ACCESSDENIED`.

### Custom Authorization

To control access to specific RDP resources, implement `IRDPGWAuthorizationHandler`:

```csharp
public class AuthorizationHandler : IRDPGWAuthorizationHandler
{
    public Task<bool> HandleUserAuthorization(string userId, string resource)
    {
        // Your authorization logic here
        return Task.FromResult(true); // Allow access
    }
}
```

The `userId` comes from your authentication handler. The `resource` is the identifier of the RDP target being accessed.


## 📞 Support & Contribution

- Found a bug? Want to suggest a feature? Open an [issue](https://github.com/mKenfenheuer/rdpgw.net/issues).
- Contributions welcome via PRs!
- License: GNU GPL v3