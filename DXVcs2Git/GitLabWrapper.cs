using System.Collections.Generic;
using System.Linq;
using NGitLab;
using NGitLab.Models;

namespace DXVcs2Git.Git {
    public class GitLabWrapper {
        readonly GitLabClient client;
        public GitLabWrapper(string server, string token) {
            client = GitLabClient.Connect(server, token);
        }
        public Project FindProject(string project) {
            return client.Projects.Accessible.FirstOrDefault(x => x.HttpUrl == project);
        }
        public IEnumerable<MergeRequest> GetMergeRequests(Project project, string branch) {
            var mergeRequestsClient = client.GetMergeRequest(project.Id);
            return mergeRequestsClient.All;
        }
        public void RemoveMergeRequest(MergeRequest mergeRequest) {
        }
        public IEnumerable<MergeRequestFileData> GetMergeRequestChanges(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var changesClient = mergeRequestsClient.Changes(mergeRequest.Id);
            return changesClient.Changes.Files;
        }
    }
}
