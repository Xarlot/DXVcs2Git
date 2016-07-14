using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DXVcs2Git.Core.Configuration {
    public class LauncherHelper {
        const string LauncherProcessName = "DXVcs2Git.Launcher";
        const string LauncherFileName = LauncherProcessName + ".exe";        
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
            var launcherFullPath = Path.Combine(path, LauncherFileName);
            if (!File.Exists(launcherFullPath))
                return false;
            if (!Directory.Exists(ConfigSerializer.SettingsPath))
                Directory.CreateDirectory(ConfigSerializer.SettingsPath);
            File.Copy(launcherFullPath, Path.Combine(ConfigSerializer.SettingsPath, LauncherFileName), true);
            config.LastVersion = version.ToString();
            ConfigSerializer.SaveConfig(config);
            return true;
        }
        public static bool StartLauncher(int waitInMilliseconds) {
            if (Process.GetProcessesByName(LauncherProcessName).Any())
                return false;
            var launcherFullName = Path.Combine(ConfigSerializer.SettingsPath, LauncherFileName);
            if (!File.Exists(launcherFullName))
                return false;
            Process.Start(new ProcessStartInfo(launcherFullName, String.Format("-w {0}", waitInMilliseconds)));
            return true;
        }
    }
}
