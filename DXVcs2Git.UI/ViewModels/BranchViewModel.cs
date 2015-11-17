using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class BranchViewModel : BindableBase {
        readonly GitLabWrapper gitLabWrapper;
        readonly GitReaderWrapper gitReader;
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
        public BranchViewModel(GitLabWrapper gitLabWrapper, GitReaderWrapper gitReader, MergeRequestsViewModel mergeRequests, MergeRequest mergeRequest, Branch branch) {
            this.gitLabWrapper = gitLabWrapper;
            this.gitReader = gitReader;
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
            var message = Branch.Commit.Message;
            string title = CalcMergeRequestTitle(message);
            string description = CalcMergeRequestDescription(message);
            string targetBranch = CalcTargetBranch(Branch.Name);
            if (targetBranch == null)
                return;
            var mergeRequest = this.gitLabWrapper.CreateMergeRequest(MergeRequests.Project, title, description, null, Branch.Name, targetBranch);
            MergeRequest = new MergeRequestViewModel(this.gitLabWrapper, mergeRequest);
            HasMergeRequest = true;

            EditMergeRequest();
        }
        string CalcTargetBranch(string name) {
            return MergeRequests.ProtectedBranches.FirstOrDefault(x => name.StartsWith(x.Name)).With(x => x.Name);
        }
        string CalcMergeRequestDescription(string message) {
            var changes = message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            changes.Skip(1).ForEach(x => sb.AppendLine(x.ToString()));
            return sb.ToString();
        }
        string CalcMergeRequestTitle(string message) {
            var changes = message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var title = changes.FirstOrDefault();
            return title;
        }
        public void CloseMergeRequest() {
            this.gitLabWrapper.CloseMergeRequest(MergeRequest.MergeRequest);
            EditableMergeRequest = null;
            IsInEditingMergeRequest = false;
            MergeRequest = null;
            HasMergeRequest = false;
        }
        public void ApplyMergeRequest(EditMergeRequestViewModel newMergeRequest) {
            if (MergeRequest != null) {
                this.gitLabWrapper.UpdateMergeRequestTitleAndDescription(
                    MergeRequest.MergeRequest, CalcMergeRequestTitle(newMergeRequest.Comment), CalcMergeRequestDescription(newMergeRequest.Comment));
            }
        }
        public void CancelMergeRequest() {
            EditableMergeRequest = null;
            IsInEditingMergeRequest = false;
            MergeRequests.Update();
        }
    }
}
