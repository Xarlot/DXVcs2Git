using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestViewModel : ViewModelBase {
        RepositoriesViewModel Repositories => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();

        IMessageBoxService MessageBoxService => GetService<IMessageBoxService>();

        string comment;
        bool assignedToService;
        bool assignedToServiceAfterTesting;
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
        public bool AssignedToServiceAfterTesting {
            get { return assignedToServiceAfterTesting; }
            set { SetProperty(ref assignedToServiceAfterTesting, value, () => AssignedToServiceAfterTesting, AssignedToServiceAfterTestingChanged); }
        }
        public bool IsModified {
            get { return GetProperty(() => IsModified); }
            private set { SetProperty(() => IsModified, value); }
        }
        public bool UseInstantMerge {
            get { return GetProperty(() => UseInstantMerge); }
            private set { SetProperty(() => UseInstantMerge, value); }
        }
        public ICommand ApplyCommand { get; }
        public ICommand MergeCommand { get; }
        BranchViewModel Branch { get; set; }
        void PerformTestingChanged() {
            IsModified = true;
            assignedToService = false;
            RaisePropertyChanged("AssignedToService");
        }
        void AssignedToServiceChanged() {
            IsModified = true;
            performTesting = false;
            assignedToServiceAfterTesting = false;
            RaisePropertyChanged("PerformTesting");
            RaisePropertyChanged("AssignedToServiceAfterTesting");
        }
        void AssignedToServiceAfterTestingChanged() {
            IsModified = true;
            assignedToService = false;
            RaisePropertyChanged("AssignedToService");
        }
        void CommentChanged() {
            IsModified = true;
        }

        public EditMergeRequestViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            ApplyCommand = DelegateCommandFactory.Create(PerformApply, CanPerformApply);
            MergeCommand = DelegateCommandFactory.Create(PerformMerge, CanPerformMerge);
            RefreshSelectedBranch();
        }
        bool CanPerformApply() {
            return Branch?.MergeRequest != null && IsModified;
        }
        bool CanPerformMerge() {
            return Branch?.MergeRequest != null && UseInstantMerge;
        }
        void PerformApply() {
            if (Repositories.Config.AlwaysSure || MessageBoxService.Show("Are you sure?", "Update merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Branch.UpdateMergeRequest(CalcMergeRequestTitle(Comment), CalcMergeRequestDescription(Comment), CalcServiceName());
                Branch.AddMergeRequestSyncInfo(PerformTesting, AssignedToServiceAfterTesting);
                IsModified = false;
                RepositoriesViewModel.RaiseRefreshSelectedBranch();
            }
        }
        void PerformMerge() {
            if(!UseInstantMerge)
                return;
            Branch.AcceptMergeRequest();
        }
        string CalcServiceName() {
            if (!AssignedToService && !PerformTesting)
                return IsServiceUser(Branch.MergeRequest.Assignee) ? Branch.MergeRequest.Author : Branch.MergeRequest.Assignee;
            return PerformTesting ? Branch.TestServiceName : Branch.SyncServiceName;
        }
        bool IsServiceUser(string assignee) {
            return !string.IsNullOrEmpty(assignee) && assignee.StartsWith("dxvcs2git.");
        }
        bool IsTestUser(string assignee) {
            return !string.IsNullOrEmpty(assignee) && assignee == Branch?.TestServiceName;
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
                if (IsModified)
                    return;
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
                if (Branch.SupportsTesting) {
                    var syncOptions = Branch.GetSyncOptions(mergeRequest.MergeRequest);
                    performTesting = syncOptions?.TestIntegration ?? false;
                    assignedToServiceAfterTesting = syncOptions?.AssignToSyncService ?? false;
                    assignedToService = !assignedToServiceAfterTesting && mergeRequest.Assignee == Branch.SyncServiceName;
                }
                else {
                    assignedToService = mergeRequest.Assignee == Branch.SyncServiceName;
                    performTesting = false;
                }
                comment = mergeRequest.Title;
                IsModified = false;
            }
            SupportsTesting = Branch?.SupportsTesting ?? false;
            UseInstantMerge = Branch?.Repository?.RepoConfig?.UseInstantMerge ?? false;
            RaisePropertyChanged(null);
        }
    }
}
