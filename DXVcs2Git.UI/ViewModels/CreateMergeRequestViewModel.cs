using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class CreateMergeRequestViewModel : BindableBase {
        public string Description {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }
    }
}
