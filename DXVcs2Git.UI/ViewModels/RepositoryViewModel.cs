using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Configuration;
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
        public TrackRepository TrackRepository { get; }
        public string DefaultServiceName { get { return RepoConfig?.DefaultServiceName; } }
        public BranchViewModel SelectedBranch {
            get { return GetProperty(() => SelectedBranch); }
            set { SetProperty(() => SelectedBranch, value); }
        }
        public RepositoryViewModel(string name, TrackRepository trackRepository, RepositoriesViewModel repositories) {
            TrackRepository = trackRepository;
            GitLabWrapper = new GitLabWrapper(TrackRepository.Server, TrackRepository.Token);
            GitReader = new GitReaderWrapper(trackRepository.LocalPath);
            Repositories = repositories;
            Project = GitLabWrapper.FindProject(GitReader.GetRemoteRepoPath());
            Name = name;
            FarmStatus = new FarmStatus();

            Update();
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
            if (Branches == null) {
                FarmStatus = null;
                return;
            }
            FarmStatus = FarmIntegrator.GetTaskStatus(RepoConfig?.FarmTaskName);
            Branches.ForEach(x => x.RefreshFarm());
        }
    }
}
