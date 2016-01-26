using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.Serialization;

namespace DXVcs2Git.Core.GitLab {
    [DataContract]
    public abstract class ProjectHookClient : IParseApiSupported {
        [DataMember(Name = "object_kind")]
        public ProjectHookType HookType { get; internal set; }


        public string Json { get; set; }

        public static ProjectHookTypeClient ParseHookType(HttpListenerRequest message) {
            var projectHook = message.Parse<ProjectHookTypeClient>();
            return projectHook;
        }
        public static ProjectHookClient ParseHook(ProjectHookTypeClient hookType) {
            if (hookType.HookType == ProjectHookType.push) 
                return HttpRequestParser.Parse<PushHookClient>(hookType.Json);
            if (hookType.HookType == ProjectHookType.merge_request)
                return HttpRequestParser.Parse<MergeRequestHookClient>(hookType.Json);
            return null;
        }
    }
}
