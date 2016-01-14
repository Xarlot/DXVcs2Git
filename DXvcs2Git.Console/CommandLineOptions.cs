using System;
using CommandLine;

namespace DXVcs2Git.Console {
    [Flags]
    public enum WorkMode {
        directchanges = 0x1,
        history = 0x2,
        mergerequests = 0x4,
        initialize = 0x8,
        allhistory = directchanges | history | initialize,
        all = allhistory | mergerequests,
    }

    public class CommandLineOptions {
        [Option('s', "server", Required = true, HelpText = "GitLab server url")]
        public string Server { get; set; }
        [Option('b', "branch", Required = true, HelpText = "Local git branch name")]
        public string Branch { get; set; }
        [Option('r', "repo", Required = true, HelpText = "Http git repo path")]
        public string Repo { get; set; }
        [Option('c', "commits", Default = 100, HelpText = "Commits count to process")]
        public int CommitsCount { get; set; }
        [Option('l', "login", Required = true, HelpText = "Login to git master account")]
        public string Login { get; set; }
        [Option('p', "password", Required = true, HelpText = "Password")]
        public string Password { get; set; }
        [Option('a', "auth", Required = true, HelpText = "GitLab auth token")]
        public string AuthToken { get; set; }
        [Option('d', "dir", HelpText = "Path to local git repo")]
        public string LocalFolder { get; set; }
        [Option('t', "tracker", Required = true, HelpText = "Path to config with items to track")]
        public string Tracker { get; set; }
        [Option('m', "mode", Required = false, Default = WorkMode.history, HelpText = "Work mode")]
        public WorkMode WorkMode { get; set; }
        [Option('f', "from", Required = false, HelpText = "Timestamp for history generation")]
        public DateTime From { get; set; }
        [Option('o', "fork-mode", Required = false, HelpText = "Fork mode - Feature branch will be removed from upstream after accepting merge request")]
        public bool ForkMode { get; set; }
    }
}
