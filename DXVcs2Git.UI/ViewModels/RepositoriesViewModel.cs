using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using NGitLab.Models;
using DXVcs2Git.Core.Configuration;
using System.Threading.Tasks;
using DXVcs2Git.Core;

namespace DXVcs2Git.UI.ViewModels {
    public class RepositoriesViewModel : ViewModelBase {
        BranchViewModel selectedBranch;
        RepositoryViewModel selectedRepository;
        bool fake = false;
        public RootViewModel RootViewModel => this.GetParentViewModel<RootViewModel>();
        public Config Config => RootViewModel?.Config ?? ConfigSerializer.GetConfig();
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
            set { SetProperty(ref this.selectedBranch, value, () => SelectedBranch, SelectedBranchChanged); }
        }
        void SelectedBranchChanged() {
            Messenger.Default.Send(new Message(MessageType.SelectedBranchChanged));
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
        }

        public void Update() {
            IsInitialized = false;
            SendBeforeUpdateMessage();
            RepoConfigs = new RepoConfigsReader();
            Repositories = Config.Repositories.With(x => x.Where(IsValidConfig).Select(repo => new RepositoryViewModel(repo.Name, repo, this))).With(x => x.ToList());
            Refresh();
            IsInitialized = true;
            SendUpdateMessage();
        }
        void SendBeforeUpdateMessage() {
            Messenger.Default.Send(new Message(MessageType.BeforeUpdate));
        }
        void SendUpdateMessage() {
            if (!fake)
                Messenger.Default.Send(new Message(MessageType.Update));
        }

        public Task BeginUpdate() {
            Log.Message("Repositories update started");
            IsInitialized = false;
            CommandManager.InvalidateRequerySuggested();
            ConfigSerializer.SaveConfig(Config);
            return Task.Run(() => {
                RepositoriesViewModel rvm = new RepositoriesViewModel();
                rvm.Update();
                return rvm;
            }).ContinueWith(_ => {
                RepoConfigs = _.Result.RepoConfigs;
                Repositories = _.Result.Repositories;
                SelectedRepository = _.Result.SelectedRepository;
                IsInitialized = true;
                Log.Message("Repositories update completed");
                SendUpdateMessage();
                CommandManager.InvalidateRequerySuggested();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        bool IsValidConfig(TrackRepository repo) {
            if (string.IsNullOrEmpty(repo.Name))
                return false;
            if (!RepoConfigs.HasConfig(repo.ConfigName))
                return false;
            if (string.IsNullOrEmpty(repo.LocalPath))
                return false;
            if (string.IsNullOrEmpty(repo.Server))
                return false;
            if (string.IsNullOrEmpty(repo.Token))
                return false;
            return DirectoryHelper.IsGitDir(repo.LocalPath);
        }
        public void Refresh() {
            if (Repositories == null)
                return;
            Repositories.ForEach(x => x.Refresh());
            if (!fake) {
                CommandManager.InvalidateRequerySuggested();
                Messenger.Default.Send(new Message(MessageType.Refresh));
            }
        }
        public void RefreshFarm() {
            if (Repositories == null)
                return;
            Repositories.ForEach(x => x.RefreshFarm());
        }
    }
}
