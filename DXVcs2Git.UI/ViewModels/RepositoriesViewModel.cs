using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using NGitLab.Models;
using DXVcs2Git.Core.Configuration;

namespace DXVcs2Git.UI.ViewModels {
    public class RepositoriesViewModel : ViewModelBase {
        BranchViewModel selectedBranch;
        RepositoryViewModel selectedRepository;
        public RootViewModel RootViewModel { get { return this.GetParentViewModel<RootViewModel>(); } }
        public Config Config { get { return RootViewModel.Config; } }
        public RepoConfigsReader RepoConfigs { get; private set; }
        public IEnumerable<BranchViewModel> Branches {
            get { return GetProperty(() => Branches); }
            set { SetProperty(() => Branches, value); }
        }
        public IEnumerable<Branch> ProtectedBranches { get; set; }
        public bool HasEditableMergeRequest {
            get { return GetProperty(() => HasEditableMergeRequest); }
            private set { SetProperty(() => HasEditableMergeRequest, value); }
        }
        public BranchViewModel SelectedBranch {
            get { return this.selectedBranch; }
            set { SetProperty(ref this.selectedBranch, value, () => SelectedBranch); }
        }
        public bool IsInitialized { get; private set; }

        public IEnumerable<RepositoryViewModel> Repositories {
            get { return GetProperty(() => Repositories); }
            set { SetProperty(() => Repositories, value); }
        }
        public RepositoryViewModel SelectedRepository {
            get { return this.selectedRepository; }
            set { SetProperty(ref this.selectedRepository, value, () => SelectedRepository, SelectedRepositoryChanged); }
        }
        void SelectedRepositoryChanged() {
            Refresh();
        }

        public RepositoriesViewModel() {
            Refresh();
        }

        public void Update() {
            IsInitialized = false;
            RepoConfigs = new RepoConfigsReader();
            Repositories = Config.Repositories.With(x => x.Where(IsValidConfig).Select(repo => new RepositoryViewModel(repo.Name, repo, this))).With(x => x.ToList());
            SelectedRepository = Repositories.With(x => x.FirstOrDefault());
            Refresh();
            IsInitialized = true;

            Messenger.Default.Send(new Message(MessageType.Update));
        }
        bool IsValidConfig(TrackRepository repo) {
            if (string.IsNullOrEmpty(repo.Name))
                return false;
            if (!RepoConfigs.HasConfig(repo.Name))
                return false;
            if (string.IsNullOrEmpty(repo.LocalPath))
                return false;
            if (string.IsNullOrEmpty(repo.Server))
                return false;
            if (string.IsNullOrEmpty(repo.Token))
                return false;
            return true;
        }
        public void Refresh() {
            if (Repositories == null)
                return;
            Repositories.ForEach(x => x.Refresh());
            CommandManager.InvalidateRequerySuggested();
            Messenger.Default.Send(new Message(MessageType.Refresh));
        }
        public void RefreshFarm() {
            if (Repositories == null)
                return;
            Repositories.ForEach(x => x.RefreshFarm());
        }
    }
}
