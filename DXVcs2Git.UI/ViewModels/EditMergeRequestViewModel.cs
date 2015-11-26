using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestViewModel : ViewModelBase {
        string serviceUser = "dxvcs2gitservice";
        public BranchViewModel Branch { get { return (BranchViewModel)Parameter; } }
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
            SelectedUser = new UserViewModel(Branch.GetUser(this.serviceUser));
        }
        bool CanCancelMergeRequest() {
            return true;
        }
        void PerformCancelMergeRequest() {
            if (MessageBoxService.Show("Are you sure?", "Apply merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Branch.CancelMergeRequestChanges();
            }
        }
        void PerformApplyMergeRequest() {
            if (MessageBoxService.Show("Are you sure?", "Apply merge request", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Branch.ApplyMergeRequestChanges(this);
            }
        }
        bool CanApplyMergeRequest() {
            return IsModified;
        }
        public bool IsModified { get; private set; }

        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            Refresh();
        }
        public void Reset() {
            this.assignedToService = false;
            this.comment = string.Empty;
            user = null;
            assignedToService = false;
            IsModified = false;
        }
        public void Refresh() {
            this.comment = Branch?.MergeRequest?.Title ?? Branch?.Branch?.Commit?.Message;
            string assignee = Branch?.MergeRequest?.Assignee;
            if (string.IsNullOrEmpty(assignee)) {
                this.assignedToService = false;
                this.user = null;
            }
            else {
                this.assignedToService = assignee == this.serviceUser;
                var registeredUser = Branch?.GetUser(assignee);
                if (registeredUser != null)
                    this.user = new UserViewModel(registeredUser);
            }
        }
    }
}
