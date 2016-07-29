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
        RepositoriesViewModel Repositories => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();

        IMessageBoxService MessageBoxService => GetService<IMessageBoxService>();

        string comment;
        bool assignedToService;
        bool performTesting;

        public bool SupportsTesting {
            get { return GetProperty(() => SupportsTesting); }
            private set { SetProperty(() => SupportsTesting, value); }
        }
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
            var mergeRequestAction = new MergeRequestSyncAction(Branch.SyncTaskName, Branch.SyncServiceName, Branch.TestServiceName, PerformTesting, AssignedToService);
            var mergeRequestOptions = new MergeRequestOptions(mergeRequestAction);
            if (Repositories.Config.AlwaysSure || MessageBoxService.Show("Are you sure?", "Update merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Branch.UpdateMergeRequest(CalcMergeRequestTitle(Comment), CalcMergeRequestDescription(Comment), CalcServiceName());
                Branch.UpdateMergeRequest(CalcOptionsComment(mergeRequestOptions));
                IsModified = false;
                RepositoriesViewModel.RaiseRefreshSelectedBranch();
            }
        }
        string CalcOptionsComment(MergeRequestOptions options) {
            return MergeRequestOptions.ConvertToString(options);
        }
        string CalcServiceName() {
            if (!AssignedToService && !PerformTesting)
                return IsServiceUser(Branch.MergeRequest.Assignee) ? null : Branch.MergeRequest.Assignee;

            return PerformTesting ? Branch.TestServiceName : Branch.SyncServiceName;
        }
        bool IsServiceUser(string assignee) {
            return !string.IsNullOrEmpty(assignee) && assignee.StartsWith("dxvcs2git.");
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
            Branch = Repositories?.SelectedBranch;
            var mergeRequest = Branch?.MergeRequest;
            if (mergeRequest == null) {
                comment = null;
                assignedToService = false;
                performTesting = false;
                IsModified = false;
            }
            else {
                performTesting = Branch.ShouldPerformTesting(mergeRequest.MergeRequest);
                comment = mergeRequest.Title;
                assignedToService = mergeRequest.Assignee == Branch.Repository.DefaultServiceName;
                IsModified = false;
            }
            SupportsTesting = Branch?.SupportsTesting ?? false;
            RaisePropertyChanged(null);
        }
    }
}
