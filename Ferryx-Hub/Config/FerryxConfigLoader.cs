using System.Text;

namespace Ferryx_Hub.Config
{
    public class FerryxConfigLoader
    {
        private const string ConfigPath = "/etc/ferryx/ferryx-hub.conf";

        public static FerryxConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                CreateDefaultConfig();
                Console.WriteLine($"[CONFIG] Default config created at {ConfigPath}");
            }

            return Parse(File.ReadAllLines(ConfigPath));
        }

        private static void CreateDefaultConfig()
        {
            Directory.CreateDirectory("/etc/ferryx");

            var sb = new StringBuilder();
            sb.AppendLine("[server]");
            sb.AppendLine("env = prod");
            sb.AppendLine();
            sb.AppendLine("[cors]");
            sb.AppendLine("allowed_origins = *");
            sb.AppendLine();
            sb.AppendLine("[deploy]");
            sb.AppendLine("allowed_services = my-app");
            sb.AppendLine("deployer_group = ferryx-deployers-prod");

            File.WriteAllText(ConfigPath, sb.ToString());
        }

        private static FerryxConfig Parse(string[] lines)
        {
            var config = new FerryxConfig();
            string section = "";

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line[1..^1];
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (section)
                {
                    case "server":
                        if (key == "env") config.Env = value;
                        break;

                    case "cors":
                        if (key == "allowed_origins") config.AllowedOrigins = value;
                        break;

                    case "deploy":
                        if (key == "allowed_services")
                            config.AllowedServices = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (key == "deployer_group")
                            config.DeployerGroup = value;
                        break;
                }
            }

            return config;
        }
    }
}
