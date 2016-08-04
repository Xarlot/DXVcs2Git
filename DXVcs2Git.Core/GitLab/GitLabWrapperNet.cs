using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using DXVcs2Git.Core;
using User = GitLab.NET.ResponseModels.User;
using GitLab.NET;
using GitLab.NET.ResponseModels;
using System.Threading.Tasks;
using System.Net;

namespace DXVcs2Git.Git {
    public class GitLabWrapperNet {
        const uint ResultsPerPage = 100;
        const string IgnoreValidation = "[IGNOREVALIDATION]";
        readonly GitLabClient client;
        public GitLabWrapperNet(string server, string token) {
            client = new GitLabClient(new Uri(server), token);
        }
        void CheckResult<T>(RequestResult<T> result) {
            if((result.StatusCode & HttpStatusCode.OK) != 0)
                throw new HttpListenerException((int)result.StatusCode);
            if(result.Data == null)
                throw new HttpListenerException((int)result.StatusCode, "Data is null");
        }
        void CheckMergeRequestParams(MergeRequest mergeRequest) {
            if(mergeRequest.ProjectId == null || mergeRequest.Id == null)
                throw new ArgumentNullException(nameof(mergeRequest), String.Format("{0}: {1}, {2} :{3}", 
                    nameof(mergeRequest.ProjectId), mergeRequest.ProjectId, 
                    nameof(mergeRequest.Id), mergeRequest.Id));
        }

