using System.Runtime.Serialization;

namespace DXVcs2Git.Core.GitLab {
    [DataContract]
    public class MergeRequestAttributes {
        [DataMember(Name = "id")]
        public int Id { get; set; }
        [DataMember(Name = "target_branch")]
        public string TargetBranch { get; set; }
        [DataMember(Name = "source_branch")]
        public string SourceBranch { get; set; }
        [DataMember(Name = "source_project_id")]
        public int SourceProjectId { get; set; }
        [DataMember(Name = "target_project_id")]
        public int TargetProjectId { get; set; }
        [DataMember(Name = "author_id")]
        public int AuthorId { get; set; }
        [DataMember(Name = "assignee_id")]
        public int? AssigneeId { get; set; }
        [DataMember(Name = "title")]
        public string Title { get; set; }
        //[DataMember(Name = "created_at")]
        //public DateTime CreatedAt { get; set; }
        //[DataMember(Name = "updated_at")]
        //public DateTime UpdatedAt { get; set; }
        [DataMember(Name = "state")]
        public MergerRequestState State { get; set; }
        [DataMember(Name = "merge_status")]
        public string MergeStatus { get; set; }
        [DataMember(Name = "iid")]
        public int IID { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "work_in_process")]
        public bool WorkInProcess { get; set; }
        [DataMember(Name = "action")]
        public string Action { get; set; }
    }

    public enum MergerRequestState {
        opened,
        reopened,
        closed,
        merged,
    }
}
