using System.Runtime.Serialization;
using NGitLab.Models;
using Commit = NGitLab.Models.Commit;

namespace DXVcs2Git.Core.GitLab {
//{
//	"object_kind": "build",
//	"ref": "2016.1",
//	"tag": false,
//	"before_sha": "6b9035d63118addc7608c23f36071a650e85fef9",
//	"sha": "6b9035d63118addc7608c23f36071a650e85fef9",
//	"build_id": 480,
//	"build_name": "test_job",
//	"build_stage": "test",
//	"build_status": "pending",
//	"build_started_at": null,
//	"build_finished_at": null,
//	"build_duration": null,
//	"build_allow_failure": false,
//	"project_id": 2,
//	"project_name": "XPF / XPF",
//	"user": {
//		"id": 1,
//		"name": "Administrator",
//		"email": "admin@example.com"
//	},
//	"commit": {
//		"id": 204,
//		"sha": "6b9035d63118addc7608c23f36071a650e85fef9",
//		"message": "[a:filippov t:8971] T409074\n",
//		"author_name": "filippov",
//		"author_email": "dmitry.filippov@devexpress.com",
//		"status": "success",
//		"duration": 96,
//		"started_at": "2016-07-31 16:38:05 UTC",
//		"finished_at": "2016-07-31 16:39:41 UTC"
//	},
//	"repository": {
//		"name": "XPF",
//		"url": "git@gitserver:XPF/XPF.git",
//		"description": "",
//		"homepage": "http://gitserver/XPF/XPF",
//		"git_http_url": "http://gitserver/XPF/XPF.git",
//		"git_ssh_url": "git@gitserver:XPF/XPF.git",
//		"visibility_level": 20
//	}
//}

    [DataContract]
    public class BuildHookClient : ProjectHookClient {
        [DataMember(Name = "build_id")]
        public int BuildId;
        [DataMember(Name = "build_name")]
        public string BuildName;
        [DataMember(Name = "build_status")]
        public BuildStatus Status;
        [DataMember(Name = "commit")]
        public BuildHookCommit Commit;
        [DataMember(Name = "project_id")]
        public int ProjectId;
        [DataMember(Name = "project_name")]
        public string ProjectName;
        [DataMember(Name = "ref")]
        public string Branch;
    }
    [DataContract]
    public class BuildHookCommit {
        [DataMember(Name = "sha")]
        public long Id;
    }
}
