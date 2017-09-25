using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.GitLab;
using Microsoft.Practices.ServiceLocation;
using NGitLab;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestTestsViewModel : ViewModelBase {
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();

        public ICommand CancelTestsCommand { get; }
        public ICommand ShowLogCommand { get; }
        public ICommand ForceTestCommand { get; }
        public ICommand AbortTestCommand { get; }
        public ICommand UseCommitDescriptionCommand { get; }

        IWindowService ShowLogsService => GetService<IWindowService>();

        public IEnumerable<CommitViewModel> Commits {
            get { return GetProperty(() => Commits); }
            private set { SetProperty(() => Commits, value); }
        }

        public EditMergeRequestTestsViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            CancelTestsCommand = DelegateCommandFactory.Create(PerformCancelTests, CanPerformCancelTests);
            ShowLogCommand = DelegateCommandFactory.Create<CommitViewModel>(PerformShowLogs, CanPerformShowLogs);
            ForceTestCommand = DelegateCommandFactory.Create<CommitViewModel>(PerformForceTest, CanPerformForceTest);
            AbortTestCommand = DelegateCommandFactory.Create<CommitViewModel>(PerformAbortTest, CanPerformAbortTest);
            UseCommitDescriptionCommand = DelegateCommandFactory.Create<CommitViewModel>(UseCommitDescription, CanUseCommitDescription);

            Initialize();
        }
        void PerformForceTest(CommitViewModel commit) {
            var actualCommit = commit ?? BranchViewModel.MergeRequest.Commits.FirstOrDefault();

            BranchViewModel.ForceBuild(BranchViewModel.MergeRequest.MergeRequest, actualCommit?.Build.Build);
        }
        bool CanPerformForceTest(CommitViewModel commit) {
            if (BranchViewModel?.MergeRequest == null)
                return false;
            var actualCommit = commit ?? BranchViewModel.MergeRequest.Commits.FirstOrDefault();
            if (actualCommit == null)
                return false;
            return actualCommit.Build?.BuildStatus != JobStatus.pending && actualCommit.Build?.BuildStatus != JobStatus.running;
        }
        bool CanPerformAbortTest(CommitViewModel commit) {
            if (BranchViewModel?.MergeRequest == null)
                return false;
            var actualCommit = commit ?? BranchViewModel.MergeRequest.Commits.FirstOrDefault();
            if (actualCommit == null)
                return false;
            return actualCommit.Build?.BuildStatus == JobStatus.pending || actualCommit.Build?.BuildStatus == JobStatus.running;
        }
        void PerformAbortTest(CommitViewModel commit) {
            var actualCommit = commit ?? BranchViewModel.MergeRequest.Commits.FirstOrDefault();

            BranchViewModel.AbortBuild(BranchViewModel.MergeRequest.MergeRequest, actualCommit?.Build.Build);
        }
        bool CanPerformShowLogs(CommitViewModel model) {
            if (model == null)
                return false;
            var buildStatus = model.Build?.BuildStatus;
            return buildStatus == JobStatus.failed || buildStatus == JobStatus.success;
        }
        void PerformShowLogs(CommitViewModel model) {
            ShowLogsService.Show(model);
        }
        bool CanPerformCancelTests() {
            return false;
        }
        void PerformCancelTests() {
        }
        bool CanUseCommitDescription(CommitViewModel commit) {
            return commit != null && BranchViewModel?.MergeRequest != null;
        }
        void UseCommitDescription(CommitViewModel commit) {
            BranchViewModel.UpdateMergeRequest(commit.Title, BranchViewModel.MergeRequest.MergeRequest.Description, BranchViewModel.MergeRequest.Assignee);
            RepositoriesViewModel.RaiseRefreshSelectedBranch();
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
            Commits = mergeRequest.Commits;
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

    public class LoadLogViewModel : BindableBase {
        public string Url {
            get { return GetProperty(() => Url); }
            set { SetProperty(() => Url, value); }
        }
    }

    public class CommitViewModel : BindableBase {
        readonly Commit commit;
        readonly Func<Build, byte[]> downloadArtifactsHandler;
        readonly Func<Build, byte[]> downloadTraceHandler;
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
        public CommitViewModel(Commit commit, Func<Sha1, IEnumerable<Build>> getBuildsHandler, Func<Build, byte[]> downloadArtifactsHandler, Func<Build, byte[]> downloadTraceHandler) {
            this.commit = commit;
            this.downloadArtifactsHandler = downloadArtifactsHandler;
            this.downloadTraceHandler = downloadTraceHandler;
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
            return new ArtifactsViewModel(Build.Artifacts, downloadArtifactsHandler(Build.Build), downloadTraceHandler(Build.Build));
        }
    }

    public class BuildViewModel : BindableBase {
        public int Id => Build.Id;
        public JobStatus BuildStatus { get; }
        public string Duration { get; }
        public ArtifactsFile Artifacts => Build.ArtifactsFile;
        public Build Build { get; }
        public BuildViewModel(Build build) {
            Build = build;
            BuildStatus = build.Status ?? JobStatus.undefined;
            if (build.StartedAt != null && (BuildStatus == JobStatus.success || BuildStatus == JobStatus.failed)) {
                Duration = ((build.FinishedAt ?? DateTime.Now) - build.StartedAt.Value).ToString("g");
            }
            else
                Duration = string.Empty;
        }
    }
}
