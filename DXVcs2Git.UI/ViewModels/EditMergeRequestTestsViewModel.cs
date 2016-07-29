using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Practices.ServiceLocation;
using NGitLab;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestTestsViewModel : ViewModelBase {
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();

        public ICommand CancelTestsCommand { get; }
        public ICommand ShowLogCommand { get; }

        IWindowService ShowLogsService => GetService<IWindowService>();

        public IEnumerable<CommitViewModel> Commits {
            get { return GetProperty(() => Commits); }
            private set { SetProperty(() => Commits, value); }
        }

        public EditMergeRequestTestsViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            CancelTestsCommand = DelegateCommandFactory.Create(PerformCancelTests, CanPerformCancelTests);
            ShowLogCommand = DelegateCommandFactory.Create<CommitViewModel>(PerformShowLogs, CanPerformShowLogs);

            Initialize();
        }
        bool CanPerformShowLogs(CommitViewModel model) {
            if (model == null)
                return false;
            var buildStatus = model.Build?.BuildStatus;
            return buildStatus == BuildStatus.failed || buildStatus == BuildStatus.success;
        }
        void PerformShowLogs(CommitViewModel model) {
            ShowLogsService.Show(model);
        }
        bool CanPerformCancelTests() {
            return false;
        }
        void PerformCancelTests() {
        }
        BranchViewModel BranchViewModel { get; set; }
        void OnMessageReceived(Message msg) {
            if (msg.MessageType == MessageType.RefreshFarm) {
                RefreshFarmStatus();
                return;
            }
            if (msg.MessageType == MessageType.RefreshSelectedBranch)
                RefreshSelectedBranch();
        }
        void RefreshSelectedBranch() {
            BranchViewModel = RepositoriesViewModel.SelectedBranch;
            if (BranchViewModel?.MergeRequest == null) {
                Commits = Enumerable.Empty<CommitViewModel>();
                return;
            }
            var mergeRequest = BranchViewModel.MergeRequest;
            Commits = BranchViewModel.GetCommits(mergeRequest.MergeRequest)
                .Select(commit => new CommitViewModel(commit, sha => BranchViewModel.GetBuilds(mergeRequest.MergeRequest, sha), x => BranchViewModel.DownloadArtifacts(mergeRequest.MergeRequest, x)))
                .ToList();
            Commits.ForEach(x => x.UpdateBuilds());
        }
        void RefreshFarmStatus() {
        }
        void Initialize() {
            RefreshSelectedBranch();
            RefreshFarmStatus();
        }
        bool CanPerformRunTests() {
            return BranchViewModel?.MergeRequest != null;
        }
        void PerformRunTests() {
        }
    }

    public class CommitViewModel : BindableBase {
        readonly Commit commit;
        readonly Func<Build, byte[]> downloadArtifactsHandler;
        readonly Func<Sha1, IEnumerable<Build>> getBuildsHandler;
        public string Id { get; }
        public string Title {
            get { return GetProperty(() => Title); }
            private set { SetProperty(() => Title, value); }
        }
        public BuildViewModel Build {
            get { return GetProperty(() => Build); }
            private set { SetProperty(() => Build, value); }
        }
        public CommitViewModel(Commit commit, Func<Sha1, IEnumerable<Build>> getBuildsHandler, Func<Build, byte[]> downloadArtifactsHandler) {
            this.commit = commit;
            this.downloadArtifactsHandler = downloadArtifactsHandler;
            this.getBuildsHandler = getBuildsHandler;
            Title = commit.Title;
            Id = commit.ShortId;
        }
        public void UpdateBuilds() {
            var builds = getBuildsHandler(commit.Id);
            Build = builds.Select(x => new BuildViewModel(x)).FirstOrDefault();
        }
        public ArtifactsViewModel DownloadArtifacts() {
            if (Build == null)
                return null;
            return new ArtifactsViewModel(Build.Artifacts, downloadArtifactsHandler(Build.Build));
        }
    }

    public class BuildViewModel : BindableBase {
        public int Id => Build.Id;
        public BuildStatus BuildStatus { get; }
        public string Duration { get; }
        public ArtifactsFile Artifacts => Build.ArtifactsFile;
        public Build Build { get; }
        public BuildViewModel(Build build) {
            Build = build;
            BuildStatus = build.Status ?? BuildStatus.undefined;
            if (build.StartedAt != null && (BuildStatus == BuildStatus.success || BuildStatus == BuildStatus.failed)) {
                Duration = ((build.FinishedAt ?? DateTime.Now) - build.StartedAt.Value).ToString("g");
            }
            else
                Duration = string.Empty;
        }
    }
}
