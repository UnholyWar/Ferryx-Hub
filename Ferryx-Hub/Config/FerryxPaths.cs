namespace Ferryx_Hub.Config;

public static class FerryxPaths
{
    public static string ConfigDir => "/etc/ferryx";
    public static string ConfigPath => Path.Combine(ConfigDir, "ferryx-hub.json");

    public static string ControlRestartUrl(int controlPort) =>
        $"http://127.0.0.1:{controlPort}/__control/restart";
}
