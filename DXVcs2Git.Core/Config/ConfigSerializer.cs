using System;
using System.IO;
using System.Reflection;

namespace DXVcs2Git.Core.Configuration {
    public static class ConfigSerializer {
        public static readonly string AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        static readonly string SettingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\GitTools\\";
        static readonly string SettingsFile = "ui_settings.config";
        static string SettingsFilePath {
            get { return SettingsPath + SettingsFile; }
        }

        public static Config GetConfig() {
            if (!File.Exists(SettingsFilePath))
                return Config.GenerateDefault();
            try {
                return Serializer.Deserialize<Config>(SettingsFilePath);
            }
            catch {
                return Config.GenerateDefault();
            }
        }
        public static void SaveConfig(Config options) {
            try {
                Serializer.Serialize(SettingsFilePath, options);
            }
            catch {
            }
        }
    }
}
