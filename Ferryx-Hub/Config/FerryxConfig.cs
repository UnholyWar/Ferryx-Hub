namespace Ferryx_Hub.Config
{
    public class FerryxConfig
    {
        public string Env { get; set; } = "prod";
        public string AllowedOrigins { get; set; } = "*";
        public string[] AllowedServices { get; set; } = Array.Empty<string>();
        public string DeployerGroup { get; set; } = "ferryx-deployers-prod";
    }
}
