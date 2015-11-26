using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBranchChangesViewModel : ViewModelBase {
        new BranchViewModel Parameter { get { return (BranchViewModel)base.Parameter; } }
        EditSelectedRepositoryViewModel Parent { get { return (EditSelectedRepositoryViewModel)this.GetParentViewModel<EditSelectedRepositoryViewModel>(); } }

        public BranchViewModel Branch {
            get { return GetProperty(() => Branch); }
            private set { SetProperty(() => Branch, value); }
        }
        public MergeRequestViewModel MergeRequest {
            get { return GetProperty(() => MergeRequest); }
            private set { SetProperty(() => MergeRequest, value); }
        }
        public bool HasEditableMergeRequest {
            get { return GetProperty(() => HasEditableMergeRequest); }
            private set { SetProperty(() => HasEditableMergeRequest, value); }
        }
        protected override void OnParentViewModelChanged(object parentViewModel) {
            base.OnParentViewModelChanged(parentViewModel);
            Refresh();
        }
        public void Refresh() {
            MergeRequest = Parameter?.MergeRequest;
            HasEditableMergeRequest = Parameter?.IsInEditingMergeRequest ?? false;
            Branch = Parameter;
        }
        public void CancelMergeRequestChanges() {
            Parent.CancelEditMergeRequest();
        }
        public void ApplyMergeRequestChanges(EditMergeRequestData data) {
            Parent.UpdateMergeRequest(data);
        }
    }
}
