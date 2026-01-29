using Ferryx_Hub.Config;
using Ferryx_Hub.Config.ConfClass;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;

// ✅ 1) CLI komutlarını en başta yakala
// Usage: ferryx where | ferryx reconfig | ferryx restart
var cmd = (args.FirstOrDefault() ?? "serve").ToLowerInvariant();
if (cmd is "where" or "reconfig" or "restart")
{
    var code = await FerryxCli.RunAsync(cmd);
    return code;
}

// ✅ 2) Serve tarafı: CLI/env url override istemiyorsan args'ı boşlayalım
var options = new WebApplicationOptions { Args = Array.Empty<string>() };
var builder = WebApplication.CreateBuilder(options);

// 🚢 Ferryx config load (yoksa otomatik oluştur)
var ferryxConfig = FerryxConfigLoader.LoadOrCreateDefault();
builder.Services.AddSingleton(ferryxConfig);

// ✅ Sadece conf'tan port/bind
builder.WebHost.UseUrls($"http://{ferryxConfig.Server.Bind}:{ferryxConfig.Server.Port}");
var jwtKey = ferryxConfig.Security.JwtKey; // conf'tan


builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o =>
  {
      o.RequireHttpsMetadata = false;

      o.Events = new JwtBearerEvents
      {
          OnMessageReceived = context =>
          {
              // SignalR WebSocket/SSE için token'ı query'den al
              var accessToken = context.Request.Query["access_token"];
              var path = context.HttpContext.Request.Path;

              if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/deploy"))
                  context.Token = accessToken;

              return Task.CompletedTask;
          }
      };

      o.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = false,
          ValidateAudience = false,
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
          ValidateLifetime = false
      };
  });

builder.Services.AddAuthorization();

// Controllers (template'den)
builder.Services.AddControllers();

// ✅ SignalR
builder.Services.AddSignalR();



var app = builder.Build();

// HTTP pipeline
app.UseHttpsRedirection();



app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ✅ Health check
app.MapGet("/", () => "Ferryx Hub is running 🚢");

// ✅ SignalR Hub route
app.MapHub<DeployHub>("/hubs/deploy").RequireAuthorization(); 

// ✅ Localhost-only control endpoint: restart
// CLI "restart/reconfig" bunu çağırır, uygulama kapanır, supervisor restart eder (systemd/docker restart policy)
app.MapPost("/__control/restart", (IHostApplicationLifetime lifetime, HttpContext ctx) =>
{
    var ip = ctx.Connection.RemoteIpAddress;
    if (ip is null || !IPAddress.IsLoopback(ip))
        return Results.Unauthorized();

    Console.WriteLine("[CTRL] Restart requested");
    lifetime.StopApplication();
    return Results.Ok(new { ok = true });
});

// ✅ Jenkins'in çağıracağı deploy endpoint (şimdilik dummy)
app.MapPost("/api/deploy", async (
    DeployRequest req,
    FerryxConfig cfg,
    IHubContext<DeployHub> hub) =>
{
    if (!cfg.Services.TryGetValue(req.Target, out var svc))
        return Results.BadRequest("Service not allowed");

    Console.WriteLine($"[DEPLOY] {req.Target}:{req.Tag} ({req.Env})");

    foreach (var group in svc.Groups)
        await hub.Clients.Group(group).SendAsync("NewDeploy", req);

    return Results.Ok(new { status = "published", groups = svc.Groups });
}).RequireAuthorization(); 


// ✅ top-level dönüş hatası olmaması için RunAsync kullan
await app.RunAsync();
return 0;


// ===== Types =====
public sealed class DeployRequest
{
    public string Env { get; init; } = "";
    public string Target { get; init; } = ""; // repo / app / component

    public string? Tag { get; init; }

    public Dictionary<string, object>? Meta { get; init; }
}


class DeployHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"[HUB] Connected: {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[HUB] Disconnected: {Context.ConnectionId} | {exception?.Message}");
        return base.OnDisconnectedAsync(exception);
    }
}
