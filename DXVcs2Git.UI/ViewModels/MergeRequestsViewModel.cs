using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class MergeRequestsViewModel : BindableBase {
        ObservableCollection<MergeRequestViewModel> mergeRequests;
        readonly GitLabWrapper gitLabWrapper;
        readonly string repo;
        MergeRequestViewModel selectedMergeRequest;
        public ObservableCollection<MergeRequestViewModel> MergeRequests {
            get { return this.mergeRequests; }
            private set { SetProperty(ref this.mergeRequests, value, () => MergeRequests); }
        }
        public MergeRequestViewModel SelectedMergeRequest {
            get { return this.selectedMergeRequest; }
            set { SetProperty(ref this.selectedMergeRequest, value, () => SelectedMergeRequest); }
        }
        public IEnumerable<BranchViewModel> Branches { get; private set; }
        public ICommand UpdateCommand { get; private set; }

        public MergeRequestsViewModel(GitLabWrapper gitLabWrapper, string repo) {
            this.repo = repo;
            this.gitLabWrapper = gitLabWrapper;
            UpdateCommand = DelegateCommandFactory.Create(Update, CanUpdate);

            Update();
        }
        public void Update() {
            Project project = gitLabWrapper.FindProject(repo);
            if (project == null) {
                Log.Error("Can`t find project");
                return;
            }

            MergeRequests = new ObservableCollection<MergeRequestViewModel>(gitLabWrapper.GetMergeRequests(project).Select(x => new MergeRequestViewModel(gitLabWrapper, x)).ToList());
            SelectedMergeRequest = SelectedMergeRequest != null && MergeRequests.Contains(SelectedMergeRequest) ? SelectedMergeRequest : MergeRequests.FirstOrDefault();

            var branches = this.gitLabWrapper.GetBranches(project);
            Branches = branches.Select(x => new BranchViewModel(this, x)).ToList();
        }
        bool CanUpdate() {
            return true;
        }
        public void AddMergeRequest(EditMergeRequestViewModel mergeRequest) {
            
        }
    }
}
