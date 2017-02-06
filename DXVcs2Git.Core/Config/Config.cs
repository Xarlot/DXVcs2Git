using System.IO;
using System.Reflection;

namespace DXVcs2Git.Core.Configuration {
    public class Config {
        public const string ConfigFileName = "ui.config";
        public TrackRepository[] Repositories { get; set; }
        public string InstallPath { get; set; }
        public string LastVersion { get; set; }
        public int UpdateDelay { get; set; }
        public string KeyGesture { get; set; }
        public bool AlwaysSure { get; set; }
        public string DefaultTheme { get; set; }
        public int ScrollBarMode { get; set; }
        public bool SupportsTesting { get; set; }
        public bool TestByDefault { get; set; }

        public static Config GenerateDefault() {
            var result = Validate(new Config());
            ConfigSerializer.SaveConfig(result);
            return result;
        }

        public static Config Validate(Config config) {
            if (config.UpdateDelay == 0)
                config.UpdateDelay = 30;
            if (string.IsNullOrEmpty(config.LastVersion))
                config.LastVersion = VersionInfo.Version.ToString();
            if (string.IsNullOrEmpty(config.InstallPath))
                config.InstallPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return config;                    
        }
    }
}
