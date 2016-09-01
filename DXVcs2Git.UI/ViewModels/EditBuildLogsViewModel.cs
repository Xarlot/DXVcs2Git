using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class EditBuildLogsViewModel : ViewModelBase {
        public EditBuildLogsViewModel() {

        }

        public XmlLogViewModel XmlLog {
            get { return GetProperty(() => XmlLog); }
            private set { SetProperty(() => XmlLog, value); }
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
        public BuildLogViewModel BuildLog {
            get { return GetProperty(() => BuildLog); }
            private set { SetProperty(() => BuildLog, value); }
        }
        public ServerLogViewModel ServerLog {
            get { return GetProperty(() => ServerLog); }
            private set { SetProperty(() => ServerLog, value); }
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
            XmlLog = new XmlLogViewModel(artifact);
            BuildLog = new BuildLogViewModel(artifact);
            Tests = new TestLogViewModel(artifact);
            ServerLog = new ServerLogViewModel(artifact);
        }
    }
}
