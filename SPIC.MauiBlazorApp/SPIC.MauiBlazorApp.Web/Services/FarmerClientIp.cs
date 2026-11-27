using Microsoft.AspNetCore.Components.Server.Circuits;
using SPIC.Core.FarmerPortal;

namespace SPIC.MauiBlazorApp.Web.Services;

// Farmer portal (docs/farmer-portal.md, Task 1 fix round 1). The web host is Blazor Server, so every
// web farmer's API call leaves from this container; without help the API's per-IP OTP limit would be
// one bucket for the whole web portal. These three pieces pass the browser's IP to the API on the OTP
// request, proven by the shared secret FarmerPortal:TrustedClientKey (the API ignores the IP header
// without the exact key).

/// <summary>The browser IP of this circuit (scoped = one per circuit).</summary>
public sealed class BrowserClientIp
{
    public string? Ip { get; set; }
}

/// <summary>
/// Captures the browser IP when the circuit connects: the hub connection's HttpContext, read the
/// same way the API reads its callers (last X-Forwarded-For entry from the ingress, else the socket).
/// </summary>
public sealed class BrowserClientIpCircuitHandler : CircuitHandler
{
    private readonly BrowserClientIp _state;
    private readonly IHttpContextAccessor _http;

    public BrowserClientIpCircuitHandler(BrowserClientIp state, IHttpContextAccessor http)
    {
        _state = state;
        _http = http;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var ctx = _http.HttpContext;
        if (ctx is not null)
        {
            var ip = FarmerOtpRules.ClientIp(ctx.Request.Headers["X-Forwarded-For"].ToString(),
                ctx.Connection.RemoteIpAddress?.ToString());
            if (ip is not null) _state.Ip = ip;
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Adds X-Spic-Client-Ip + X-Spic-Client-Key to api/FarmerAuth/otp/request only, and only when the
/// key is configured and the browser IP is known. Every other API call is untouched.
/// </summary>
public sealed class FarmerClientIpHandler : DelegatingHandler
{
    private readonly BrowserClientIp _state;
    private readonly string? _key;

    public FarmerClientIpHandler(BrowserClientIp state, string? key)
    {
        _state = state;
        _key = key;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (!string.IsNullOrEmpty(_key) && !string.IsNullOrEmpty(_state.Ip)
            && path.EndsWith("/api/FarmerAuth/otp/request", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Remove(FarmerOtpRules.ClientIpHeader);
            request.Headers.Remove(FarmerOtpRules.ClientKeyHeader);
            request.Headers.TryAddWithoutValidation(FarmerOtpRules.ClientIpHeader, _state.Ip);
            request.Headers.TryAddWithoutValidation(FarmerOtpRules.ClientKeyHeader, _key);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
