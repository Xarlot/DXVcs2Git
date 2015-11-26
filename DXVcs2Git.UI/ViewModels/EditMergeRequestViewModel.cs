using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestViewModel : ViewModelBase {
        string serviceUser = "dxvcs2gitservice";
        public BranchViewModel Parent { get { return this.GetParentViewModel<BranchViewModel>(); } }

        UserViewModel user;
        string comment;
        bool assignedToService;

        public ICommand ApplyMergeRequestCommand { get; private set; }
        public ICommand CancelMergeRequestCommand { get; private set; }
        public ICommand AssignToServiceCommand { get; private set; }
        public ICommand ResetCommand { get; private set; }

        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>("ruSure", ServiceSearchMode.PreferLocal); } }

        public string Comment {
            get { return this.comment; }
            set { SetProperty(ref this.comment, value, () => Comment, Invalidate); }
        }
        public bool AssignedToService {
            get { return this.assignedToService; }
            set { SetProperty(ref this.assignedToService, value, () => AssignedToService, Invalidate); }
        }
        public UserViewModel SelectedUser {
            get { return user; }
            set {
                SetProperty(ref user, value, () => SelectedUser, () => {
                    Invalidate();
                    AssignedToService = value?.Name == this.serviceUser;
                });
            }
        }

        public EditMergeRequestViewModel() {
            ApplyMergeRequestCommand = DelegateCommandFactory.Create(PerformApplyMergeRequest, CanApplyMergeRequest);
            CancelMergeRequestCommand = DelegateCommandFactory.Create(PerformCancelMergeRequest, CanCancelMergeRequest);
            AssignToServiceCommand = DelegateCommandFactory.Create(PerformAssignToService, CanAssignToService);
            ResetCommand = DelegateCommandFactory.Create(Reset);
        }
        void Invalidate() {
            IsModified = true;
            CommandManager.InvalidateRequerySuggested();
        }
        bool CanAssignToService() {
            return IsModified;
        }
        void PerformAssignToService() {
            SelectedUser = new UserViewModel(Parent.GetUser(this.serviceUser));
        }
        bool CanCancelMergeRequest() {
            return true;
        }
        void PerformCancelMergeRequest() {
            if (MessageBoxService.Show("Are you sure?", "Apply merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Parent.CancelMergeRequestChanges();
            }
        }
        void PerformApplyMergeRequest() {
            if (MessageBoxService.Show("Are you sure?", "Apply merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Parent.ApplyMergeRequestChanges(this);
            }
        }
        bool CanApplyMergeRequest() {
            return IsModified;
        }
        public bool IsModified { get; private set; }

        protected override void OnParentViewModelChanged(object parentViewModel) {
            base.OnParentViewModelChanged(parentViewModel);
            Refresh();
        }
        public void Reset() {
            this.assignedToService = false;
            this.comment = string.Empty;
            IsModified = false;
        }
        public void Refresh() {
            this.comment = Parent?.MergeRequest?.Title ?? Parent?.Branch?.Commit?.Message;
        }
    }
}
