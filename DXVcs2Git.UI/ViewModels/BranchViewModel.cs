using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core;
using DXVcs2Git.Core.GitLab;
using DXVcs2Git.Git;
using DXVcs2Git.UI.Farm;
using Microsoft.Practices.ServiceLocation;
using NGitLab;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class BranchViewModel : BindableBase {
        protected bool Equals(BranchViewModel other) {
            return this.Repository.Equals(other.Repository) && Name == other.Name;
        }
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((BranchViewModel)obj);
        }
        public override int GetHashCode() {
            return 0;
        }
        readonly GitLabWrapper gitLabWrapper;
        public RepositoriesViewModel Repositories => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();
        public RepositoryViewModel Repository { get; }
        public string Name { get; }

        public ICommand ForceBuildCommand { get; private set; }
        public FarmStatus FarmStatus {
            get { return GetProperty(() => FarmStatus); }
            private set { SetProperty(() => FarmStatus, value); }
        }
        public MergeRequestViewModel MergeRequest {
            get { return GetProperty(() => MergeRequest); }
            private set { SetProperty(() => MergeRequest, value); }
        }
        public bool SupportsTesting => Repositories.Config.SupportsTesting && Repository.RepoConfig.SupportsTesting;
        public string SyncTaskName => Repository.RepoConfig.FarmSyncTaskName;
        public string SyncServiceName => Repository.RepoConfig.DefaultServiceName;
        public string TestServiceName => Repository.RepoConfig.TestServiceName ?? SyncServiceName;
        public string WebHookTask => Repository.RepoConfig.WebHookTask;
        public string WebHook => Repository.RepoConfig.WebHook;
        public bool HasChanges {
            get { return MergeRequest.Return(x => x.Changes.Any(), () => false); }
        }
        public BranchViewModel(GitLabWrapper gitLabWrapper, RepositoryViewModel repository, string branch) {
            this.gitLabWrapper = gitLabWrapper;
            Repository = repository;
            Name = branch;
            ForceBuildCommand = DelegateCommandFactory.Create(ForceBuild, CanForceBuild);
        }
        bool CanForceBuild() {
            return Repositories.IsInitialized;
        }
        void ForceBuild() {
            Repository.ForceBuild();
        }
        public void RefreshMergeRequest() {
            var mergeRequest = gitLabWrapper.GetMergeRequests(Repository.Upstream, x => x.SourceProjectId == Repository.Origin.Id && x.SourceBranch == Name).FirstOrDefault();
            if (mergeRequest != null)
                MergeRequest = new MergeRequestViewModel(this, mergeRequest);
            else
                MergeRequest = null;
        }
        public Branch CalcBranchInfo() {
            return gitLabWrapper.GetBranch(Repository.Origin, Name);
        }
        public void CreateMergeRequest(string title, string description, string user, string sourceBranch, string targetBranch) {
            var mergeRequest = this.gitLabWrapper.CreateMergeRequest(Repository.Origin, Repository.Upstream, title, description, user, sourceBranch, targetBranch);
            MergeRequest = new MergeRequestViewModel(this, mergeRequest);
            RepositoriesViewModel.RaiseRefreshSelectedBranch();
        }
        public void CloseMergeRequest() {
            this.gitLabWrapper.CloseMergeRequest(MergeRequest.MergeRequest);
            MergeRequest = null;
            RepositoriesViewModel.RaiseRefreshSelectedBranch();
        }
        public IEnumerable<Commit> GetCommits(MergeRequest mergeRequest) {
            return gitLabWrapper.GetMergeRequestCommits(mergeRequest);
        }
        public IEnumerable<Job> GetBuilds(MergeRequest mergeRequest, Sha1 sha) {
            return gitLabWrapper.GetBuilds(mergeRequest, sha);
        }
        public void UpdateMergeRequest(string title, string description, string assignee) {
            var mergeRequest = this.gitLabWrapper.UpdateMergeRequestTitleAndDescription(MergeRequest.MergeRequest, title, description);
            mergeRequest = this.gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, assignee);
            MergeRequest = new MergeRequestViewModel(this, mergeRequest);
        }
        public void UpdateMergeRequest(string comment) {
            this.gitLabWrapper.AddCommentToMergeRequest(MergeRequest.MergeRequest, comment);
        }
        public void AddMergeRequestSyncInfo(bool testIntegration, bool assignToService) {
            var mergeRequestAction = new MergeRequestSyncAction(SyncTaskName, SyncServiceName, testIntegration, assignToService);
            var mergeRequestOptions = new MergeRequestOptions(mergeRequestAction);
            string comment = MergeRequestOptions.ConvertToString(mergeRequestOptions);
            var mergeRequest = MergeRequest.MergeRequest;
            gitLabWrapper.AddCommentToMergeRequest(mergeRequest, comment);
            //UpdateWebHook();
        }
        public void RefreshFarm() {
            FarmStatus = FarmIntegrator.GetTaskStatus(Repository.RepoConfig.FarmSyncTaskName);
        }
        public MergeRequestSyncAction GetSyncOptions(MergeRequest mergeRequest) {
            var comments = gitLabWrapper.GetComments(mergeRequest);
            var mergeRequestSyncOptions = comments.Where(x => IsXml(x.Note)).Where(x => {
                var mr = MergeRequestOptions.ConvertFromString(x.Note);
                return mr?.ActionType == MergeRequestActionType.sync;
            }).Select(x => (MergeRequestSyncAction)MergeRequestOptions.ConvertFromString(x.Note).Action).FirstOrDefault();
            return mergeRequestSyncOptions;
        }
        static bool IsXml(string xml) {
            return !string.IsNullOrEmpty(xml) && xml.StartsWith("<");
        }
        public byte[] DownloadArtifacts(string project, Job build) {
            return gitLabWrapper.DownloadArtifacts(project, build);
        }
        public byte[] DownloadArtifacts(MergeRequest mergeRequest, Job build) {
            return gitLabWrapper.DownloadArtifacts(mergeRequest, build);
        }
        public byte[] DownloadTrace(MergeRequest mergeRequest, Job build) {
            return gitLabWrapper.DownloadTrace(mergeRequest, build);
        }
        public void ForceBuild(MergeRequest mergeRequest, Job build = null) {
            gitLabWrapper.ForceBuild(mergeRequest, build);
        }
        public void AbortBuild(MergeRequest mergeRequest, Job build = null) {
            gitLabWrapper.AbortBuild(mergeRequest, build);
        }
        public void UpdateWebHook() {
            if (!SupportsTesting)
                return;
            var sourceProject = gitLabWrapper.GetProject(MergeRequest.MergeRequest.SourceProjectId);
            var webHook = gitLabWrapper.FindProjectHook(sourceProject, x => WebHookHelper.IsSharedHook(WebHook, x.Url));
            if (webHook != null && WebHookHelper.EnsureWebHook(webHook))
                return;

            var webHookTask = WebHookTask;
            var webHookPath = WebHook;
            if (string.IsNullOrEmpty(webHookTask) || string.IsNullOrEmpty(webHookPath))
                return;
            var farmStatus = FarmIntegrator.GetExtendedTaskStatus(webHookTask);
            if (farmStatus == null)
                return;
            var url = new Uri(WebHookHelper.GetSharedHookUrl(IPAddress.Parse(farmStatus.HyperHost), webHookPath));
            if (webHook == null)
                gitLabWrapper.CreateProjectHook(sourceProject, url, true, true, true);
            else
                gitLabWrapper.UpdateProjectHook(sourceProject, webHook, url, true, true, true);
        }
        public IEnumerable<MergeRequestFileData> GetMergeRequestChanges(MergeRequest mergeRequest) {
            return gitLabWrapper.GetMergeRequestChanges(mergeRequest);
        }
    }
}
