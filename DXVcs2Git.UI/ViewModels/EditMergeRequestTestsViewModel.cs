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
            Commits = BranchViewModel?.GetCommits(BranchViewModel.MergeRequest.MergeRequest).Select(x => new CommitViewModel(x)).ToList() ?? Enumerable.Empty<CommitViewModel>();
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
        public Sha1 Id { get; }
        public string Title {
            get { return GetProperty(() => Title); }
            private set { SetProperty(() => Title, value); }
        }
        public CommitViewModel(Commit commit) {
            Title = commit.Title;
            Id = commit.Id;
        }

    }
}
