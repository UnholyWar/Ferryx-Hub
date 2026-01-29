using Ferryx_Hub.Config.ConfClass;
using System.Text.Json;

namespace Ferryx_Hub.Config;

public static class FerryxConfigLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static FerryxConfig Load()
    {
        if (!File.Exists(FerryxPaths.ConfigPath))
        {
            var cfg = Default();
            Write(cfg, overwrite: true);
            Console.WriteLine($"[CONFIG] Default config created at {FerryxPaths.ConfigPath}");
            return cfg;
        }

        var json = File.ReadAllText(FerryxPaths.ConfigPath);
        var cfgLoaded = JsonSerializer.Deserialize<FerryxConfig>(json, JsonOpts);

        if (cfgLoaded is null)
            throw new InvalidOperationException($"[CONFIG] Invalid JSON at {FerryxPaths.ConfigPath}");

        Normalize(cfgLoaded);
        Validate(cfgLoaded);

        return cfgLoaded;
    }

    public static FerryxConfig LoadOrCreateDefault() => Load();

    // CLI: ferryx reconfig
    public static void Reconfig()
    {
        Directory.CreateDirectory(FerryxPaths.ConfigDir);

        if (File.Exists(FerryxPaths.ConfigPath))
        {
            var backup = FerryxPaths.ConfigPath + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak";
            File.Copy(FerryxPaths.ConfigPath, backup, overwrite: true);
            Console.WriteLine($"[CONFIG] Backup created: {backup}");
        }

        var cfg = Default();
        Write(cfg, overwrite: true);
        Console.WriteLine($"[CONFIG] Reconfigured: {FerryxPaths.ConfigPath}");
    }

    private static FerryxConfig Default()
    {
        return new FerryxConfig
        {
            Server = new ServerConfig
            {
                Env = "prod",
                Bind = "0.0.0.0",
                Port = 18080,
                ControlPort = 18081
            },
            Cors = new CorsConfig
            {
                AllowedOrigins = new[] { "*" }
            },
            Services = new Dictionary<string, ServiceConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["my-repo"] = new ServiceConfig
                {
                    Groups = new[] { "deployers-prod", "ops" }
                }
            }
        };
    }

    private static void Write(FerryxConfig cfg, bool overwrite)
    {
        Directory.CreateDirectory(FerryxPaths.ConfigDir);

        if (!overwrite && File.Exists(FerryxPaths.ConfigPath))
            return;

        var json = JsonSerializer.Serialize(cfg, JsonOpts);
        File.WriteAllText(FerryxPaths.ConfigPath, json);
    }

    private static void Normalize(FerryxConfig cfg)
    {
        cfg.Server.Bind = string.IsNullOrWhiteSpace(cfg.Server.Bind) ? "0.0.0.0" : cfg.Server.Bind;

        // AllowedOrigins null ise "*"
        cfg.Cors.AllowedOrigins ??= new[] { "*" };

        // Services null ise boş sözlük
        cfg.Services ??= new Dictionary<string, ServiceConfig>(StringComparer.OrdinalIgnoreCase);

        // Her service için null groups -> empty
        foreach (var k in cfg.Services.Keys.ToList())
        {
            cfg.Services[k] ??= new ServiceConfig();
            cfg.Services[k].Groups ??= Array.Empty<string>();
        }
    }

    private static void Validate(FerryxConfig cfg)
    {
        if (cfg.Server.Port is < 1 or > 65535)
            throw new InvalidOperationException("[CONFIG] server.port must be 1..65535");

        if (cfg.Server.ControlPort is < 1 or > 65535)
            throw new InvalidOperationException("[CONFIG] server.controlPort must be 1..65535");

        if (cfg.Server.Port == cfg.Server.ControlPort)
            throw new InvalidOperationException("[CONFIG] server.port and server.controlPort must be different");
    }
}
