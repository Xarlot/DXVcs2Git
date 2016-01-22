using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using DXVcs2Git.UI.Farm;
using NGitLab.Models;
using User = NGitLab.Models.User;
using ThoughtWorks.CruiseControl.Remote;

namespace DXVcs2Git.UI.ViewModels {
    public class BranchViewModel : BindableBase {
        readonly GitLabWrapper gitLabWrapper;
        readonly GitReaderWrapper gitReader;
        public Branch Branch { get; }
        public RepositoriesViewModel Repositories { get; }
        public RepositoryViewModel Repository { get; }
        public string Name { get; }
        public event EventHandler MergeRequestChanged = (_, __) => { };
        FarmStatus oldFarmStatus;
        public FarmStatus FarmStatus {
            get { return GetProperty(() => FarmStatus); }
            private set { SetProperty(() => FarmStatus, value, new Action(OnFarmStatusChanged)); }
        }        
        public ICommand ForceBuildCommand { get; private set; }
        public MergeRequestViewModel MergeRequest {
            get { return GetProperty(() => MergeRequest); }
            private set { SetProperty(() => MergeRequest, value, new Action(OnMergeRequestChanged)); }
        }        
        public bool IsInEditingMergeRequest {
            get { return GetProperty(() => IsInEditingMergeRequest); }
            internal set { SetProperty(() => IsInEditingMergeRequest, value); }
        }
        public bool HasChanges {
            get { return MergeRequest.Return(x => x.Changes.Any(), () => false); }
        }
        public BranchViewModel(GitLabWrapper gitLabWrapper, GitReaderWrapper gitReader, RepositoriesViewModel repositories, RepositoryViewModel repository, MergeRequest mergeRequest, Branch branch) {
            this.gitLabWrapper = gitLabWrapper;
            this.gitReader = gitReader;
            Repository = repository;
            Branch = branch;
            Name = branch.Name;
            Repositories = repositories;
            oldFarmStatus = new FarmStatus();
            FarmStatus = oldFarmStatus;
            MergeRequest = mergeRequest.With(x => new MergeRequestViewModel(gitLabWrapper, mergeRequest));
            ForceBuildCommand = DelegateCommandFactory.Create(ForceBuild, CanForceBuild);
        }

        bool CanForceBuild() {
            return Repositories.IsInitialized && FarmStatus.ActivityStatus == ActivityStatus.Sleeping;
        }
        void ForceBuild() {
            Repository.ForceBuild();
        }
        public void Refresh() {
            RefreshFarm();
        }
        public void CreateMergeRequest(string title, string description, string user, string sourceBranch, string targetBranch) {
            var mergeRequest = this.gitLabWrapper.CreateMergeRequest(Repository.Project, title, description, user, sourceBranch, targetBranch);
            MergeRequest = new MergeRequestViewModel(this.gitLabWrapper, mergeRequest);
        }
        public void CloseMergeRequest() {
            this.gitLabWrapper.CloseMergeRequest(MergeRequest.MergeRequest);
            MergeRequest = null;
        }
        public void UpdateMergeRequest(string title, string description, string assignee) {
            var mergeRequest = this.gitLabWrapper.UpdateMergeRequestTitleAndDescription(MergeRequest.MergeRequest, title, description);
            mergeRequest = this.gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, assignee);
            MergeRequest = new MergeRequestViewModel(this.gitLabWrapper, mergeRequest);
        }
        public void RefreshFarm() {
            FarmStatus = FarmIntegrator.GetTaskStatus(Repository.RepoConfig?.FarmSyncTaskName);
        }
        void OnFarmStatusChanged() {
            try {
                if (oldFarmStatus.BuildStatus == IntegrationStatus.Unknown)
                    return;
                if (oldFarmStatus.ActivityStatus != FarmStatus.ActivityStatus && FarmStatus.ActivityStatus == ActivityStatus.Sleeping) {
                    if (MergeRequest != null)
                        Repositories.BeginUpdate();
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
