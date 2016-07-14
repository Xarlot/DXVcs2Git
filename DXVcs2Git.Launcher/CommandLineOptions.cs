using CommandLine;

namespace DXVcs2Git.Launcher {
    class CommandLineOptions {
        [Option('w', "wait", Default = 0, HelpText = "Wait for exit")]
        public int WaitForExit { get; set; }
    }
}
