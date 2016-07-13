using DevExpress.Mvvm;
using DXVcs2Git.UI.Farm;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchDescriptionViewModel : BindableBase {
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();
        BranchViewModel BranchViewModel => RepositoriesViewModel.SelectedBranch;
        public EditBranchDescriptionViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            Refresh();
        }

        public string RepositoryName {
            get { return GetProperty(() => RepositoryName); }
            private set { SetProperty(() => RepositoryName, value); }
        }
        public string BranchName {
            get { return GetProperty(() => BranchName); }
            private set { SetProperty(() => BranchName, value); }
        }
        public string MergeRequestTitle {
            get { return GetProperty(() => MergeRequestTitle); }
            private set { SetProperty(() => MergeRequestTitle, value); }
        }
        public string MergeRequestAuthor {
            get { return GetProperty(() => MergeRequestAuthor); }
            private set { SetProperty(() => MergeRequestAuthor, value); }
        }
        public string MergeRequestAssignee {
            get { return GetProperty(() => MergeRequestAssignee); }
            private set { SetProperty(() => MergeRequestAssignee, value); }
        }
        public FarmStatus FarmStatus {
            get { return GetProperty(() => FarmStatus); }
            private set { SetProperty(() => FarmStatus, value); }
        }

        void OnFarmStatusChanged() {
        }
        void Refresh() {
            if (BranchViewModel == null) {
                RepositoryName = string.Empty;
                BranchName = string.Empty;
                MergeRequestAuthor = string.Empty;
                MergeRequestTitle = string.Empty;
                MergeRequestAssignee = string.Empty;
            }
            else {
                RepositoryName = BranchViewModel.Repository.Name;
                BranchName = BranchViewModel.Name;
                MergeRequestAuthor = BranchViewModel.MergeRequest?.Author;
                MergeRequestTitle = BranchViewModel.MergeRequest?.Title;
                MergeRequestAssignee = BranchViewModel.MergeRequest?.Assignee;
            }
            RefreshFarmStatus();
        }
        void OnMessageReceived(Message msg) {
            if (msg.MessageType == MessageType.RefreshSelectedBranch)
                Refresh();
            if (msg.MessageType == MessageType.BeforeUpdate)
                Refresh();
            if (msg.MessageType == MessageType.Update)
                Refresh();
            if (msg.MessageType == MessageType.RefreshFarm)
                RefreshFarmStatus();
        }
        void RefreshFarmStatus() {
            FarmStatus = BranchViewModel?.FarmStatus ?? new FarmStatus();
        }
    }
}
