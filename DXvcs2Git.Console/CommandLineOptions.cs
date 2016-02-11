using System;
using CommandLine;

namespace DXVcs2Git.Console {
    public enum WorkMode {
        listener,
        synchronizer,
    }

    public class CommandLineOptions {
        [Option('s', "server", Required = true, HelpText = "GitLab server url")]
        public string Server { get; set; }
        [Option('b', "branch", Required = false, HelpText = "Local git branch name")]
        public string Branch { get; set; }
        [Option('r', "repo", Required = false, HelpText = "Http git repo path")]
        public string Repo { get; set; }
        [Option('c', "commits", Default = 100, HelpText = "Commits count to process")]
        public int CommitsCount { get; set; }
        [Option('l', "login", Required = false, HelpText = "Login to git master account")]
        public string Login { get; set; }
        [Option('p', "password", Required = true, HelpText = "Password")]
        public string Password { get; set; }
        [Option('a', "auth", Required = true, HelpText = "GitLab auth token")]
        public string AuthToken { get; set; }
        [Option('d', "dir", HelpText = "Path to local git repo")]
        public string LocalFolder { get; set; }
        [Option('t', "tracker", Required = false, HelpText = "Path to config with items to track")]
        public string Tracker { get; set; }
        [Option('m', "mode", Required = false, Default = WorkMode.synchronizer, HelpText = "Work mode")]
        public WorkMode WorkMode { get; set; }
        [Option('f', "from", Required = false, HelpText = "Timestamp for history generation")]
        public DateTime From { get; set; }
        [Option('i', "interval", Required = false, Default = 30, HelpText = "Duration in minutes")]
        public int Timeout { get; set; }
    }
}
