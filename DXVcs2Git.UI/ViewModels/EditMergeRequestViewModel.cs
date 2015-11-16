using System.ComponentModel;
using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestViewModel : BindableBase, IDataErrorInfo {
        BranchViewModel model;

        public EditMergeRequestViewModel(BranchViewModel model) {
            this.model = model;
        }

        public bool IsModified { get; private set; }
        public string Title {
            get { return GetProperty(() => Title); }
            set { SetProperty(() => Title, value, () => IsModified = true); }
        }
        public string Description {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value, () => IsModified = true); }
        }
        public string this[string columnName] {
            get {
                if (columnName == "Title")
                    return string.IsNullOrEmpty(Title) ? "error" : null;
                if (columnName == "Description")
                    return string.IsNullOrEmpty(Description) ? "error" : null;
                return null;
            }
        }
        public string Error { get; }
    }
}
