using Newtonsoft.Json;

namespace DXVcs2Git.Core.Listener {
    public class ProjectHookClient {
        public static ProjectHookClientSide ParseHook(string json) {
            var hook = JsonConvert.DeserializeObject<ProjectHookClientSide>(json);
            return hook;
        }
    }
}
