using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class NewMergeRequestViewModel : BindableBase {
        BranchViewModel model;

        public NewMergeRequestViewModel(BranchViewModel model) {
            this.model = model;
        }

        public string Title {
            get { return GetProperty(() => Title); }
            set { SetProperty(() => Title, value); }
        }
        public string Description {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }
    }
}
