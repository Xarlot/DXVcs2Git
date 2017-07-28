using System;
using CommandLine;
using ThoughtWorks.CruiseControl.Remote;

namespace DXVcs2Git.FarmIntegrator {
    class Program {
        static void Main(string[] args) {
            var result = Parser.Default.ParseArguments<Options>(args);
            try {
                var exitCode = result.MapResult(
                    (Options options) => DoWork(options),
                    err => DoErrors(args));
                Environment.Exit(exitCode);
            }
            catch (Exception ex) {
                Environment.Exit(1);
            }
        }
        static int DoErrors(string[] args) {
            return 1;
        }
        static int DoWork(Options options) {
            string auxPath = options.AuxPath;
            string forcer = options.Forcer;
            string taskname = options.TaskName;
            
            RemoteCruiseManagerFactory f = new RemoteCruiseManagerFactory();
            ICruiseManager m = f.GetCruiseManager(auxPath);
            m.ForceBuild(taskname, forcer);
            return 0;
        }
    }
}
