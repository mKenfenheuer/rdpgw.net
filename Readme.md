# RDP Gateway .Net (RDPGW.Net)
This library provides plug in RDP Gateway gateway functionality for your application. 

## Getting Started

Add the library to your project as a reference (Nuget is not yet available)

Add the services to the service catalogue like below:

```csharp
builder.Services.AddRDPGW();

//Optional custom handlers for authentication and authorization. See below section "Authentication & Authorization"
builder.Services.AddSingleton<IRDPGWAuthenticationHandler, AuthHandler>();
builder.Services.AddSingleton<IRDPGWAuthorizationHandler, AuthorizationHandler>();
```

Add the middleware to the application as first item like below:


```csharp
var app = builder.Build();

app.UseRDPGW();

...
```

## Authentication and Authorization

To authenticate users, create a custom authentication handler which implements the `IRDPGWAuthenticationHandler` interface and add it to the service catalogue as singleton.

```csharp
builder.Services.AddSingleton<IRDPGWAuthenticationHandler, AuthHandler>();
```

```csharp
/* 
 * Perform authentication using the HTTP Authorization header value provided using the methods Basic, Digest and Negotiate.
 * Return a RDPGWAuthenticationResult based on the success/failure. If succeeded you can provide a unique userId string 
 * to be used for identifying the authenticated user during authorization checks.
 */
public class AuthHandler : IRDPGWAuthenticationHandler
{
    public Task<RDPGWAuthenticationResult> HandleBasicAuth(string auth)
    {
        var challenge = Encoding.UTF8.GetString(Convert.FromBase64String(auth));
        var split = challenge.Split(":");

        var userName = split[0];
        var userPassword = split[1];

        //Your custom autentication handling code
        
        return Task.FromResult(RDPGWAuthenticationResult.Success(userName));
    }

    public Task<RDPGWAuthenticationResult> HandleDigestAuth(string auth)
    {
        throw new NotImplementedException();
    }

    public Task<RDPGWAuthenticationResult> HandleNegotiateAuth(string auth)
    {
        throw new NotImplementedException();
    }
}
```

To authorize users for a specific resource rather than any requested resource, create a custom authorization handler which implements the `IRDPGWAuthorizationHandler` interface and add it to the service catalogue as singleton.

```csharp
builder.Services.AddSingleton<IRDPGWAuthorizationHandler, AuthorizationHandler>();
```

```csharp
public class AuthorizationHandler : IRDPGWAuthorizationHandler
{
    /* 
     * Perform Authorization checks against the resource requested. Return true for passed checks, false for failed checks.
     * The userId represents the result userId provided from your custom authentication handler.
     */
    public Task<bool> HandleUserAuthorization(string userId, string resource)
    {
        //Your custom authorization handling code
        return Task.FromResult(true);
    }
}
```