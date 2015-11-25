using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestViewModel : ViewModelBase {
        public EditSelectedRepositoryViewModel Parent { get { return this.GetParentViewModel<EditSelectedRepositoryViewModel>(); } }

        UserViewModel user;
        string comment;

        public ICommand ApplyMergeRequestCommand { get; private set; }
        public ICommand CancelMergeRequestCommand { get; private set; }
        public ICommand AssignToServiceCommand { get; private set; }

        public bool HasChanges { get { return false; } }
        public EditMergeRequestViewModel() {

            ApplyMergeRequestCommand = DelegateCommandFactory.Create(PerformApplyMergeRequest, CanApplyMergeRequest);
            CancelMergeRequestCommand = DelegateCommandFactory.Create(PerformCancelMergeRequest, CanCancelMergeRequest);
            AssignToServiceCommand = DelegateCommandFactory.Create(PerformAssignToService, CanAssignToService);
//            comment = model.MergeRequest?.MergeRequest.Title ?? model.Branch.Commit.Message;
        }
        bool CanAssignToService() {
            return HasChanges;
        }
        void PerformAssignToService() {
            SelectedUser = new UserViewModel(Parent.SelectedBranch.GetUser("dxvcs2gitservice"));
        }
        bool CanCancelMergeRequest() {
            return true;
        }
        void PerformCancelMergeRequest() {
//            this.model.CancelMergeRequest();
        }
        void PerformApplyMergeRequest() {
            if (DXMessageBox.Show(null, "Are you sure?", "Apply merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
//                this.model.ApplyMergeRequest(this);
            }
        }
        bool CanApplyMergeRequest() {
            return IsModified;
        }

        public bool IsModified { get; private set; }
        public UserViewModel SelectedUser {
            get { return user; }
            set {
                SetProperty(ref user, value, () => SelectedUser, () => {
                    IsModified = true;
                    CommandManager.InvalidateRequerySuggested();
                });
            }
        }
        public string Comment {
            get { return this.comment; }
            set {
                SetProperty(ref comment, value, () => Comment, () => {
                    IsModified = true;
                    CommandManager.InvalidateRequerySuggested();
                });
            }
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
