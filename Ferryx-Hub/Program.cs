using Ferryx_Hub.Config;
using Ferryx_Hub.Config.ConfClass;
using Microsoft.AspNetCore.SignalR;
using System.Net;

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


// Controllers (template'den)
builder.Services.AddControllers();

// ✅ SignalR
builder.Services.AddSignalR();

// ✅ CORS (default *)
// ✅ CORS (JSON: cors.allowedOrigins[])
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        var origins = ferryxConfig.Cors.AllowedOrigins ?? new[] { "*" };

        if (origins.Length == 1 && origins[0] == "*")
        {
            // Credentials yoksa AllowAnyOrigin kullanılabilir
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(origins);
        }

        policy
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

// HTTP pipeline
app.UseHttpsRedirection();

// ✅ CORS middleware: Map'lerden önce
app.UseCors("default");

// Şimdilik auth yok ama kalsın (ileride JWT ekleyince kullanacağız)
app.UseAuthorization();

app.MapControllers();

// ✅ Health check
app.MapGet("/", () => "Ferryx Hub is running 🚢");

// ✅ SignalR Hub route
app.MapHub<DeployHub>("/hubs/deploy");

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
    if (!cfg.Services.TryGetValue(req.Service, out var svc))
        return Results.BadRequest("Service not allowed");

    Console.WriteLine($"[DEPLOY] {req.Service}:{req.Tag} ({req.Env})");

    foreach (var group in svc.Groups)
        await hub.Clients.Group(group).SendAsync("NewDeploy", req);

    return Results.Ok(new { status = "published", groups = svc.Groups });
});


// ✅ top-level dönüş hatası olmaması için RunAsync kullan
await app.RunAsync();
return 0;


// ===== Types =====
record DeployRequest(string Env, string Service, string Tag);

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
