using System;
using System.IO;
using CommandLine;
using DXVcs2Git.Core.Git;

namespace DXVcs2Git.UI.ViewModels {
    public class Options {
        [Option('s', "source", HelpText = "Source git branch name")]
        public string SourceBranch { get; set; }
        [Option('t', "target", HelpText = "Target git branch name")]
        public string TargetBranch { get; set; }
        [Option('d', "dir", HelpText = "Path to local git repo")]
        public string LocalFolder { get; set; }
        [Option('p', "password", Required = true, HelpText = "Token for gitlab cli")]
        public string Token { get; set; }

        public const string GitServer = @"http://litvinov-lnx";
        public string DetectRepo() {
            if (string.IsNullOrEmpty(LocalFolder))
                return null;
            GitReaderWrapper reader = new GitReaderWrapper(Path.Combine(LocalFolder, ".git"));
            return reader.GetRemoteRepoPath();
        }
    }
}
