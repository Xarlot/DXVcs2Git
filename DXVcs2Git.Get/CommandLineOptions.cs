using CommandLine;

namespace DXVcs2Git.Get {
    public class CommandLineOptions {
        [Option('s', "server", Required = true, HelpText = "GitLab server url")]
        public string Server { get; set; }
        [Option('r', "repo", Required = true, HelpText = "Http git repo path")]
        public string Repo { get; set; }
        [Option('a', "auth", Required = true, HelpText = "GitLab auth token")]
        public string AuthToken { get; set; }
        [Option("sha", Required = true, HelpText = "The commit SHA to download")]
        public string Sha { get; set; }
    }
}
