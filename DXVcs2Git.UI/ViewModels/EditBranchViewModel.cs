using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.Git;
using DXVcs2Git.UI.Farm;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchViewModel : ViewModelBase {
        public ICommand CreateMergeRequestCommand { get; }
        public ICommand CloseMergeRequestCommand { get; }
        public ICommand ShowMergeRequestCommand { get; }
        public ICommand CopyMergeRequestLinkCommand { get; }
        public ICommand ForceBuildCommand { get; }
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();
        IMessageBoxService MessageBoxService => GetService<IMessageBoxService>();

        public BranchViewModel Branch {
            get { return GetProperty(() => Branch); }
            private set { SetProperty(() => Branch, value); }
        }
        public MergeRequestViewModel MergeRequest {
            get { return GetProperty(() => MergeRequest); }
            private set { SetProperty(() => MergeRequest, value); }
        }
        public bool HasMergeRequest {
            get { return GetProperty(() => HasMergeRequest); }
            private set { SetProperty(() => HasMergeRequest, value); }
        }
        public bool SupportsTesting {
            get { return GetProperty(() => SupportsTesting); }
            private set { SetProperty(() => SupportsTesting, value); }
        }
        public FarmStatus FarmStatus {
            get { return GetProperty(() => FarmStatus); }
            private set { SetProperty(() => FarmStatus, value); }
        }
        public EditBranchViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);

            CreateMergeRequestCommand = DelegateCommandFactory.Create(PerformCreateMergeRequest, CanPerformCreateMergeRequest);
            CloseMergeRequestCommand = DelegateCommandFactory.Create(PerformCloseMergeRequest, CanPerformCloseMergeRequest);
            ShowMergeRequestCommand = DelegateCommandFactory.Create(PerformShowMergeRequest, CanShowMergeRequest);
            CopyMergeRequestLinkCommand = DelegateCommandFactory.Create(PerformCopyMergeRequestLink, CanCopyMergeRequestLink);
            ForceBuildCommand = DelegateCommandFactory.Create(PerformForceBuild, CanPerformForceBuild);
            RefreshSelectedBranch();
        }
        bool CanCopyMergeRequestLink() {
            return Branch?.MergeRequest != null;
        }
        void PerformCopyMergeRequestLink() {
            var mergeRequestUri = GetMergeLink();
            Clipboard.SetData(DataFormats.Text, mergeRequestUri);
        }
        bool CanShowMergeRequest() {
            return Branch?.MergeRequest != null;
        }
        void PerformShowMergeRequest() {
            var mergeRequestUri = GetMergeLink();
            Process.Start(mergeRequestUri);
        }
        string GetMergeLink() {
            var mergeRequest = Branch.MergeRequest;
            var repository = Branch.Repository;
            var config = repository.RepoConfig;
            string mergeRequestUri = $"{config.Server}/{repository.Upstream.PathWithNamespace}/merge_requests/{mergeRequest.MergeRequest.Iid}";
            return mergeRequestUri;
        }
        bool CanPerformForceBuild() {
            return Branch?.MergeRequest != null && (FarmStatus.ActivityStatus == ActivityStatus.Sleeping || FarmStatus.ActivityStatus == ActivityStatus.Pending);
        }
        void PerformForceBuild() {
            FarmIntegrator.ForceBuild(Branch.Repository.RepoConfig.FarmSyncTaskName);
        }
        bool CanPerformCloseMergeRequest() {
            return Branch?.MergeRequest != null;
        }
        void PerformCloseMergeRequest() {
            Branch.CloseMergeRequest();
        }
        bool CanPerformCreateMergeRequest() {
            return Branch != null && MergeRequest == null;
        }
        void PerformCreateMergeRequest() {
            var branchInfo = Branch.CalcBranchInfo();
            string message = branchInfo.Commit.Message;
            string title = CalcMergeRequestTitle(message);
            string description = CalcMergeRequestDescription(message);
            string targetBranch = CalcTargetBranch();
            if (targetBranch == null) {
                MessageBoxService.Show("Can`t create merge request. Target branch not found.", "Create merge request error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Branch.CreateMergeRequest(title, description, null, Branch.Name, targetBranch);
        }
        string CalcMergeRequestDescription(string message) {
            if (string.IsNullOrEmpty(message))
                return string.Empty;
            var changes = message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            changes.Skip(1).ForEach(x => sb.AppendLine(x.ToString()));
            return sb.ToString();
        }
        string CalcMergeRequestTitle(string message) {
            if (string.IsNullOrEmpty(message))
                return string.Empty;
            var changes = message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var title = changes.FirstOrDefault();
            return title;
        }
        string CalcTargetBranch() {
            var repository = Branch.Repository;
            RepoConfig repoConfig = repository.RepoConfig;
            return repoConfig?.TargetBranch;
        }

        void OnMessageReceived(Message msg) {
            if (msg.MessageType == MessageType.RefreshSelectedBranch) 
                RefreshSelectedBranch();
            RefreshFarm();
        }
        void RefreshSelectedBranch() {
            Branch = RepositoriesViewModel.SelectedBranch;
            MergeRequest = Branch?.MergeRequest;
            HasMergeRequest = MergeRequest != null;
            SupportsTesting = Branch?.SupportsTesting ?? false;
        }
        void RefreshFarm() {
            FarmStatus = Branch?.FarmStatus ?? new FarmStatus();
        }
    }
}
