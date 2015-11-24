using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class MergeRequestsViewModel : ViewModelBase {
        GitLabWrapper gitLabWrapper;
        BranchViewModel selectedBranch;
        RepositoryViewModel selectedRepository;

        public Project Project { get; private set; }
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

        public ICommand UpdateCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public Config Config { get; private set; }
        public IEnumerable<RepositoryViewModel> Repositories {
            get { return GetProperty(() => Repositories); }
            set { SetProperty(() => Repositories, value); }
        }
        public RepositoryViewModel SelectedRepository {
            get { return this.selectedRepository; }
            set { SetProperty(ref this.selectedRepository, value, () => SelectedRepository); }
        }
        public IDialogService SettingsDialogService { get { return GetService<IDialogService>("settingsDialogService", ServiceSearchMode.PreferParents); } }

        public MergeRequestsViewModel() {
            UpdateCommand = DelegateCommandFactory.Create(Update, CanUpdate);
            SettingsCommand = DelegateCommandFactory.Create(ShowSettings, CanShowSettings);
            Config = ConfigSerializer.GetConfig();
        }
        void ShowSettings() {
            var viewModel = new EditConfigViewModel(Config);
            if (SettingsDialogService.ShowDialog(MessageButton.OKCancel, "Settings", viewModel) == MessageResult.OK) {
                Config = viewModel.CreateConfig();
                ConfigSerializer.SaveConfig(Config);
                Update();
            }
        }
        bool CanShowSettings() {
            return true;
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
        }
        bool CanUpdate() {
            return IsInitialized;
        }
    }
}
