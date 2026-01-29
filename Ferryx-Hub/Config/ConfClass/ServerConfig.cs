namespace Ferryx_Hub.Config.ConfClass
{
    public class ServerConfig
    {
        public string Env { get; set; } = "prod";
        public string Bind { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 18080;
        public int ControlPort { get; set; } = 18081;
    }
}
