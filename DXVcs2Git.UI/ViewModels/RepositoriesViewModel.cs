using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DXVcs2Git.Core.Configuration;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Git;

namespace DXVcs2Git.UI.ViewModels {
    public class RepositoriesViewModel : ViewModelBase {
        public static void RaiseRefreshSelectedBranch() {
            Messenger.Default.Send(new Message(MessageType.RefreshSelectedBranch));
            CommandManager.InvalidateRequerySuggested();
        }

        BranchViewModel selectedBranch;
        RepositoryViewModel selectedRepository;
        public RootViewModel RootViewModel => this.GetParentViewModel<RootViewModel>();
        public Config Config => RootViewModel?.Config ?? ConfigSerializer.GetConfig();
        public RepoConfigsReader RepoConfigs { get; private set; }
        public TestConfigsReader TestConfigs { get; private set; }
        public BranchViewModel SelectedBranch {
            get { return this.selectedBranch; }
            set { SetProperty(ref this.selectedBranch, value, () => SelectedBranch, SelectedBranchChanged); }
        }
        void SelectedBranchChanged() {
            SelectedBranch?.RefreshMergeRequest();
            RaiseRefreshSelectedBranch();
        }
        public bool IsInitialized { get; private set; }

        public IEnumerable<RepositoryViewModel> Repositories {
            get { return GetProperty(() => Repositories); }
            set { SetProperty(() => Repositories, value); }
        }
        public RepositoryViewModel SelectedRepository {
            get { return this.selectedRepository; }
            set { SetProperty(ref this.selectedRepository, value, () => SelectedRepository); }
        }

        public RepositoriesViewModel() {
        }

        public void Update() {
            IsInitialized = false;
            SendBeforeUpdateMessage();
            RepoConfigs = new RepoConfigsReader();
            TestConfigs = new TestConfigsReader();
            Repositories = Config.Repositories.With(x => x.Where(IsValidConfig).Select(repo => new RepositoryViewModel(repo.Name, repo, this))).With(x => x.ToList());
            IsInitialized = true;
            SendUpdateMessage();
        }
        void SendBeforeUpdateMessage() {
            Messenger.Default.Send(new Message(MessageType.BeforeUpdate));
        }
        void SendUpdateMessage() {
            Messenger.Default.Send(new Message(MessageType.Update));
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
    }
}
