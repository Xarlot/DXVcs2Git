using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestViewModel : ViewModelBase {
        string serviceUser = "dxvcs2gitservice";
        new BranchViewModel Parameter { get { return (BranchViewModel)base.Parameter; } }
        EditBranchChangesViewModel Parent { get { return this.GetParentViewModel<EditBranchChangesViewModel>(); } }

        UserViewModel user;
        string comment;
        bool assignedToService;

        public ICommand ApplyMergeRequestCommand { get; private set; }
        public ICommand CancelMergeRequestCommand { get; private set; }
        public ICommand AssignToServiceCommand { get; private set; }
        public ICommand ResetCommand { get; private set; }

        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>("MessageBoxService", ServiceSearchMode.PreferLocal); } }

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
            return true;
        }
        void PerformAssignToService() {
            SelectedUser = new UserViewModel(Parameter.GetUser(this.serviceUser));
        }
        bool CanCancelMergeRequest() {
            return true;
        }
        void PerformCancelMergeRequest() {
            Parent.CancelMergeRequestChanges();
        }
        void PerformApplyMergeRequest() {
            Parent.ApplyMergeRequestChanges(new EditMergeRequestData() { Comment = Comment, Assignee = SelectedUser });
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
            this.comment = Parameter?.MergeRequest?.Title ?? Parameter?.Branch?.Commit?.Message;
            string assignee = Parameter?.MergeRequest?.Assignee;
            if (string.IsNullOrEmpty(assignee)) {
                this.assignedToService = false;
                this.user = null;
            }
            else {
                this.assignedToService = assignee == this.serviceUser;
                var registeredUser = Parameter?.GetUser(assignee);
                if (registeredUser != null)
                    this.user = new UserViewModel(registeredUser);
            }
            RaisePropertyChanged(null);
        }
    }
}
