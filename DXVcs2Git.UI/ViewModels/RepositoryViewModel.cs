using System.Collections.Generic;
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
        protected bool Equals(RepositoryViewModel other) {
            return this.Origin?.Id == other?.Origin?.Id && this.Upstream?.Id == other?.Upstream?.Id;
        }
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((RepositoryViewModel)obj);
        }
        public override int GetHashCode() {
            return 0;
        }
        GitLabWrapper GitLabWrapper { get; }
        GitReaderWrapper GitReader { get; }
        public IEnumerable<BranchViewModel> Branches {
            get { return GetProperty(() => Branches); }
            private set { SetProperty(() => Branches, value); }
        }
        public string Name { get; }
        public Project Origin { get; }
        public Project Upstream { get; }
        RepositoriesViewModel Repositories { get; }
        public RepoConfig RepoConfig { get; private set; }
        public TrackRepository TrackRepository { get; }
        public bool HasAdminPrivileges { get; }
        public string DefaultServiceName => RepoConfig?.DefaultServiceName;

        public BranchViewModel SelectedBranch {
            get { return GetProperty(() => SelectedBranch); }
            set { SetProperty(() => SelectedBranch, value); }
        }
        public RepositoryViewModel(string name, TrackRepository trackRepository, RepositoriesViewModel repositories) {
            TrackRepository = trackRepository;
            GitLabWrapper = new GitLabWrapper(TrackRepository.Server, TrackRepository.Token);
            HasAdminPrivileges = GitLabWrapper.IsAdmin();
            GitReader = new GitReaderWrapper(trackRepository.LocalPath);
            UpdateConfigs(trackRepository, repositories);
            Repositories = repositories;
            Origin = GitLabWrapper.FindProject(GitReader.GetOriginRepoPath());
            Upstream = GitLabWrapper.FindProject(GitReader.GetUpstreamRepoPath());
            Name = name;
            Update();
        }
        void UpdateConfigs(TrackRepository trackRepository, RepositoriesViewModel repositories) {
            RepoConfig = repositories.RepoConfigs[trackRepository.ConfigName];
        }
        public void Update() {
            if (Origin == null) {
                Log.Error("Can`t find project");
                return;
            }

            var branches = this.GitLabWrapper.GetBranches(Origin).ToList();
            var localBranches = GitReader.GetLocalBranches();

            Branches = branches.Where(x => !x.Protected && localBranches.Any(local => local.FriendlyName == x.Name))
                .Select(x => new RepositoryBranchViewModel(GitLabWrapper, this, x.Name)).ToList();
        }
        public void ForceBuild() {
            FarmIntegrator.ForceBuild(RepoConfig.FarmSyncTaskName);
        }
        public MergeRequest CreateMergeRequest(string title, string description, string user, string sourceBranch, string targetBranch) {
            return GitLabWrapper.CreateMergeRequest(Origin, Upstream, title, description, user, sourceBranch, targetBranch);
        }
        public void RefreshFarm() {
            Branches.ForEach(x => x.RefreshFarm());
        }
    }
}
