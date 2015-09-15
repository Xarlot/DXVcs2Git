using CommandLine;

namespace DXVcs2Git.Console {
    public class CommandLineOptions {
        [Option('b', "branch", Required = true, HelpText = "Local git branch name")]
        public string Branch { get; set; }
        [Option('r', "repo", Required = true, HelpText = " Http git repo path")]
        public string Repo { get; set; }
        [Option('c', "commits", Default = 100, HelpText = "Commits count to process")]
        public int CommitsCount { get; set; }
        [Option('l', "login", Required = true, HelpText = "Login to git master account")]
        public string Login { get; set; }
        [Option('p', "password", Required = true, HelpText = "Password to git master account")]
        public string Password { get; set; }
        [Option('d', "dir", HelpText = "Path to local git repo")]
        public string LocalFolder { get; set; }
        [Option('t', "tracker", Required = true, HelpText = "Path to config with items to track")]
        public string Tracker { get; set; }
    }
}
