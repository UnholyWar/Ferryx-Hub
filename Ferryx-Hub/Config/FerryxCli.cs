using Ferryx_Hub.Config.ConfClass;
using Ferryx_Hub.Helper;
using System.Net.Http;

namespace Ferryx_Hub.Config;

public static class FerryxCli
{
    public static async Task<int> RunAsync(string cmd)
    {
        switch (cmd)
        {
            case "where":
                Console.WriteLine(FerryxPaths.ConfigPath);
                return 0;

            case "reconfig":
                return await RequestRestartAsync();
       
            case "jwt":
                {
                    var cfg = FerryxConfigLoader.LoadOrCreateDefault();
                    var token = JWTHelper.CreateJwtFromKey(cfg.Security.JwtKey);
                    Console.WriteLine(token);
                    return 0;
                }



            default:
                Console.WriteLine("Usage: ferryx where | reconfig");
                return 1;
        }
    }


    private static async Task<int> RequestRestartAsync()
    {
        // ✅ ControlPort config’ten okunur (default 18081)
        var cfg = FerryxConfigLoader.LoadOrCreateDefault();
        var url = FerryxPaths.ControlRestartUrl(cfg.Server.ControlPort);

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var res = await http.PostAsync(url, content: null);

            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine($"Restart failed: HTTP {(int)res.StatusCode}");
                return 2;
            }

            Console.WriteLine("Restart requested.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Restart failed: {ex.Message}");
            Console.WriteLine("Ensure Ferryx is running and restart policy is enabled.");
            return 3;
        }
    }
}
