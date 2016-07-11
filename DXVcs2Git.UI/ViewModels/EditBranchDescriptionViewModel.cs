using DevExpress.Mvvm;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchDescriptionViewModel : BindableBase {
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();
        BranchViewModel BranchViewModel => RepositoriesViewModel.SelectedBranch;
        public EditBranchDescriptionViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
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
        public void Refresh() {
            var parameter = BranchViewModel;
            if (parameter == null) {
                RepositoryName = string.Empty;
                BranchName = string.Empty;
                MergeRequestAuthor = string.Empty;
                MergeRequestTitle = string.Empty;
                MergeRequestAssignee = string.Empty;
            }
            else {
                RepositoryName = parameter.Repository.Name;
                BranchName = parameter.Name;
                MergeRequestAuthor = parameter.MergeRequest?.Author;
                MergeRequestTitle = parameter.MergeRequest?.Title;
                MergeRequestAssignee = parameter.MergeRequest?.Assignee;
            }
        }
        void OnMessageReceived(Message msg) {
            if (msg.MessageType == MessageType.RefreshSelectedBranch)
                Refresh();
        }
    }
}
