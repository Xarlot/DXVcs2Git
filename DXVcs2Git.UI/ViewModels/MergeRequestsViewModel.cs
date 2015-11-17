using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class MergeRequestsViewModel : BindableBase {
        readonly GitLabWrapper gitLabWrapper;
        readonly GitReaderWrapper gitReader;
        BranchViewModel selectedBranch;

        public IEnumerable<BranchViewModel> Branches { get; private set; }
        public IEnumerable<Branch> ProtectedBranches { get; set; }
        public bool HasEditableMergeRequest {
            get { return GetProperty(() => HasEditableMergeRequest); }
            private set { SetProperty(() => HasEditableMergeRequest, value); }
        }
        public BranchViewModel SelectedBranch {
            get { return this.selectedBranch; }
            set { SetProperty(ref this.selectedBranch, value, () => SelectedBranch); }
        }

        public ICommand UpdateCommand { get; private set; }

        public MergeRequestsViewModel(GitLabWrapper gitLabWrapper, GitReaderWrapper gitReader) {
            this.gitReader = gitReader;
            this.gitLabWrapper = gitLabWrapper;
            UpdateCommand = DelegateCommandFactory.Create(Update, CanUpdate);

            Update();
        }
        public void Update() {
            Project project = gitLabWrapper.FindProject(this.gitReader.GetRemoteRepoPath());
            if (project == null) {
                Log.Error("Can`t find project");
                return;
            }

            var mergeRequests = this.gitLabWrapper.GetMergeRequests(project);
            var branches = this.gitLabWrapper.GetBranches(project).ToList();
            ProtectedBranches = branches.Where(x => x.Protected).ToList();
            Branches = branches.Where(x => !x.Protected)
                .Select(x => new BranchViewModel(gitLabWrapper, this, mergeRequests.FirstOrDefault(mr => mr.SourceBranch == x.Name), x)).ToList();
            SelectedBranch = Branches.FirstOrDefault();
            HasEditableMergeRequest = SelectedBranch.If(x => x.IsInEditingMergeRequest).ReturnSuccess();
        }
        bool CanUpdate() {
            return true;
        }
        public void AddMergeRequest(EditMergeRequestViewModel mergeRequest) {
            
        }
    }
}
