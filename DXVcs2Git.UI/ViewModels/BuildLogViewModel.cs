using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class BuildLogViewModel : BindableBase {
        ArtifactsViewModel model;
        public BuildLogViewModel(ArtifactsViewModel model) {
            this.model = model;
            Text = model.HasContent ? model.BuildLog : "Text";
        }

        public string Text {
            get { return GetProperty(() => Text); }
            private set { SetProperty(() => Text, value); }
        }

    }
}
