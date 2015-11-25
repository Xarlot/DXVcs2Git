using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;

namespace DXVcs2Git.UI.ViewModels {
    public class EditSelectedRepositoryViewModel : ViewModelBase {
        public RepositoriesViewModel Parent { get { return this.GetParentViewModel<RepositoriesViewModel>(); } }
        public bool IsInitialized {
            get { return GetProperty(() => IsInitialized); }
            private set { SetProperty(() => IsInitialized, value); }
        }
        public BranchViewModel SelectedBranch {
            get { return GetProperty(() => SelectedBranch); }
            set { SetProperty(() => SelectedBranch, value); }
        }
        protected override void OnParentViewModelChanged(object parentViewModel) {
            base.OnParentViewModelChanged(parentViewModel);
        }

        public void Update() {
            Refresh();
        }
        public void Refresh() {
            IsInitialized = Parent.Return(x => x.IsInitialized, () => false);
            SelectedBranch = Parent.With(x => x.SelectedBranch);
        }
    }
}
