using System.Runtime.Serialization;

namespace DXVcs2Git.Core.GitLab {
    [DataContract]
    public class MergeRequestUser {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "username")]
        public string UserName { get; set; }
        [DataMember(Name = "avatar_url")]
        public string AvatarURL;
    }
}
