using CommandLine;

namespace DXVcs2Git.FarmIntegrator {
    public class Options {
        [Option('s', "server", Required = false, HelpText = "Aux path", Default = @"tcp://ccnet.devexpress.devx:21234/CruiseManager.rem")]
        public string AuxPath { get; }
        [Option('t', "taskname", Required = true, HelpText = "Task name")]
        public string TaskName { get; }
        [Option('f', "forcer", Required = false, HelpText = "Forcer", Default = @"DXVcs2Git.FarmIntegrator")]
        public string Forcer { get; }
        
        public Options(string auxPath, string taskName, string forcer) {
            AuxPath = auxPath;
            TaskName = taskName;
            Forcer = forcer;
        }
    }
}
