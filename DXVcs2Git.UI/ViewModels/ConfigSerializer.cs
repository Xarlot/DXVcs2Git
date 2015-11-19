using System;
using System.IO;
using System.Reflection;
using DXVcs2Git.Core;

namespace DXVcs2Git.UI.ViewModels {
    public static class ConfigSerializer {
        public static readonly string AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        static readonly string SettingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\GitTools\\";
        static readonly string SettingsFile = "ui_settings.config";
        static string SettingsFilePath {
            get { return SettingsPath + SettingsFile; }
        }

        public static Config GetOptions() {
            if (!File.Exists(SettingsFilePath))
                return Config.GenerateDefault();
            try {
                return Serializer.Deserialize<Config>(SettingsFilePath);
            }
            catch {
                return Config.GenerateDefault();
            }
        }
        public static void SaveOptions(Config options) {
            try {
                Serializer.Serialize(SettingsFilePath, options);
            }
            catch {
            }
        }
    }
}
