using System;
using System.IO;
using System.Reflection;
using DXVcs2Git.Core;
using DXVcs2Git.GitTools.ViewModels;

namespace DXVcs2Git.GitTools {
    public static class ConfigSerializer {
        public static readonly string AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string SettingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\GitTools\\";
        static readonly string SettingsFile = "settings.config";
        static string SettingsFilePath {
            get { return SettingsPath + SettingsFile; }
        }

        public static Options GetOptions() {
            if (!File.Exists(SettingsFilePath))
                return Options.GenerateDefault();
            try {
                return Serializer.Deserialize<Options>(SettingsFilePath);
            }
            catch {
                return Options.GenerateDefault();
            }
        }
        public static void SaveOptions(Options options) {
            try {
                Serializer.Serialize<Options>(SettingsFilePath, options);
            }
            catch {
            }
        }
    }
}
