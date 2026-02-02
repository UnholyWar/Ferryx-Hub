using Ferryx_Hub.Config.ConfClass;
using System.Security.Cryptography;
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
            Console.WriteLine($"[token] {cfg.Security.JwtKey}");
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



    private static string GenerateKey()
    {
        // 32 byte = 256-bit
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
    private static FerryxConfig Default()
    {
        return new FerryxConfig
        {
            Security=new SecurityConfig
            {
                JwtKey=GenerateKey()
            },
            Server = new ServerConfig
            {
                Env = "hub",
                Bind = "0.0.0.0",
                Port = 18080,
                ControlPort = 18081
            },
            Services = new Dictionary<string, ServiceConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["my-app"] = new ServiceConfig
                {
                    Groups = new[] {"pre-prod" ,"prod"}
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
