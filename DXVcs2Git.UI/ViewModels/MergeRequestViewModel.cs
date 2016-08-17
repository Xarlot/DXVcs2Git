using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class MergeRequestViewModel : BindableBase {
        public MergeRequest MergeRequest { get; }
        public IEnumerable<MergeRequestFileDataViewModel> Changes { get; private set; }
        public IEnumerable<CommitViewModel> Commits { get; }
        public BranchViewModel Branch { get; }
        public string Title { get; }
        public string Author { get; }
        public string SourceBranch { get; }
        public string TargetBranch { get; }
        public string Assignee { get; }
        public int? AssigneeId { get; }
        public int MergeRequestId => MergeRequest.Id;
        public MergeRequestViewModel(BranchViewModel branch, MergeRequest mergeRequest) {
            Branch = branch;
            MergeRequest = mergeRequest;
            Changes = branch.GetMergeRequestChanges(mergeRequest).Select(x => new MergeRequestFileDataViewModel(x)).ToList();
            Commits = branch.GetCommits(mergeRequest)
                .Select(commit => new CommitViewModel(commit, sha => branch.GetBuilds(mergeRequest, sha), x => branch.DownloadArtifacts(mergeRequest, x)))
                .ToList();
            Title = MergeRequest.Title;
            SourceBranch = MergeRequest.SourceBranch;
            TargetBranch = MergeRequest.TargetBranch;
            Author = MergeRequest.Author.Username;
            Assignee = MergeRequest.Assignee?.Username;
            AssigneeId = MergeRequest?.Assignee?.Id;
        }
    }
}
