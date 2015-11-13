using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DXVcs2Git.Core;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class MergeRequestsViewModel : BindableBase {
        GitLabWrapper gitLabWrapper;
        string repo;
        MergeRequestViewModel selectedMergeRequest;
        public IEnumerable<MergeRequestViewModel> MergeRequests { get; private set; }
        public MergeRequestViewModel SelectedMergeRequest {
            get { return this.selectedMergeRequest; }
            set { SetProperty(ref this.selectedMergeRequest, value, () => SelectedMergeRequest); }
        }

        public MergeRequestsViewModel(GitLabWrapper gitLabWrapper, string repo) {
            this.repo = repo;
            this.gitLabWrapper = gitLabWrapper;
            Update();
        }

        public void Update() {
            Project project = gitLabWrapper.FindProject(repo);
            if (project == null) {
                Log.Error("Can`t find project");
                return;
            }

            var mergeRequests = gitLabWrapper.GetMergeRequests(project);
            MergeRequests = mergeRequests.Select(x => new MergeRequestViewModel(x)).ToList();
        }
    }
}
