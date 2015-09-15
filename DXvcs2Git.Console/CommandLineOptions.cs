using CommandLine;

namespace DXVcs2Git.Console {
    public class CommandLineOptions {
        [Option('b', "branch", Required = true, HelpText = "Local git branch name")]
        public string Branch { get; set; }
        [Option('r', "repo", Required = true, HelpText = " Http git repo path")]
        public string Repo { get; set; }
        [Option('c', "commitscount", Default = 100, HelpText = "Commits count to process")]
        public int? CommitsCount { get; set; }
    }
}
