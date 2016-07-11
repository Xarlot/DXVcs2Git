using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.Git;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchViewModel : ViewModelBase {
        public ICommand CreateMergeRequestCommand { get; }
        public ICommand CloseMergeRequestCommand { get; }
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();
        IDialogService EditMergeRequestService => GetService<IDialogService>("editMergeRequestService");
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
        public EditBranchViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);

            CreateMergeRequestCommand = DelegateCommandFactory.Create(PerformCreateMergeRequest, CanPerformCreateMergeRequest);
            CloseMergeRequestCommand = DelegateCommandFactory.Create(PerformCloseMergeRequest, CanPerformCloseMergeRequest);
        }
        bool CanPerformCloseMergeRequest() {
            return Branch != null && Branch.MergeRequest != null;
        }
        void PerformCloseMergeRequest() {
            Branch.CloseMergeRequest();
        }
        bool CanPerformCreateMergeRequest() {
            return Branch != null && MergeRequest == null;
        }
        void PerformCreateMergeRequest() {
            var branchInfo = Branch.CalcBranchInfo();
            var createMergeRequestViewModel = new CreateMergeRequestViewModel() {
                Description = branchInfo.Commit.Message,
            };
            var dialogResult = EditMergeRequestService.ShowDialog(MessageButton.OKCancel, "Merge request", createMergeRequestViewModel);
            if (dialogResult == MessageResult.OK) {
                string message = createMergeRequestViewModel.Description;
                string title = CalcMergeRequestTitle(message);
                string description = CalcMergeRequestDescription(message);
                string targetBranch = CalcTargetBranch();
                if (targetBranch == null) {
                    MessageBoxService.Show("Can`t create merge request. Target branch not found.", "Create merge request error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Branch.CreateMergeRequest(title, description, null, Branch.Name, targetBranch);
            }
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
        string CalcTargetBranch() {
            var repository = Branch.Repository;
            GitRepoConfig repoConfig = repository.RepoConfig;
            return repoConfig?.TargetBranch;
        }

        void OnMessageReceived(Message msg) {
            if (msg.MessageType == MessageType.RefreshSelectedBranch) {
                Branch = RepositoriesViewModel.SelectedBranch;
                MergeRequest = Branch?.MergeRequest;
                HasMergeRequest = MergeRequest != null;
            }
        }
    }
}
