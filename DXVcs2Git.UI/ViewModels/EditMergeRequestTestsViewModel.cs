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
            return actualCommit.Build?.BuildStatus != PipelineStatus.pending && actualCommit.Build?.BuildStatus != PipelineStatus.running;
        }
        bool CanPerformAbortTest(CommitViewModel commit) {
            if (BranchViewModel?.MergeRequest == null)
                return false;
            var actualCommit = commit ?? BranchViewModel.MergeRequest.Commits.FirstOrDefault();
            if (actualCommit == null)
                return false;
            return actualCommit.Build?.BuildStatus == PipelineStatus.pending || actualCommit.Build?.BuildStatus == PipelineStatus.running;
        }
        void PerformAbortTest(CommitViewModel commit) {
            var actualCommit = commit ?? BranchViewModel.MergeRequest.Commits.FirstOrDefault();

            BranchViewModel.AbortBuild(BranchViewModel.MergeRequest.MergeRequest, actualCommit?.Build.Build);
        }
        bool CanPerformShowLogs(CommitViewModel model) {
            if (model == null)
                return false;
            if (model.Build == null)
                model.UpdateBuilds();
            var buildStatus = model.Build?.BuildStatus;
            return buildStatus == PipelineStatus.failed || buildStatus == PipelineStatus.success;
        }
        void PerformShowLogs(CommitViewModel model) {
            if (model.Build == null)
                model.UpdateBuilds();
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
        readonly Func<Job, byte[]> downloadArtifactsHandler;
        readonly Func<Job, byte[]> downloadTraceHandler;
        readonly Func<Sha1, IEnumerable<Job>> getBuildsHandler;
        public string Id { get; }
        public BuildViewModel Build { get; private set; }
        public string Title {
            get { return GetProperty(() => Title); }
            private set { SetProperty(() => Title, value); }
        }
        public PipelineStatus BuildStatus {
            get { return GetProperty(() => BuildStatus); }
            private set { SetProperty(() => BuildStatus, value); }
        }
        public string Duration {
            get { return GetProperty(() => Duration); }
            private set { SetProperty(() => Duration, value); }
        }
        public CommitViewModel(Commit commit, Func<Sha1, IEnumerable<Job>> getBuildsHandler, Func<Job, byte[]> downloadArtifactsHandler, Func<Job, byte[]> downloadTraceHandler) {
            this.commit = commit;
            this.downloadArtifactsHandler = downloadArtifactsHandler;
            this.downloadTraceHandler = downloadTraceHandler;
            this.getBuildsHandler = getBuildsHandler;
            Title = commit.Title;
            Id = commit.ShortId;
            Duration = "Click to load";
        }
        public void UpdateBuilds() {
            var builds = getBuildsHandler(commit.Id);
            var job = builds.OrderByDescending(b => b.CreatedAt).FirstOrDefault();
            if(job == null)
                return;
            Build = new BuildViewModel(job);
            BuildStatus = Build.BuildStatus;
            Duration = Build.Duration;
        }
        public ArtifactsViewModel DownloadArtifacts() {
            if (Build == null)
                return null;
            return new ArtifactsViewModel(Build.Artifacts, downloadArtifactsHandler(Build.Build), downloadTraceHandler(Build.Build));
        }
    }

    public class BuildViewModel : BindableBase {
        public int Id => Build.Id;
        public PipelineStatus BuildStatus { get; }
        public string Duration { get; }
        public ArtifactsFile Artifacts => Build.File;
        public Job Build { get; }
        public BuildViewModel(Job build) {
            Build = build;
            BuildStatus = build.Pipeline.Status;
            if (build.CreatedAt != null && (BuildStatus == PipelineStatus.success || BuildStatus == PipelineStatus.failed)) {
                Duration = ((build.FinishedAt ?? DateTime.Now) - build.CreatedAt.Value).ToString("g");
            }
            else
                Duration = string.Empty;
        }
    }
}
