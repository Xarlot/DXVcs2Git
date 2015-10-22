using System.Collections.Generic;
using System.Linq;
using NGitLab;
using NGitLab.Models;

namespace DXVcs2Git.Git {
    public class GitLabWrapper {
        readonly GitLabClient client;
        readonly string branch;
        public GitLabWrapper(string server, string branch, string token) {
            client = GitLabClient.Connect(server, token);
            this.branch = branch;
        }
        public Project FindProject(string project) {
            return client.Projects.Accessible.FirstOrDefault(x => x.HttpUrl == project);
        }
        public IEnumerable<MergeRequest> GetMergeRequests(Project project) {
            var mergeRequestsClient = client.GetMergeRequest(project.Id);
            return mergeRequestsClient.AllInState(MergeRequestState.opened).Where(x => x.TargetBranch == branch);
        }
        public void RemoveMergeRequest(MergeRequest mergeRequest) {
        }
        public IEnumerable<MergeRequestFileData> GetMergeRequestChanges(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var changesClient = mergeRequestsClient.Changes(mergeRequest.Id);
            return changesClient.Changes.Files;
        }
        public MergeRequest ProcessMergeRequest(MergeRequest mergeRequest, string comment) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            return mergeRequestsClient.Accept(mergeRequest.Id, new MergeCommitMessage() { Message = comment});
        }
        public MergeRequest UpdateMergeRequest(MergeRequest mergeRequest, string autoMergeFailedComment) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            return mergeRequestsClient.Update(mergeRequest.Id, new MergeRequestUpdate() {Description = autoMergeFailedComment});
        }
        public MergeRequest ReopenMergeRequest(MergeRequest mergeRequest, string autoMergeFailedComment) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            return mergeRequestsClient.Update(mergeRequest.Id, new MergeRequestUpdate() { NewState = "reopen", Description = autoMergeFailedComment });
        }
    }
}
