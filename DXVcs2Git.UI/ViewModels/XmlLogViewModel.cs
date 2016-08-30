using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class XmlLogViewModel : BindableBase {
        ArtifactsViewModel model;
        public XmlLogViewModel(ArtifactsViewModel model) {
            this.model = model;
            Text = model.HasContent ? model.WorkerLog : "Text";
        }

        public string Text {
            get { return GetProperty(() => Text); }
            private set { SetProperty(() => Text, value); }
        }
    }
}
