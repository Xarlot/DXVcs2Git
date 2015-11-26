using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchDescriptionViewModel : ViewModelBase {
        new BranchViewModel Parameter { get { return (BranchViewModel)base.Parameter; } }
        public EditBranchDescriptionViewModel() {
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
        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            Refresh();
        }
        public void Refresh() {
            if (Parameter == null) {
                RepositoryName = string.Empty;
                BranchName = string.Empty;
                MergeRequestAuthor = string.Empty;
                MergeRequestTitle = string.Empty;
                MergeRequestAssignee = string.Empty;
            }
            else {
                RepositoryName = Parameter.Repository.Name;
                BranchName = Parameter.Name;
                MergeRequestAuthor = Parameter.MergeRequest?.Author;
                MergeRequestTitle = Parameter.MergeRequest?.Title;
                MergeRequestAssignee = Parameter.MergeRequest?.Assignee;
            }
        }
    }
}
