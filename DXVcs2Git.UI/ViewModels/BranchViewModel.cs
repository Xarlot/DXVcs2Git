using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class BranchViewModel : BindableBase {
        readonly GitLabWrapper gitLabWrapper;
        public Branch Branch { get; }
        public MergeRequestsViewModel MergeRequests { get; }
        public string Name { get; }

        public bool HasMergeRequest { get; private set; }
        public ICommand CreateMergeRequestCommand { get; private set; }
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
            HasMergeRequest = MergeRequest != null;

            CreateMergeRequestCommand = DelegateCommandFactory.Create(CreateMergeRequest, CanCreateMergeRequest);
            EditMergeRequestCommand = DelegateCommandFactory.Create(EditMergeRequest, CanEditMergeRequest);
        }
        bool CanEditMergeRequest() {
            return HasMergeRequest && !IsInEditingMergeRequest;
        }
        void EditMergeRequest() {
            EditableMergeRequest = new EditMergeRequestViewModel(this);
        }
        bool CanCreateMergeRequest() {
            return !HasMergeRequest;
        }
        public void CreateMergeRequest() {
            //this.gitLabWrapper.CreateMergeRequest();
        }
        public void CloseMergeRequest() {
            this.gitLabWrapper.CloseMergeRequest(MergeRequest.MergeRequest);
            MergeRequests.Update();
        }
    }
}
