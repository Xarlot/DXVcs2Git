namespace DXVcs2Git.GitTools.ViewModels {
    public class Options {
        public string Token { get; set; }

        public static Options GenerateDefault() {
            return new Options();
        }
    }
}
