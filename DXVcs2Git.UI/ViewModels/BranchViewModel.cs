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

namespace DXVcs2Git.UI.ViewModels {
    public class BranchViewModel : BindableBase {
        readonly GitLabWrapper gitLabWrapper;
        readonly GitReaderWrapper gitReader;
        public Branch Branch { get; }
        public RepositoriesViewModel Repositories { get; }
        public RepositoryViewModel Repository { get; }
        public string Name { get; }
        public FarmStatus FarmStatus {
            get { return GetProperty(() => FarmStatus); }
            private set { SetProperty(() => FarmStatus, value); }
        }

        public ICommand ForceBuildCommand { get; private set; }
        public MergeRequestViewModel MergeRequest { get; private set; }
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
            FarmStatus = new FarmStatus();

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
            FarmStatus = FarmIntegrator.GetTaskStatus(Repository.RepoConfig?.FarmSyncTaskName);
        }
        public User GetUser(string name) {
            return this.gitLabWrapper.GetUsers().FirstOrDefault(x => x.Name == name);
        }
        public void CreateMergeRequest(string title, string description, string user, string sourceBranch, string targetBranch) {
            var mergeRequest = this.gitLabWrapper.CreateMergeRequest(Repository.Project, title, description, user, sourceBranch, targetBranch);
            MergeRequest = new MergeRequestViewModel(this.gitLabWrapper, mergeRequest);
        }
        public void CloseMergeRequest() {
            this.gitLabWrapper.CloseMergeRequest(MergeRequest.MergeRequest);
            MergeRequest = null;
        }
    }
}
