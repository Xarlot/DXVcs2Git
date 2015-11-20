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
        public MergeRequestsViewModel MergeRequests { get; }
        public RepositoryViewModel Repository { get; }
        public string Name { get; }
        public FarmStatus FarmStatus {
            get { return GetProperty(() => FarmStatus); }
            private set { SetProperty(() => FarmStatus, value); }
        }

        public bool HasMergeRequest { get; private set; }
        public bool IsMyMergeRequest { get; private set; }
        public ICommand CreateMergeRequestCommand { get; private set; }
        public ICommand EditMergeRequestCommand { get; private set; }
        public ICommand CloseMergeRequestCommand { get; private set; }
        public ICommand ForceBuildCommand { get; private set; }
        public MergeRequestViewModel MergeRequest { get; private set; }
        public bool IsInEditingMergeRequest {
            get { return GetProperty(() => IsInEditingMergeRequest); }
            internal set { SetProperty(() => IsInEditingMergeRequest, value); }
        }
        public EditMergeRequestViewModel EditableMergeRequest {
            get { return GetProperty(() => EditableMergeRequest); }
            private set { SetProperty(() => EditableMergeRequest, value); }
        }
        public bool HasChanges {
            get { return MergeRequest.Return(x => x.Changes.Any(), () => false); }
        }
        public BranchViewModel(GitLabWrapper gitLabWrapper, GitReaderWrapper gitReader, MergeRequestsViewModel mergeRequests, RepositoryViewModel repository, MergeRequest mergeRequest, Branch branch) {
            this.gitLabWrapper = gitLabWrapper;
            this.gitReader = gitReader;
            Repository = repository;
            Branch = branch;
            Name = branch.Name;
            MergeRequests = mergeRequests;
            FarmStatus = new FarmStatus();

            MergeRequest = mergeRequest.With(x => new MergeRequestViewModel(gitLabWrapper, mergeRequest));
            HasMergeRequest = MergeRequest != null;
            IsMyMergeRequest = HasMergeRequest;

            CreateMergeRequestCommand = DelegateCommandFactory.Create(CreateMergeRequest, CanCreateMergeRequest);
            EditMergeRequestCommand = DelegateCommandFactory.Create(EditMergeRequest, CanEditMergeRequest);
            CloseMergeRequestCommand = DelegateCommandFactory.Create(CloseMergeRequest, CanCloseMergeRequest);
            ForceBuildCommand = DelegateCommandFactory.Create(ForceBuild, CanForceBuild);
        }
        bool CanCloseMergeRequest() {
            return MergeRequests.IsInitialized && HasMergeRequest && IsMyMergeRequest;
        }
        bool CanForceBuild() {
            return MergeRequests.IsInitialized && FarmStatus.ActivityStatus == ActivityStatus.Sleeping;
        }
        void ForceBuild() {
            Repository.ForceBuild();
        }
        bool CanEditMergeRequest() {
            return MergeRequests.IsInitialized && HasMergeRequest && IsMyMergeRequest && !IsInEditingMergeRequest;
        }
        void EditMergeRequest() {
            EditableMergeRequest = new EditMergeRequestViewModel(this);
        }
        bool CanCreateMergeRequest() {
            return MergeRequests.IsInitialized && !HasMergeRequest;
        }
        public void CreateMergeRequest() {
            var message = Branch.Commit.Message;
            string title = CalcMergeRequestTitle(message);
            string description = CalcMergeRequestDescription(message);
            string targetBranch = CalcTargetBranch(Branch.Name);
            if (targetBranch == null)
                return;
            var mergeRequest = this.gitLabWrapper.CreateMergeRequest(Repository.Project, title, description, null, Branch.Name, targetBranch);
            MergeRequest = new MergeRequestViewModel(this.gitLabWrapper, mergeRequest);
            HasMergeRequest = true;

            EditMergeRequest();
        }
        string CalcTargetBranch(string name) {
            GitRepoConfig repoConfig = Repository.RepoConfig;
            if (repoConfig != null)
                return repoConfig.Name;
            return MergeRequests.ProtectedBranches.FirstOrDefault(x => name.StartsWith(x.Name)).With(x => x.Name);
        }
        string CalcMergeRequestDescription(string message) {
            var changes = message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            changes.Skip(1).ForEach(x => sb.AppendLine(x.ToString()));
            return sb.ToString();
        }
        string CalcMergeRequestTitle(string message) {
            var changes = message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var title = changes.FirstOrDefault();
            return title;
        }
        public void CloseMergeRequest() {
            if (DXMessageBox.Show(null, "Are you sure?", "Close merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                this.gitLabWrapper.CloseMergeRequest(MergeRequest.MergeRequest);
                CloseEditableMergeRequest();
                MergeRequest = null;
                HasMergeRequest = false;
            }
        }
        public void ApplyMergeRequest(EditMergeRequestViewModel newMergeRequest) {
            if (MergeRequest != null) {
                var mergeRequest = this.gitLabWrapper.UpdateMergeRequestTitleAndDescription(
                    MergeRequest.MergeRequest, CalcMergeRequestTitle(newMergeRequest.Comment), CalcMergeRequestDescription(newMergeRequest.Comment));
                if (newMergeRequest.SelectedUser != null)
                    this.gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, newMergeRequest.SelectedUser.User);
                CloseEditableMergeRequest();
                MergeRequests.Update();
            }
        }
        void CloseEditableMergeRequest() {
            EditableMergeRequest = null;
            IsInEditingMergeRequest = false;
        }
        public void CancelMergeRequest() {
            EditableMergeRequest = null;
            IsInEditingMergeRequest = false;
            MergeRequests.Update();
        }
        public void Refresh() {
            FarmStatus = FarmIntegrator.GetTaskStatus(Repository.RepoConfig?.FarmSyncTaskName);
        }
        public User GetUser(string name) {
            return this.gitLabWrapper.GetUsers().FirstOrDefault(x => x.Name == name);
        }
    }
}
