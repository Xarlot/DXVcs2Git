using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchChangesViewModel : ViewModelBase {
        BranchViewModel Parent { get { return this.GetParentViewModel<BranchViewModel>(); } }

        public MergeRequestViewModel MergeRequest {
            get { return GetProperty(() => MergeRequest); }
            private set { SetProperty(() => MergeRequest, value); }
        }

        protected override void OnParentViewModelChanged(object parentViewModel) {
            base.OnParentViewModelChanged(parentViewModel);
            Refresh();
        }
        public void Refresh() {
            MergeRequest = Parent?.MergeRequest;
        }
    }
}
