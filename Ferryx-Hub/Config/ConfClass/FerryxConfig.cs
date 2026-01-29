namespace Ferryx_Hub.Config.ConfClass
{
    public class FerryxConfig
    {
        public ServerConfig Server { get; set; } = new();
        public CorsConfig Cors { get; set; } = new();
        public Dictionary<string, ServiceConfig> Services { get; set; } = new();
    }

}
