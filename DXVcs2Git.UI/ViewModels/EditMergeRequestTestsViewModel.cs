using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public ICommand LoadLogCommand { get; }

        IWindowService ShowLogsService => GetService<IWindowService>();
        IDialogService LoadLogService => GetService<IDialogService>("loadLog");

        public IEnumerable<CommitViewModel> Commits {
            get { return GetProperty(() => Commits); }
            private set { SetProperty(() => Commits, value); }
        }

        public EditMergeRequestTestsViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            CancelTestsCommand = DelegateCommandFactory.Create(PerformCancelTests, CanPerformCancelTests);
            ShowLogCommand = DelegateCommandFactory.Create<CommitViewModel>(PerformShowLogs, CanPerformShowLogs);
            LoadLogCommand = DelegateCommandFactory.Create(PerformLoadLog, CanPerformLoadLog);

            Initialize();
        }
        bool CanPerformLoadLog() {
            return BranchViewModel != null;
        }
        readonly Regex parseBuildRegex = new Regex(@"http://(?<server>[\w\._-]+)/(?<nspace>[\w\._-]+)/(?<name>[\w\._-]+)/builds/(?<build>\d+)", RegexOptions.Compiled);
        void PerformLoadLog() {
            var log = new LoadLogViewModel();
            if (LoadLogService.ShowDialog(MessageButton.OKCancel, "Load log", log) == MessageResult.OK) {
                string url = log.Url;
                if (string.IsNullOrEmpty(url))
                    return;
                var match = parseBuildRegex.Match(url);
                if (!match.Success)
                    return;
                var artifacts = BranchViewModel.DownloadArtifacts($@"http://{match.Groups["server"]}/{match.Groups["nspace"]}/{match.Groups["name"]}.git", new Build() { Id = Convert.ToInt32(match.Groups["build"].Value)});
                if (artifacts == null)
                    return;
                ArtifactsViewModel model = new ArtifactsViewModel(new ArtifactsFile() { FileName = "test.zip"}, artifacts);
                ShowLogsService.Show(model);
            }
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
