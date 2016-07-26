using System;
using System.Collections.Generic;
using System.Linq;
using DXVcs2Git.Core;
using User = GitLab.NET.ResponseModels.User;
using GitLab.NET;
using GitLab.NET.ResponseModels;
using System.Threading.Tasks;

namespace DXVcs2Git.Git {
    public class GitLabWrapperNet {
        const string IgnoreValidation = "[IGNOREVALIDATION]";
        readonly GitLabClient client;
        public GitLabWrapperNet(string server, string token) {
            client = new GitLabClient(new Uri(server), token);
        }
        public async Task<PaginatedResult<Project>> GetProjects() {
            return await client.Projects.Accessible();
        }
        public async Task<RequestResult<Project>> GetProject(uint id) {
            return await client.Projects.Find(id);
        }
        public Project FindProject(string project) {//need full path now (with server address)
            var projectsTask = client.Projects.GetAll();
            projectsTask.RunSynchronously();
            return projectsTask.Result.Data.FirstOrDefault(x => x.HttpUrlToRepo.EndsWith(project, StringComparison.InvariantCultureIgnoreCase));
        }
        public async Task<PaginatedResult<MergeRequest>> GetMergeRequests(Project project, Func<MergeRequest, bool> mergeRequestsHandler = null) {
            mergeRequestsHandler = mergeRequestsHandler ?? (x => true);
            return await client.MergeRequests.GetAll(project.Id, MergeRequestState.Opened);
        }
        public async Task<RequestResult<MergeRequest>> GetMergeRequest(Project project, uint id) {
            return await client.MergeRequests.Find(project.Id, id);
        }
        public async Task<RequestResult<MergeRequest>> GetMergeRequestChanges(MergeRequest mergeRequest) {
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                return await Task.FromResult<RequestResult<MergeRequest>>(null);
            return await client.MergeRequests.GetChanges(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
        }
        public async Task<RequestResult<MergeRequest>> ProcessMergeRequest(MergeRequest mergeRequest, string comment) {
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                return await Task.FromResult<RequestResult<MergeRequest>>(null);
            return await client.MergeRequests.Accept(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, comment);
            //catch (Exception ex) {
            //    Log.Error("Merging has thrown exception", ex);
            //    mergeRequest.State = "merging_failed";
            //    return mergeRequest;
            //}
        }
        public async Task<RequestResult<MergeRequest>> UpdateMergeRequestTitleAndDescription(MergeRequest mergeRequest, string title, string description) {
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                return await Task.FromResult<RequestResult<MergeRequest>>(null);
            return await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, null, title, description);
        }
        public async Task<RequestResult<MergeRequest>> CloseMergeRequest(MergeRequest mergeRequest) {
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                return await Task.FromResult<RequestResult<MergeRequest>>(null);
            return await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, null, null, null, null, null, null, MergeRequestStateEvent.Close);
        }
        public async Task<RequestResult<MergeRequest>> CreateMergeRequest(Project origin, Project upstream, string title, string description, string user, string sourceBranch, string targetBranch) {
            return await client.MergeRequests.Create(origin.Id, sourceBranch, targetBranch, title, description, null, upstream.Id); //maybe wrong
        }
        public async Task<RequestResult<MergeRequest>> ReopenMergeRequest(MergeRequest mergeRequest, string autoMergeFailedComment) {
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                return await Task.FromResult<RequestResult<MergeRequest>>(null);
            return await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, null, null, null, null, null, null, MergeRequestStateEvent.Reopen);
        }
        public async Task<RequestResult<User>> GetUser(uint id) {
            return await client.Users.Find(id);
        }
        public async Task<PaginatedResult<User>> GetUsers() {
            return await client.Users.GetAll();
        }
        public async Task<RequestResult<User>> RegisterUser(string userName, string displayName, string email) {
            return await client.Users.Create(email, new Guid().ToString(), userName, displayName);
            //catch (Exception ex) {
            //    Log.Error($"Can`t create user {userName} email {email}", ex);
            //    throw;
            //}
        }
        public async Task<RequestResult<User>> RenameUser(User gitLabUser, string userName, string displayName, string email) {
            return await client.Users.Update(Convert.ToUInt32(gitLabUser.Id), email, new Guid().ToString(), userName, displayName);
            //catch (Exception ex) {
            //    Log.Error($"Can`t change user {userName} email {email}", ex);
            //    throw;
            //}
        }
        public async Task<RequestResult<Branch>> GetBranch(Project project, string branch) {
            return await client.Branches.Find(project.Id, branch);
        }
        public async Task<RequestResult<List<Branch>>> GetBranches(Project project) {
            return await client.Branches.GetAll(Convert.ToUInt32(project.Id));
        }
        public async Task<RequestResult<MergeRequest>> UpdateMergeRequestAssignee(MergeRequest mergeRequest, string user) {
            if(mergeRequest.Assignee.Name == user)
                return await Task.FromResult<RequestResult<MergeRequest>>(new RequestResult<MergeRequest>(new RestSharp.RestResponse<MergeRequest>(), mergeRequest));
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                return await Task.FromResult<RequestResult<MergeRequest>>(null);
            var userTask = await client.Users.Find(user);
            return await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, null, null, null, Convert.ToUInt32(userTask.Data.Id));
        }
        public async Task<RequestResult<Comment>> AddCommentToMergeRequest(MergeRequest mergeRequest, string comment) {//warning! Need testing
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                return await Task.FromResult<RequestResult<Comment>>(null);
            return await client.MergeRequests.CreateComment(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, comment);
        }
        public async Task<RequestResult<List<SystemHook>>> GetHooks(Project project) {
            return await client.SystemHooks.GetAll();
        }
        public async Task<RequestResult<SystemHook>> UpdateProjectHook(Project project, SystemHook hook, Uri uri) {//warning! Need testing
            return await client.SystemHooks.Update(hook.Id, uri.AbsolutePath, mergeRequestsEvents: true, pushEvents: true);
        }
        public async Task<RequestResult<List<Comment>>> GetComments(MergeRequest mergeRequest) {//warning! Need testing
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                return await Task.FromResult<RequestResult<List<Comment>>>(null);
            return await client.MergeRequests.GetComments(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
        }
        public async Task<bool> ShouldIgnoreSharedFiles(MergeRequest mergeRequest) {//warning! Need testing
            var comments = await GetComments(mergeRequest);
            if(comments.Data == null)
                return false;
            var comment = comments.Data.LastOrDefault();
            return comment?.Note == IgnoreValidation;
        }
        public async Task<RequestResult<MergeRequest>> GetFileChanges(MergeRequest mergeRequest) {
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                return await Task.FromResult<RequestResult<MergeRequest>>(null);
            return await client.MergeRequests.GetChanges(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
        }
    }
}
