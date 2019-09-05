using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXVcs2Git.Core.Git {
    public class RepoConfigsReader {
        const string ConfigFolder = "Configs";
        readonly Dictionary<string, RepoConfig> configs;
        public IEnumerable<RepoConfig> RegisteredConfigs => configs.Values;
        public RepoConfig this[string name] => configs[name];

        public RepoConfigsReader(string appDir) {
            configs = GetRegisteredConfigs(appDir).ToDictionary(x => x.Name, config => config);
        }
        public IEnumerable<RepoConfig> GetRegisteredConfigs(string appDir) {
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
