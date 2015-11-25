using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class RepositoriesViewModel : ViewModelBase {
        GitLabWrapper gitLabWrapper;
        BranchViewModel selectedBranch;
        RepositoryViewModel selectedRepository;
        public RootViewModel RootViewModel { get { return this.GetParentViewModel<RootViewModel>(); } }
        public Config Config { get { return RootViewModel.Config; } }

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
            set { SetProperty(ref this.selectedRepository, value, () => SelectedRepository); }
        }

        public RepositoriesViewModel() {
        }

        public void Update() {
            IsInitialized = false;
            if (!IsValidConfig(Config)) 
                return;
            gitLabWrapper = new GitLabWrapper(Config.GitServer, Config.Token);
            Repositories = Config.Repositories.With(x => x.Where(repo => repo.Watch).Select(repo => new RepositoryViewModel(repo.Name, this.gitLabWrapper, new GitReaderWrapper(repo.LocalPath), this))).With(x => x.ToList());
            SelectedRepository = Repositories.With(x => x.FirstOrDefault());
            Refresh();
            IsInitialized = true;

            Messenger.Default.Send(new Message(MessageType.Update));
        }
        bool IsValidConfig(Config config) {
            if (config == null)
                return false;
            if (string.IsNullOrEmpty(config.Token))
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
    }
}
