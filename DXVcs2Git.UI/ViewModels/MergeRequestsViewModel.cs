using System.Collections;
using System.Collections.Generic;
using DevExpress.Mvvm;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class MergeRequestsViewModel : BindableBase {
        GitLabWrapper gitLabWrapper;
        public IEnumerable<MergeRequestViewModel> MergeRequests { get; }

        public MergeRequestsViewModel(GitLabWrapper gitLabWrapper) {
            this.gitLabWrapper = gitLabWrapper;
        }

        public void Update() {
        }
    }
}
