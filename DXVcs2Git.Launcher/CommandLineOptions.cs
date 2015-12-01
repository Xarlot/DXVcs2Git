using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Launcher {
    public class CommandLineOptions {
        [Option('w', "wait", Default = 0, HelpText = "Wait for exit")]
        public int WaitForExit { get; set; }
    }
}
