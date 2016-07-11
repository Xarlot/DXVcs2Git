using System;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Git;
using DXVcs2Git.UI.Farm;
using Microsoft.Practices.ServiceLocation;
using NGitLab.Models;
using ThoughtWorks.CruiseControl.Remote;

namespace DXVcs2Git.UI.ViewModels {
    public class BranchViewModel : BindableBase {
        readonly GitLabWrapper gitLabWrapper;
        public RepositoriesViewModel Repositories => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();
        public RepositoryViewModel Repository { get; }
        public string Name { get; }
        public event EventHandler MergeRequestChanged = (_, __) => { };
        FarmStatus oldFarmStatus;
        public FarmStatus FarmStatus {
            get { return GetProperty(() => FarmStatus); }
            private set { SetProperty(() => FarmStatus, value, OnFarmStatusChanged); }
        }        
        public ICommand ForceBuildCommand { get; private set; }
        public MergeRequestViewModel MergeRequest {
            get { return GetProperty(() => MergeRequest); }
            private set { SetProperty(() => MergeRequest, value, OnMergeRequestChanged); }
        }        
        public bool IsInEditingMergeRequest {
            get { return GetProperty(() => IsInEditingMergeRequest); }
            internal set { SetProperty(() => IsInEditingMergeRequest, value); }
        }
        public bool HasChanges {
            get { return MergeRequest.Return(x => x.Changes.Any(), () => false); }
        }
        public BranchViewModel(GitLabWrapper gitLabWrapper, RepositoryViewModel repository, string branch) {
            this.gitLabWrapper = gitLabWrapper;
            Repository = repository;
            Name = branch;
            oldFarmStatus = new FarmStatus();
            FarmStatus = oldFarmStatus;
            ForceBuildCommand = DelegateCommandFactory.Create(ForceBuild, CanForceBuild);
            Refresh();
        }
        bool CanForceBuild() {
            return Repositories.IsInitialized && (FarmStatus.ActivityStatus == ActivityStatus.Sleeping || FarmStatus.ActivityStatus == ActivityStatus.Pending);
        }
        void ForceBuild() {
            Repository.ForceBuild();
        }
        public void Refresh() {
            RefreshFarm();
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
        }
        public void CloseMergeRequest() {
            this.gitLabWrapper.CloseMergeRequest(MergeRequest.MergeRequest);
            MergeRequest = null;
            RepositoriesViewModel.RaiseRefreshSelectedBranch();
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
            FarmStatus = FarmIntegrator.GetTaskStatus(Repository.RepoConfig?.FarmSyncTaskName);
        }
        void OnFarmStatusChanged() {
            try {
                if (oldFarmStatus.BuildStatus == IntegrationStatus.Unknown)
                    return;
                if (oldFarmStatus.ActivityStatus != FarmStatus.ActivityStatus && FarmStatus.ActivityStatus == ActivityStatus.Sleeping) {
                    //if (MergeRequest != null)
                        //Repositories.Update();
                }                    
            } finally {
                oldFarmStatus = FarmStatus;
            }
        }
        void OnMergeRequestChanged() {
            MergeRequestChanged(this, null);
        }
    }
}
