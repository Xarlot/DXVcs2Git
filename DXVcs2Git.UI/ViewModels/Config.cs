namespace DXVcs2Git.UI.ViewModels {
    public class Config {
        public const string ConfigFileName = "ui.config";
        public TrackRepository[] TrackRepositories { get; set; }
        public static Config GenerateDefault() {
            return new Config();
        }
    }
}
