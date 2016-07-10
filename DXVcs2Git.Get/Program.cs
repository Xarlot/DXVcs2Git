using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using DXVcs2Git.Core;
using DXVcs2Git.Git;

namespace DXVcs2Git.Get {
    class Program {
        static void Main(string[] args) {
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            var exitCode = result.MapResult(clo => {
                try {
                    return DoWork(clo);
                }
                catch (Exception ex) {
                    Log.Error("Application crashed with exception", ex);
                    return 1;
                }
            },
            errors => 1);
            Environment.Exit(exitCode);
        }

        static int DoWork(CommandLineOptions clo) {
            GitLabWrapper gitLabWrapper = new GitLabWrapper(clo.Server, clo.Token);
            var project = gitLabWrapper.FindProject(clo.Repo);
            if (project == null) {
                Log.Error($"Can`t find project {clo.Repo}");
                return 1;
            }
            gitLabWrapper.GetArchive(project, clo.Sha);
            return 0;
        }
    }
}
