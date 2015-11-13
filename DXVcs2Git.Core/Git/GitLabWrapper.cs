using System;
using System.Collections.Generic;
using System.Linq;
using NGitLab;
using NGitLab.Models;
using User = DXVcs2Git.Core.User;

namespace DXVcs2Git.Git {
    public class GitLabWrapper {
        readonly GitLabClient client;
        public GitLabWrapper(string server, string token) {
            client = GitLabClient.Connect(server, token);
        }
        public Project FindProject(string project) {
            return client.Projects.Accessible.FirstOrDefault(x => x.HttpUrl == project);
        }
        public IEnumerable<MergeRequest> GetMergeRequests(Project project, Func<MergeRequest, bool> mergeRequestsHandler = null) {
            mergeRequestsHandler = mergeRequestsHandler ?? (x => true);
            var mergeRequestsClient = client.GetMergeRequest(project.Id);
            return mergeRequestsClient.AllInState(MergeRequestState.opened).Where(x => mergeRequestsHandler(x));
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
            return mergeRequestsClient.Update(mergeRequest.Id, new MergeRequestUpdate() {
                Description = autoMergeFailedComment,
                AssigneeId = mergeRequest.Assignee.Id,
            });
        }
        public MergeRequest ReopenMergeRequest(MergeRequest mergeRequest, string autoMergeFailedComment) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            return mergeRequestsClient.Update(mergeRequest.Id, new MergeRequestUpdate() { NewState = "reopen", Description = autoMergeFailedComment });
        }
        public IEnumerable<User> GetUsers() {
            var usersClient = this.client.Users;
            return usersClient.All.Select(x => new User(x.Username, x.Email, x.Name));
        }
        public void RegisterUser(User user) {
            var userClient = this.client.Users;
            var userUpsert = new UserUpsert() {Username = user.UserName, Name = user.DisplayName, Email = user.Email, Password = new Guid().ToString()};
            userClient.Create(userUpsert);
        }
    }
}
