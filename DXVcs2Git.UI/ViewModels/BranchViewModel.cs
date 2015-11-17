using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class BranchViewModel : BindableBase {
        GitLabWrapper gitLabWrapper;
        public Branch Branch { get; }
        public MergeRequestsViewModel MergeRequests { get; }
        public string Name { get; }

        public bool HasMergeRequest { get; private set; }
        public ICommand CreateNewMergeRequestCommand { get; private set; }
        public ICommand EditMergeRequestCommand { get; private set; }
        public MergeRequestViewModel MergeRequest { get; private set; }
        public bool IsInEditingMergeRequest {
            get { return GetProperty(() => IsInEditingMergeRequest); }
            internal set { SetProperty(() => IsInEditingMergeRequest, value); }
        }
        public EditMergeRequestViewModel EditableMergeRequest {
            get { return GetProperty(() => EditableMergeRequest); }
            private set { SetProperty(() => EditableMergeRequest, value); }
        }
        public BranchViewModel(GitLabWrapper gitLabWrapper, MergeRequestsViewModel mergeRequests, MergeRequest mergeRequest, Branch branch) {
            this.gitLabWrapper = gitLabWrapper;
            Branch = branch;
            Name = branch.Name;
            MergeRequests = mergeRequests;
            MergeRequest = mergeRequest.With(x => new MergeRequestViewModel(gitLabWrapper, mergeRequest));
            EditableMergeRequest = MergeRequests.With(x => new EditMergeRequestViewModel(this));
            HasMergeRequest = MergeRequests != null;

            CreateNewMergeRequestCommand = DelegateCommandFactory.Create(CreateNewMergeRequest, CanCreateNewMergeRequest);
            EditMergeRequestCommand = DelegateCommandFactory.Create(EditMergeRequest, CanEditMergeRequest);
        }
        bool CanEditMergeRequest() {
            return !IsInEditingMergeRequest;
        }
        void EditMergeRequest() {
            EditableMergeRequest = new EditMergeRequestViewModel(this);
        }
        bool CanCreateNewMergeRequest() {
            return !HasMergeRequest && !IsInEditingMergeRequest;
        }
        public void CreateNewMergeRequest() {
            //this.gitLabWrapper.CreateMergeRequest();
        }
    }
}
