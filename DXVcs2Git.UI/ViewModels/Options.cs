using System;
using CommandLine;

namespace DXVcs2Git.UI.ViewModels {
    public class Options {
        [Option('b', "branch", Required = true, HelpText = "Local git branch name")]
        public string Branch { get; set; }
        [Option('r', "repo", Required = true, HelpText = "Http git repo path")]
        public string Repo { get; set; }
        [Option('d', "dir", HelpText = "Path to local git repo")]
        public string LocalFolder { get; set; }
        [Option('t', "token", Required = true, HelpText = "Token for gitlab cli")]
        public string Token { get; set; }

        public const string gitServer = @"http://litvinov-lnx";
    }
}
