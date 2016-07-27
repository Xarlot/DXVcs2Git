using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBuildLogsViewModel : ViewModelBase {
        CommitViewModel Commit => (CommitViewModel)((ISupportParentViewModel)this).ParentViewModel;
        public EditBuildLogsViewModel() {
            
        }

        protected override void OnParentViewModelChanged(object parentViewModel) {
            base.OnParentViewModelChanged(parentViewModel);
            Initialize();
        }
        void Initialize() {
            Commit.DownloadArtifacts();
        }
    }
}
