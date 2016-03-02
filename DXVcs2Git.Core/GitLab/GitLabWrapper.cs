using System;
using System.Collections.Generic;
using System.Linq;
using DXVcs2Git.Core;
using NGitLab;
using NGitLab.Models;
using User = NGitLab.Models.User;

namespace DXVcs2Git.Git {
    public class GitLabWrapper {
        const string Ignoresharedfiles = "[ISF]";
        readonly GitLabClient client;
        public GitLabWrapper(string server, string token) {
            client = GitLabClient.Connect(server, token);
        }
        public IEnumerable<Project> GetProjects() {
            return client.Projects.Accessible;
        }
        public Project GetProject(int id) {
            return this.client.Projects[id];
        }
        public Project FindProject(string project) {
            return client.Projects.Accessible.FirstOrDefault(x => x.HttpUrl == project);
        }
        public IEnumerable<MergeRequest> GetMergeRequests(Project project, Func<MergeRequest, bool> mergeRequestsHandler = null) {
            mergeRequestsHandler = mergeRequestsHandler ?? (x => true);
            var mergeRequestsClient = client.GetMergeRequest(project.Id);
            return mergeRequestsClient.AllInState(MergeRequestState.opened).Where(x => mergeRequestsHandler(x));
        }
        public MergeRequest GetMergeRequest(Project project, int id) {
            var mergeRequestsClient = client.GetMergeRequest(project.Id);
            return mergeRequestsClient[id];
        }
        public IEnumerable<MergeRequestFileData> GetMergeRequestChanges(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var changesClient = mergeRequestsClient.Changes(mergeRequest.Id);
            return changesClient.Changes.Files;
        }
        public MergeRequest ProcessMergeRequest(MergeRequest mergeRequest, string comment) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            try {
                return mergeRequestsClient.Accept(mergeRequest.Id, new MergeCommitMessage() {Message = comment});
            }
            catch (Exception ex) {
                Log.Error("Merging has thrown exception", ex);
                mergeRequest.State = "merging_failed";
                return mergeRequest;
            }
        }
        public MergeRequest UpdateMergeRequestTitleAndDescription(MergeRequest mergeRequest, string title, string description) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            return mergeRequestsClient.Update(mergeRequest.Id, new MergeRequestUpdate() {
                Description = description,
                Title = title,
            });
        }
        public MergeRequest CloseMergeRequest(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            return mergeRequestsClient.Update(mergeRequest.Id, new MergeRequestUpdate() { NewState = "close" });
        }
        public MergeRequest CreateMergeRequest(Project project, string title, string description, string user, string sourceBranch, string targetBranch) {
            var mergeRequestClient = this.client.GetMergeRequest(project.Id);
            var mergeRequest = mergeRequestClient.Create(new MergeRequestCreate() {
                Title = title,
                SourceBranch = sourceBranch,
                TargetBranch = targetBranch,
                TargetProjectId = project.Id,
            });
            return UpdateMergeRequestTitleAndDescription(mergeRequest, title, description);
        }
        public MergeRequest ReopenMergeRequest(MergeRequest mergeRequest, string autoMergeFailedComment) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            return mergeRequestsClient.Update(mergeRequest.Id, new MergeRequestUpdate() { NewState = "reopen", Description = autoMergeFailedComment });
        }
        public User GetUser(int id) {
            return this.client.Users[id];
        }
        public IEnumerable<User> GetUsers() {
            var usersClient = this.client.Users;
            return usersClient.All.ToList();
        }
        public void RegisterUser(string userName, string displayName, string email) {
            try {
                var userClient = this.client.Users;
                var userUpsert = new UserUpsert() {Username = userName, Name = displayName, Email = email, Password = new Guid().ToString()};
                userClient.Create(userUpsert);
            }
            catch (Exception ex) {
                Log.Error($"Can`t create user {userName} email {email}", ex);
                throw;
            }
        }
        public User RenameUser(User gitLabUser, string userName, string displayName, string email) {
            try {
                var userClient = this.client.Users;
                var userUpsert = new UserUpsert() { Username = userName, Name = displayName, Email = email, Password = new Guid().ToString() };
                return userClient.Update(gitLabUser.Id, userUpsert);
            }
            catch (Exception ex) {
                Log.Error($"Can`t change user {userName} email {email}", ex);
                throw;
            }
        }
        public Branch GetBranch(Project project, string branch) {
            var repo = this.client.GetRepository(project.Id);
            var branchesClient = repo.Branches;
            return branchesClient[branch];
        }
        public IEnumerable<Branch> GetBranches(Project project) {
            var repo = this.client.GetRepository(project.Id);
            var branchesClient = repo.Branches;
            return branchesClient.All;
        }
        public MergeRequest UpdateMergeRequestAssignee(MergeRequest mergeRequest, string user) {
            var userInfo = GetUsers().FirstOrDefault(x => x.Username == user);
            if (mergeRequest.Assignee?.Username != userInfo?.Username) {
                var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
                return mergeRequestsClient.Update(mergeRequest.Id, new MergeRequestUpdate() {AssigneeId = userInfo?.Id});
            }
            return mergeRequest;
        }
        public Comment AddCommentToMergeRequest(MergeRequest mergeRequest, string comment) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var commentsClient = mergeRequestsClient.Comments(mergeRequest.Id);
            return commentsClient.Add(new MergeRequestComment() {Note = comment});
        }
        public IEnumerable<ProjectHook> GetHooks(Project project) {
            var repository = this.client.GetRepository(project.Id);
            return repository.ProjectHooks.All;
        }
        public ProjectHook UpdateProjectHook(Project project, ProjectHook hook, Uri uri) {
            var repository = this.client.GetRepository(project.Id);
            return repository.ProjectHooks.Update(hook.Id, new ProjectHookUpsert() {Url = uri, MergeRequestsEvents = true, PushEvents = true});
        }
        public IEnumerable<Comment> GetComments(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var commentsClient = mergeRequestsClient.Comments(mergeRequest.Id);
            return commentsClient.All;
        }
        public bool ShouldIgnoreSharedFiles(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var commentsClient = mergeRequestsClient.Comments(mergeRequest.Id);
            var comment = commentsClient.All.LastOrDefault();
            return comment?.Note == Ignoresharedfiles;
        }
        public IEnumerable<MergeRequestFileData> GetFileChanges(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var changes = mergeRequestsClient.Changes(mergeRequest.Id);
            return changes.Changes.Files;
        }
    }
}
