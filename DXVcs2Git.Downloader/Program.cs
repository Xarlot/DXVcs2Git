using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace DXVcs2Git.Downloader {
    class Program {
        const string LocalDirectory = "git";
        const string GitPathError = "Error. Specify git path (-git your_git_http_path).";
        static void Main(string[] args) {
            if (args == null || args.Count() < 2) {
                Console.WriteLine(GitPathError);
                Environment.Exit(1);
                return;
            }
            int gitIndex = Array.IndexOf(args, "-git");
            if (gitIndex < 0) {
                Console.WriteLine(GitPathError);
                Environment.Exit(1);
                return;
            }
            int gitPathIndex = gitIndex + 1;
            if (gitPathIndex >= args.Count()) {
                Console.WriteLine(GitPathError);
                Environment.Exit(1);
            }
            string gitPath = args[gitPathIndex];
            string localPath = Path.Combine(Environment.CurrentDirectory, LocalDirectory);
            try {
                Repository.Clone(gitPath, localPath);
            }
            catch (Exception ex) {
                Console.Write(ex.ToString());
                Environment.Exit(1);
            }
            Console.WriteLine($"Cloning git repo {gitPath} to {localPath} was successful.");
            Environment.Exit(0);
        }
    }
}
