using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class RepositoryViewModel : BindableBase {
        GitLabWrapper GitLabWrapper { get; }
        GitReaderWrapper GitReader { get; }
        public IEnumerable<BranchViewModel> Branches {
            get { return GetProperty(() => Branches); }
            private set { SetProperty(() => Branches, value); }
        }
        public string Name { get; }
        Project Project { get; }
        MergeRequestsViewModel MergeRequests { get; }
        public RepositoryViewModel(string name, GitLabWrapper gitLabWrapper, GitReaderWrapper gitReader, MergeRequestsViewModel mergeRequests) {
            Name = name;
            GitLabWrapper = gitLabWrapper;
            GitReader = gitReader;
            MergeRequests = mergeRequests;
            Project = gitLabWrapper.FindProject(GitReader.GetRemoteRepoPath());
        }

        public void Update() {
            if (Project == null) {
                Log.Error("Can`t find project");
                return;
            }

            var mergeRequests = this.GitLabWrapper.GetMergeRequests(Project);
            var branches = this.GitLabWrapper.GetBranches(Project).ToList();
            var localBranches = GitReader.GetLocalBranches();

            Branches = branches.Where(x => !x.Protected && localBranches.Any(local => local.FriendlyName == x.Name))
                .Select(x => new BranchViewModel(GitLabWrapper, GitReader, MergeRequests, mergeRequests.FirstOrDefault(mr => mr.SourceBranch == x.Name), x)).ToList();
        }
    }
}
