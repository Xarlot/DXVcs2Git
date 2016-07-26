using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Practices.ServiceLocation;
using NGitLab;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class EditMergeRequestTestsViewModel : ViewModelBase {
        RepositoriesViewModel RepositoriesViewModel => ServiceLocator.Current.GetInstance<RepositoriesViewModel>();

        public ICommand RunTestsCommand { get; }
        public ICommand CancelTestsCommand { get; }

        public IEnumerable<CommitViewModel> Commits {
            get { return GetProperty(() => Commits); }
            private set { SetProperty(() => Commits, value); }
        }

        public EditMergeRequestTestsViewModel() {
            Messenger.Default.Register<Message>(this, OnMessageReceived);
            RunTestsCommand = DelegateCommandFactory.Create(PerformRunTests, CanPerformRunTests);
            CancelTestsCommand = DelegateCommandFactory.Create(PerformCancelTests, CanPerformCancelTests);

            Initialize();
        }
        bool CanPerformCancelTests() {
            return false;
        }
        void PerformCancelTests() {
        }
        BranchViewModel BranchViewModel { get; set; }

        public bool IsTestsRunning {
            get { return GetProperty(() => IsTestsRunning); }
            private set { SetProperty(() => IsTestsRunning, value); }
        }
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
            Commits = BranchViewModel.GetCommits(mergeRequest.MergeRequest).Select(x => new CommitViewModel(x)).ToList();
            Commits.ForEach(x => x.UpdateBuilds(sha => BranchViewModel.GetBuilds(mergeRequest.MergeRequest, sha)));
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
        public string Id { get; }
        public string Title {
            get { return GetProperty(() => Title); }
            private set { SetProperty(() => Title, value); }
        }
        public BuildViewModel Build {
            get { return GetProperty(() => Build); }
            private set { SetProperty(() => Build, value); }
        }
        public CommitViewModel(Commit commit) {
            this.commit = commit;
            Title = commit.Title;
            Id = commit.ShortId;
        }
        public void UpdateBuilds(Func<Sha1, IEnumerable<Build>> getBuilds) {
            var builds = getBuilds(commit.Id);
            Build = builds.Select(x => new BuildViewModel(x)).FirstOrDefault();
        }
    }

    public class BuildViewModel : BindableBase {
        public int Id { get; }
        public BuildStatus BuildStatus { get; }
        public string Duration { get; }
        public BuildViewModel(Build build) {
            Id = build.Id;
            BuildStatus = build.Status ?? BuildStatus.undefined;
            if (build.StartedAt != null && (BuildStatus == BuildStatus.success || BuildStatus == BuildStatus.failed)) {
                Duration = ((build.FinishedAt ?? DateTime.Now) - build.StartedAt.Value).ToString("g");
            }
            else
                Duration = string.Empty;

        }
    }
}
