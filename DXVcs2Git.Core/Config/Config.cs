namespace DXVcs2Git.Core.Configuration {
    public class Config {
        public const string ConfigFileName = "ui.config";
        public TrackRepository[] Repositories { get; set; }
        public string InstallPath { get; set; }
        public string LastVersion { get; set; }
        public int UpdateDelay { get; set; }

        public static Config GenerateDefault() {
            return new Config();
        }
    }
}
