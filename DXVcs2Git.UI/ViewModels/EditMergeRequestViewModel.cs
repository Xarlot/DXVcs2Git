using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestViewModel : BindableBase, IDataErrorInfo {
        readonly BranchViewModel model;
        
        public ICommand CloseMergeRequestCommand { get; private set; }
        public ICommand ApplyMergeRequestCommand { get; private set; }
        public ICommand CancelMergeRequestCommand { get; private set; }
        
        public EditMergeRequestViewModel(BranchViewModel model) {
            this.model = model;
            CloseMergeRequestCommand = DelegateCommandFactory.Create(PerformCloseMergeRequest);
            ApplyMergeRequestCommand = DelegateCommandFactory.Create(PerformApplyMergeRequest, CanApplyMergeRequest);
            CancelMergeRequestCommand = DelegateCommandFactory.Create(PerformCancelMergeRequest, CanCancelMergeRequest);

            model.IsInEditingMergeRequest = true;

        }
        bool CanCancelMergeRequest() {
            return true;
        }
        void PerformCancelMergeRequest() {
            this.model.CancelMergeRequest();
        }
        void PerformCloseMergeRequest() {
            if (DXMessageBox.Show(null, "Are you sure?", "Close merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                this.model.CloseMergeRequest();
        }
        void PerformApplyMergeRequest() {
            if (DXMessageBox.Show(null, "Are you sure?", "Apply merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                this.model.ApplyMergeRequest(this);
            }
        }
        bool CanApplyMergeRequest() {
            return IsModified;
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
