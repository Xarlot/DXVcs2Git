using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBuildLogsViewModel : ViewModelBase {
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
            var commitViewModel = parentViewModel as CommitViewModel;
            if (commitViewModel != null) {
                Initialize(commitViewModel);
            }
            else {
                var artifactsViewModel = parentViewModel as ArtifactsViewModel;
                if (artifactsViewModel != null)
                    Initialize(artifactsViewModel);
            }
        }
        void Initialize(CommitViewModel commit) {
            Artifact = commit.DownloadArtifacts();
            Initialize(Artifact);
        }
        void Initialize(ArtifactsViewModel artifact) {
            Modifications = new ModificationsViewModel(artifact);
            Build = new BuildLogViewModel(artifact);
            Tests = new TestLogViewModel(artifact);
        }
    }
}
