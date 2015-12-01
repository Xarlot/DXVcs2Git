using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Core.Configuration {
    public class LauncherHelper {
        const string LauncherProcessName = "DXVcs2Git.Launcher";
        const string LauncherFileName = LauncherProcessName + ".exe";
        static readonly string[] files = {
            LauncherFileName,
            "DXVcs2Git.Launcher.exe.config",
            "DXVcs2Git.Core.dll"
        };        
        public static bool UpdateLauncher(string installPath = null, Version version = null) {
            version = version ?? VersionInfo.Version;
            var config = ConfigSerializer.GetConfig();
            Version lastVersion;
            if (Version.TryParse(config.LastVersion, out lastVersion)) {
                if (lastVersion == version)
                    return false;
            }
            var path = installPath;
            if (!Directory.Exists(path)) {
                path = config.InstallPath;
                if (!Directory.Exists(path))
                    return false;
            } else {
                config.InstallPath = path;
            }
            if(!files.Select(x=> Path.Combine(path, x)).All(File.Exists))

            foreach(var file in files) {
                File.Copy(Path.Combine(path, file), Path.Combine(ConfigSerializer.SettingsPath, file), true);
            }
            config.LastVersion = VersionInfo.Version.ToString();
            ConfigSerializer.SaveConfig(config);
            return true;
        }
        public static bool StartLauncher(int waitInMilliseconds) {
            if (Process.GetProcessesByName(LauncherProcessName).Any())
                return false;
            var launcherFullName = Path.Combine(ConfigSerializer.SettingsPath, LauncherFileName);
            if (File.Exists(launcherFullName))
                return false;
            Process.Start(new ProcessStartInfo(launcherFullName, String.Format("-w {0}", waitInMilliseconds)));
            return true;
        }
    }
}
