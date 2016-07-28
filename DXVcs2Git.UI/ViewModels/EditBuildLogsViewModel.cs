using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBuildLogsViewModel : ViewModelBase {
        CommitViewModel Commit => (CommitViewModel)((ISupportParentViewModel)this).ParentViewModel;
        public EditBuildLogsViewModel() {

        }

        public BuildLogViewModel Build {
            get { return GetProperty(() => Build); }
            private set { SetProperty(() => Build, value); }
        }
        public ArtifactsViewModel Artifact {
            get { return GetProperty(() => Artifact); }
            private set { SetProperty(() => Artifact, value); }
        }
        public TestLogViewModel Tests {
            get { return GetProperty(() => Tests); }
            private set { SetProperty(() => Tests, value); }
        }
        public ModificationsViewModel Modifications {
            get { return GetProperty(() => Modifications); }
            private set { SetProperty(() => Modifications, value); }
        }

        protected override void OnParentViewModelChanged(object parentViewModel) {
            base.OnParentViewModelChanged(parentViewModel);
            Initialize();
        }
        void Initialize() {
            Artifact = Commit.DownloadArtifacts();
            Modifications = new ModificationsViewModel(Artifact);
            Build = new BuildLogViewModel(Artifact);
            Tests = new TestLogViewModel(Artifact);
        }
    }
}
