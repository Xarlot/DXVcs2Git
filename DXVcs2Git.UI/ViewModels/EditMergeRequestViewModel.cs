using System.ComponentModel;
using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestViewModel : BindableBase, IDataErrorInfo {
        BranchViewModel model;

        public EditMergeRequestViewModel(BranchViewModel model) {
            this.model = model;
        }

        public bool IsModified { get; private set; }
        public string Comment {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value, () => IsModified = true); }
        }
        public string this[string columnName] {
            get {
                if (columnName == "Title")
                    return string.IsNullOrEmpty(Comment) ? "error" : null;
                return null;
            }
        }
        public string Error { get; }
    }
}
