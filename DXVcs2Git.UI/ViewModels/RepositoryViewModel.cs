using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using DXVcs2Git.UI.Farm;
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
        public FarmStatus FarmStatus {
            get { return GetProperty(() => FarmStatus); }
            private set { SetProperty(() => FarmStatus, value); }
        }
        public Project Project { get; }
        RepositoriesViewModel Repositories { get; }
        public GitRepoConfig RepoConfig { get; }
        public BranchViewModel SelectedBranch {
            get { return GetProperty(() => SelectedBranch); }
            set { SetProperty(() => SelectedBranch, value); }
        }
        public RepositoryViewModel(string name, GitLabWrapper gitLabWrapper, GitReaderWrapper gitReader, RepositoriesViewModel repositories) {
            GitLabWrapper = gitLabWrapper;
            GitReader = gitReader;
            Repositories = repositories;
            Project = gitLabWrapper.FindProject(GitReader.GetRemoteRepoPath());
            RepoConfig = CreateRepoConfig();
            Name = name ?? RepoConfig?.Name;
            FarmStatus = new FarmStatus();

            Update();
        }
        GitRepoConfig CreateRepoConfig() {
            string localRepoPath = GitReader.GetLocalRepoPath();
            GitRepoConfig repoConfig = Serializer.Deserialize<GitRepoConfig>(Path.Combine(localRepoPath, GitRepoConfig.ConfigFileName));
            return repoConfig;
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
                .Select(x => new BranchViewModel(GitLabWrapper, GitReader, Repositories, this, mergeRequests.FirstOrDefault(mr => mr.SourceBranch == x.Name), x)).ToList();
        }
        public void Refresh() {
            if (Branches == null)
                return;
            RefreshFarm();
            Branches.ForEach(x => x.Refresh());
        }
        public void ForceBuild() {
            FarmIntegrator.ForceBuild(RepoConfig.FarmSyncTaskName);
        }
        public MergeRequest CreateMergeRequest(string title, string description, string user, string sourceBranch, string targetBranch) {
            return GitLabWrapper.CreateMergeRequest(Project, title, description, user, sourceBranch, targetBranch);
        }
        public void RefreshFarm() {
            FarmStatus = FarmIntegrator.GetTaskStatus(RepoConfig?.FarmTaskName);
        }
    }
}
