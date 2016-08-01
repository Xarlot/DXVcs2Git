using System.Runtime.Serialization;

namespace DXVcs2Git.Core.GitLab {
    [DataContract]
    public class ProjectHookTypeClient : IParseApiSupported {
        [DataMember(Name = "object_kind")]
        public ProjectHookType HookType { get; set; }
        public string Json { get; set; }
    }

    public interface IParseApiSupported {
        string Json { get; set; }
    }

    public enum ProjectHookType {
        push,
        tag_push,
        issue,
        note,
        merge_request,
        build,
    }
}
