using CommandLine;
using DXVcs2Git.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Launcher {
    enum ExitCodes : int {
        Success = 0,
        WaitTimeout = 1,
        UIFileNotExist = 2
    }
    class Program {
        static int Main(string[] args) {            
            const string UIProcessName = "DXVcs2Git.UI";
            const string UIExecutableName = UIProcessName + ".exe";
            var options = Parser.Default.ParseArguments<CommandLineOptions>(args);
            var delay = options.MapResult(x => x.WaitForExit, x => 0);
            do {
                if (delay == 0)
                    break;
                Process uiProcess = Process.GetProcessesByName(UIProcessName).FirstOrDefault();
                if (uiProcess == null)
                    break;
                if (!uiProcess.WaitForExit(delay))
                    return (int)ExitCodes.WaitTimeout;
            } while (false);
            var installPath = ConfigSerializer.GetConfig().InstallPath;
            var uiFileFullName = Path.Combine(installPath, UIExecutableName);
            if (!File.Exists(uiFileFullName))
                return (int)ExitCodes.UIFileNotExist;
            string str = "";
            foreach (var arg in args)
                str += arg + " ";
            Process.Start(new ProcessStartInfo(uiFileFullName) { UseShellExecute = true, Arguments =  str});
            return (int)ExitCodes.Success;
        }
    }
}