        public async Task<bool> IsAdmin() {
            var user = await client.Users.GetCurrent();
            CheckResult(user);
            return await Task.FromResult<bool>(user.Data.IsAdmin.GetValueOrDefault(false));
        }
        public async Task<PaginatedResult<Project>> GetProjects(uint page = 1, uint resultsPerPage = ResultsPerPage) {
            return await client.Projects.Accessible(page, resultsPerPage);
        }
        public async Task<PaginatedResult<Project>> GetAllProjects(uint page = 1, uint resultsPerPage = ResultsPerPage) {
            return await client.Projects.GetAll(page, resultsPerPage);
        }
        public async Task<RequestResult<Project>> GetProject(uint id) {
            return await client.Projects.Find(id);
        }
        public async Task<Project> FindProject(string project) {//need full path now (with server address)
            var projects = await GetProjects();
            CheckResult(projects);
            return projects.Data.FirstOrDefault(x => x.HttpUrlToRepo.EndsWith(project, StringComparison.InvariantCultureIgnoreCase));
        }
        public async Task<Project> FindProjectFromAll(string project) {//need full path now (with server address)
            var projects = await GetAllProjects();
            CheckResult(projects);
            return projects.Data.FirstOrDefault(x => x.HttpUrlToRepo.EndsWith(project, StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task<PaginatedResult<MergeRequest>> GetMergeRequests(Project project, Func<MergeRequest, bool> mergeRequestsHandler = null, uint page = 1, uint resultsPerPage = ResultsPerPage) {
            mergeRequestsHandler = mergeRequestsHandler ?? (x => true);
            return await client.MergeRequests.GetAll(project.Id, MergeRequestState.Opened, null, null, page, resultsPerPage);
        }
        public async Task<RequestResult<MergeRequest>> GetMergeRequest(Project project, uint id) {
            return await client.MergeRequests.Find(project.Id, id);
        }
        public async Task<RequestResult<MergeRequest>> GetMergeRequestChanges(MergeRequest mergeRequest) {
            CheckMergeRequestParams(mergeRequest);
            return await client.MergeRequests.GetChanges(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
        }
        public async Task<RequestResult<List<Commit>>> GetMergeRequestCommits(MergeRequest mergeRequest) {
            CheckMergeRequestParams(mergeRequest);
            return await client.MergeRequests.GetCommits(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
        }
        public async Task<RequestResult<MergeRequest>> ProcessMergeRequest(MergeRequest mergeRequest, string comment) {
            CheckMergeRequestParams(mergeRequest);
            return await client.MergeRequests.Accept(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, comment);
            //catch (Exception ex) {
            //    Log.Error("Merging has thrown exception", ex);
            //    mergeRequest.State = "merging_failed";
            //    return mergeRequest;
            //}
        }
        public async Task<RequestResult<MergeRequest>> UpdateMergeRequestTitleAndDescription(MergeRequest mergeRequest, string title, string description) {
            CheckMergeRequestParams(mergeRequest);
            return await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, null, title, description);
        }
        public async Task<RequestResult<MergeRequest>> CloseMergeRequest(MergeRequest mergeRequest) {
            CheckMergeRequestParams(mergeRequest);
            return await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, null, null, null, null, null, null, MergeRequestStateEvent.Close);
        }
        public async Task<RequestResult<MergeRequest>> CreateMergeRequest(Project origin, Project upstream, string title, string description, string user, string sourceBranch, string targetBranch) {
            return await client.MergeRequests.Create(origin.Id, sourceBranch, targetBranch, title, description, null, upstream.Id); //maybe wrong
        }
        public async Task<RequestResult<MergeRequest>> ReopenMergeRequest(MergeRequest mergeRequest, string autoMergeFailedComment) {
            CheckMergeRequestParams(mergeRequest);
            return await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, null, null, null, null, null, null, MergeRequestStateEvent.Reopen);
        }
        public async Task<RequestResult<User>> GetUser(uint id) {
            return await client.Users.Find(id);
        }
        public async Task<PaginatedResult<User>> GetUsers(uint page = 1, uint resultsPerPage = ResultsPerPage) {
            return await client.Users.GetAll(page, resultsPerPage);
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
            CheckMergeRequestParams(mergeRequest);
            var userTask = await client.Users.Find(user);
            return await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, null, null, null, Convert.ToUInt32(userTask.Data.Id));
        }
        public async Task<RequestResult<Comment>> AddCommentToMergeRequest(MergeRequest mergeRequest, string comment) {//warning! Need testing
            CheckMergeRequestParams(mergeRequest);
            return await client.MergeRequests.CreateComment(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, comment);
        }
        public async Task<RequestResult<List<SystemHook>>> GetProjectsHooks(Project project) {//warning! Need testing
            return await client.SystemHooks.GetAll(project.Id);
        }
        public async Task<SystemHook> FindProjectHook(Project project, Func<SystemHook, bool> projectHookHandler) {//warning! Need testing
            var hooks = await GetProjectsHooks(project);
            CheckResult(hooks);
            return await Task.FromResult<SystemHook>(hooks.Data.FirstOrDefault(projectHookHandler));
        }
        public async Task<RequestResult<SystemHook>> CreateProjectHook(Project project, Uri url, bool mergeRequestEvents, bool pushEvents, bool buildEvents) {//warning! Need testing
            return await client.SystemHooks.Create(url.AbsolutePath, mergeRequestEvents, pushEvents, buildEvents, enableSSLVerification: false);
        }
        public async Task<RequestResult<SystemHook>> UpdateProjectHook(Project project, SystemHook hook, Uri uri, bool mergeRequestEvents, bool pushEvents, bool buildEvents) {//warning! Need testing
            return await client.SystemHooks.Update(hook.Id, uri.AbsolutePath, mergeRequestEvents, pushEvents, buildEvents, enableSSLVerification: false);
        }
        public async Task<RequestResult<List<Comment>>> GetComments(MergeRequest mergeRequest) {//warning! Need testing
            CheckMergeRequestParams(mergeRequest);
            return await client.MergeRequests.GetComments(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
        }
        public async Task<bool> ShouldIgnoreSharedFiles(MergeRequest mergeRequest) {//warning! Need testing
            var comments = await GetComments(mergeRequest);
            CheckResult(comments);
            var comment = comments.Data.LastOrDefault();
            return comment?.Note == IgnoreValidation;
        }
        public async Task<RequestResult<MergeRequest>> GetFileChanges(MergeRequest mergeRequest) {
            CheckMergeRequestParams(mergeRequest);
            return await client.MergeRequests.GetChanges(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
        }
        public async Task<PaginatedResult<Build>> GetBuilds(MergeRequest mergeRequest, string sha) {
            CheckMergeRequestParams(mergeRequest);
            return await client.Builds.GetByCommit(mergeRequest.ProjectId.Value, sha);
        }
        public async Task<RequestResult<Build>> ForceBuild(MergeRequest mergeRequest) {
            CheckMergeRequestParams(mergeRequest);
            foreach(var build in (await client.Builds.GetByProject(mergeRequest.ProjectId.Value, new BuildStatus[] { BuildStatus.Failed })).Data) {
                return await client.Builds.Retry(mergeRequest.ProjectId.Value, build.Id);
            }
            return await Task.FromResult<RequestResult<Build>>(null);
        }
        public async Task<RequestResult<byte[]>> DownloadArtifacts(string projectUrl, Build build) { //need full path now (with server address)
            var project = await IsAdmin() ? await FindProjectFromAll(projectUrl) : await FindProject(projectUrl);
            if(project == null)
                return await Task.FromResult<RequestResult<byte[]>>(null);
            return await client.Builds.GetArtifacts(project.Id, build.Id);
        }
        public async Task<RequestResult<byte[]>> DownloadArtifacts(MergeRequest mergeRequest, Build build) {
            if(mergeRequest.ProjectId == null)
                return await Task.FromResult<RequestResult<byte[]>>(null);
            return await client.Builds.GetArtifacts(mergeRequest.ProjectId.Value, build.Id);
        }
    }
}
