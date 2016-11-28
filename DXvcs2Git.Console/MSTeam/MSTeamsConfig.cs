using System.Collections.Generic;
using System.Linq;

namespace DXVcs2Git.Console {
    public class MSTeamsConfigItem {
        public string WebHook { get; set; }
        public string Repo { get; set; }
    }

    public class MSTeamsConfig {
        public List<MSTeamsConfigItem> Configs { get; set; }

        public MSTeamsConfigItem GetConfig(string url) {
            return Configs.FirstOrDefault(x => string.CompareOrdinal(url, x.Repo) == 0);
        }
    }
}
