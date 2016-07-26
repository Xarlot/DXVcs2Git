using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.GitLab;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestViewModel : ViewModelBase {
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();

        IMessageBoxService MessageBoxService => GetService<IMessageBoxService>();

        string comment;
        bool assignedToService;
        bool performTesting;

        public string Comment {
            get { return comment; }
            set { SetProperty(ref comment, value, () => Comment, CommentChanged); }
        }
        public bool PerformTesting {
            get { return performTesting; }
            set { SetProperty(ref performTesting, value, () => PerformTesting, PerformTestingChanged); }
        }
        public bool AssignedToService {
            get { return assignedToService; }
            set { SetProperty(ref assignedToService, value, () => AssignedToService, AssignedToServiceChanged); }
        }
        public bool IsModified {
            get { return GetProperty(() => IsModified); }
            private set { SetProperty(() => IsModified, value); }
        }
        public ICommand ApplyCommand { get; }
        BranchViewModel Branch { get; set; }
        void PerformTestingChanged() {
            IsModified = true;
        }
        void AssignedToServiceChanged() {
            IsModified = true;
        }
        void CommentChanged() {
            IsModified = true;
        }

        public EditMergeRequestViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            ApplyCommand = DelegateCommandFactory.Create(PerformApply, CanPerformApply);
            RefreshSelectedBranch();
        }
        bool CanPerformApply() {
            return Branch?.MergeRequest != null && IsModified;
        }
        void PerformApply() {
            var mergeRequestAction = new MergeRequestSyncAction(Branch.Repository.RepoConfig.FarmTaskName, Branch.Repository.RepoConfig.FarmSyncTaskName, PerformTesting);
            var mergeRequestOptions = new MergeRequestOptions(mergeRequestAction);
            if (RepositoriesViewModel.Config.AlwaysSure || MessageBoxService.Show("Are you sure?", "Update merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Branch.UpdateMergeRequest(CalcMergeRequestTitle(Comment), CalcMergeRequestDescription(Comment), AssignedToService ? CalcServiceName() : Branch.MergeRequest.Assignee);
                Branch.UpdateMergeRequest(CalcOptionsComment(mergeRequestOptions));
                IsModified = false;
            }
        }
        string CalcOptionsComment(MergeRequestOptions options) {
            return MergeRequestOptions.ConvertToString(options);
        }
        string CalcServiceName() {
            return Branch.Repository.RepoConfig.DefaultServiceName;
        }
        string CalcMergeRequestDescription(string message) {
            var changes = message.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            changes.Skip(1).ForEach(x => sb.AppendLine(x.ToString()));
            return sb.ToString();
        }
        string CalcMergeRequestTitle(string message) {
            var changes = message.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var title = changes.FirstOrDefault();
            return title;
        }

        void OnMessageReceived(Message msg) {
            if (msg.MessageType == MessageType.RefreshSelectedBranch) {
                RefreshSelectedBranch();
            }
        }
        void RefreshSelectedBranch() {
            Branch = RepositoriesViewModel?.SelectedBranch;
            var mergeRequest = Branch?.MergeRequest;
            if (mergeRequest == null) {
                comment = null;
                assignedToService = false;
                IsModified = false;
            }
            else {
                comment = mergeRequest.Title;
                assignedToService = mergeRequest.Assignee == Branch.Repository.DefaultServiceName;
                IsModified = false;
            }
            RaisePropertyChanged(null);
        }
    }
}
