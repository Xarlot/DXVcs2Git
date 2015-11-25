using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchDescriptionViewModel : ViewModelBase {
        BranchViewModel Parent { get { return this.GetParentViewModel<BranchViewModel>(); } }
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
        protected override void OnParentViewModelChanged(object parentViewModel) {
            base.OnParentViewModelChanged(parentViewModel);
            Refresh();
        }
        public void Refresh() {
            if (Parent == null) {
                RepositoryName = string.Empty;
                BranchName = string.Empty;
                MergeRequestAuthor = string.Empty;
                MergeRequestTitle = string.Empty;
            }
            else {
                RepositoryName = Parent.Repository.Name;
                BranchName = Parent.Name;
                MergeRequestAuthor = Parent.MergeRequest?.Author;
                MergeRequestTitle = Parent.MergeRequest?.Title;
            }
        }
    }
}
