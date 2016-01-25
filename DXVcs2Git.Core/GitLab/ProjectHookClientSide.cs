using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace DXVcs2Git.Core.Listener {
    [DataContract]
    public class ProjectHookClientSide {
        [DataType("object_kind")]
        public ProjectHookType HookType { get; set; }
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
    //var jsonError = SimpleJson.DeserializeObject<JsonError>(jsonString);
    //                        throw new Exception(string.Format("The remote server returned an error ({0}): {1}", errorResponse.StatusCode, jsonError.Message));
    //                    }

}
