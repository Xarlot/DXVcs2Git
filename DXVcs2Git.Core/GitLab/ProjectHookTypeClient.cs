using System.ComponentModel.DataAnnotations;
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
        merge_request
    }
    //                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
    //                    {
    //                        string jsonString = reader.ReadToEnd();
    //                        var jsonError = SimpleJson.DeserializeObject<JsonError>(jsonString);
    //                        throw new Exception(string.Format("The remote server returned an error ({0}): {1}", errorResponse.StatusCode, jsonError.Message));
    //                    }

}
