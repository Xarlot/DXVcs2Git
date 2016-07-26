using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.GitLab;
using DXVcs2Git.Git;
using DXVcs2Git.UI.Farm;
using Microsoft.Practices.ServiceLocation;
using NGitLab;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class BranchViewModel : BindableBase {
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
                MergeRequest = new MergeRequestViewModel(gitLabWrapper, mergeRequest);
            else
                MergeRequest = null;
        }
        public Branch CalcBranchInfo() {
            return gitLabWrapper.GetBranch(Repository.Origin, Name);
        }
        public void CreateMergeRequest(string title, string description, string user, string sourceBranch, string targetBranch) {
            var mergeRequest = this.gitLabWrapper.CreateMergeRequest(Repository.Origin, Repository.Upstream, title, description, user, sourceBranch, targetBranch);
            MergeRequest = new MergeRequestViewModel(this.gitLabWrapper, mergeRequest);
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
        public IEnumerable<Build> GetBuilds(MergeRequest mergeRequest, Sha1 sha) {
            return gitLabWrapper.GetBuilds(mergeRequest, sha);
        }
        public void UpdateMergeRequest(string title, string description, string assignee) {
            var mergeRequest = this.gitLabWrapper.UpdateMergeRequestTitleAndDescription(MergeRequest.MergeRequest, title, description);
            mergeRequest = this.gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, assignee);
            MergeRequest = new MergeRequestViewModel(this.gitLabWrapper, mergeRequest);
        }
        public void UpdateMergeRequest(string comment) {
            this.gitLabWrapper.AddCommentToMergeRequest(MergeRequest.MergeRequest, comment);
        }
        public void RefreshFarm() {
            FarmStatus = FarmIntegrator.GetTaskStatus(Repository.RepoConfig.FarmSyncTaskName);
        }
        public bool ShouldPerformTesting(MergeRequest mergeRequest) {
            var comments = gitLabWrapper.GetComments(mergeRequest);
            var mergeRequestSyncOptions = comments.Where(x => IsXml(x.Note)).Where(x => {
                var mr = MergeRequestOptions.ConvertFromString(x.Note);
                return mr?.ActionType == MergeRequestActionType.sync;
            }).Select(x => (MergeRequestSyncAction)MergeRequestOptions.ConvertFromString(x.Note).Action).LastOrDefault();
            return mergeRequestSyncOptions?.PerformTesting ?? false;
        }
        static bool IsXml(string xml) {
            return !string.IsNullOrEmpty(xml) && xml.StartsWith("<");
        }
    }
}
