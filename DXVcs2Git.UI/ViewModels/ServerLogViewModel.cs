using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class ServerLogViewModel : BindableBase {
        ArtifactsViewModel model;
        public ServerLogViewModel(ArtifactsViewModel model) {
            this.model = model;
            Text = model.HasTrace ? model.Trace : "Text";
        }

        public string Text {
            get { return GetProperty(() => Text); }
            private set { SetProperty(() => Text, value); }
        }
    }
}

