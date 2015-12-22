using System.Collections.Generic;
using System.IO;
using System.Linq;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Git;

namespace DXVcs2Git.UI.ViewModels {
    public class RepoConfigsReader {
        const string ConfigFolder = "Configs";
        readonly Dictionary<string, GitRepoConfig> configs;
        public IEnumerable<GitRepoConfig> RegisteredConfigs { get { return configs.Values; } }
        public GitRepoConfig this[string name] { get { return configs[name]; } }

        public RepoConfigsReader() {
            configs = GetRegisteredConfigs().ToDictionary(x => x.Name, config => config);
        }
        static IEnumerable<GitRepoConfig> GetRegisteredConfigs() {
            string appDir = Path.GetDirectoryName(typeof (RepoConfigsReader).Assembly.Location);
            var dir = Path.Combine(appDir, ConfigFolder);
            if(Directory.Exists(dir))
                return Directory.GetFiles(dir, "*.config", SearchOption.TopDirectoryOnly).Select(Serializer.Deserialize<GitRepoConfig>).ToList();
            return Enumerable.Empty<GitRepoConfig>();
        }
        public bool HasConfig(string name) {
            return configs.ContainsKey(name);
        }
    }
}
