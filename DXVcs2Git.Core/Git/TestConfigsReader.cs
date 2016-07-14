using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXVcs2Git.Core.Git {
    public class TestConfigsReader {
        const string ConfigFolder = "TestConfigs";
        readonly Dictionary<string, TestConfig> configs;
        public IEnumerable<TestConfig> RegisteredConfigs => configs.Values;
        public TestConfig this[string name] => configs[name];

        public TestConfigsReader() {
            configs = GetRegisteredConfigs().ToDictionary(x => x.Name, config => config);
        }
        static IEnumerable<TestConfig> GetRegisteredConfigs() {
            string appDir = Path.GetDirectoryName(typeof (TestConfigsReader).Assembly.Location);
            var dir = Path.Combine(appDir, ConfigFolder);
            if(Directory.Exists(dir))
                return Directory.GetFiles(dir, "*.config", SearchOption.AllDirectories).Select(Serializer.Deserialize<TestConfig>).ToList();
            return Enumerable.Empty<TestConfig>();
        }
        public bool HasConfig(string name) {
            return configs.ContainsKey(name);
        }
    }
}
