namespace DXVcs2Git.UI.ViewModels {
    public class Config {
        public const string GitServer = @"http:\\litvinov-lnx";
        public const string ConfigFileName = "ui.config";
        public TrackRepository[] Repositories { get; set; }
        public string Token { get; set; }
        
        public static Config GenerateDefault() {
            return new Config();
        }
    }
}
