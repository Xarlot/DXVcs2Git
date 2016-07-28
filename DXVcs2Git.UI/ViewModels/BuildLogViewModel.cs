using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class BuildLogViewModel : BindableBase {
        public BuildLogViewModel() {
            Text = "text";
        }

        public string Text {
            get { return GetProperty(() => Text); }
            private set { SetProperty(() => Text, value); }
        }
    }
}
