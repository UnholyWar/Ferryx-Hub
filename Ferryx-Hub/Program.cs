using Ferryx_Hub.Config;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
// 🚢 Ferryx config load
var ferryxConfig = FerryxConfigLoader.Load();
builder.Services.AddSingleton(ferryxConfig);

// Controllers (template'den)
builder.Services.AddControllers();

// ✅ SignalR
builder.Services.AddSignalR();

// ✅ CORS (default *)
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        if (ferryxConfig.AllowedOrigins == "*")
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(ferryxConfig.AllowedOrigins.Split(','));
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

// ✅ Jenkins'in çağıracağı deploy endpoint (şimdilik dummy)
app.MapPost("/api/deploy", async (
    DeployRequest req,
    FerryxConfig cfg,
    IHubContext<DeployHub> hub) =>
{
    if (!cfg.AllowedServices.Contains(req.Service))
        return Results.BadRequest("Service not allowed");

    Console.WriteLine($"[DEPLOY] {req.Service}:{req.Tag} ({req.Env})");

    await hub.Clients.Group(cfg.DeployerGroup)
        .SendAsync("NewDeploy", req);

    return Results.Ok(new { status = "published" });
});


app.Run();


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
