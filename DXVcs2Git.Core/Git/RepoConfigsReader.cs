using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXVcs2Git.Core.Git {
    public class RepoConfigsReader {
        const string ConfigFolder = "Configs";
        readonly Dictionary<string, RepoConfig> configs;
        public IEnumerable<RepoConfig> RegisteredConfigs => configs.Values;
        public RepoConfig this[string name] => configs[name];

        public RepoConfigsReader() {
            configs = GetRegisteredConfigs().ToDictionary(x => x.Name, config => config);
        }
        static IEnumerable<RepoConfig> GetRegisteredConfigs() {
            string appDir = Path.GetDirectoryName(typeof (RepoConfigsReader).Assembly.Location);
            var dir = Path.Combine(appDir, ConfigFolder);
            if(Directory.Exists(dir))
                return Directory.GetFiles(dir, "*.config", SearchOption.AllDirectories).Select(Serializer.Deserialize<RepoConfig>).ToList();
            return Enumerable.Empty<RepoConfig>();
        }
        public bool HasConfig(string name) {
            return configs.ContainsKey(name);
        }
    }
}
