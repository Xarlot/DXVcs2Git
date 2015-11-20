using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;
using DXVcs2Git.UI.Farm;
using DXVcs2Git.UI.Views;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class MergeRequestsViewModel : BindableBase {
        readonly GitLabWrapper gitLabWrapper;
        readonly GitReaderWrapper gitReader;
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

        public ICommand UpdateCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public IEnumerable<UserViewModel> Users { get; private set; }
        public Config Config { get; private set; }
        public IEnumerable<RepositoryViewModel> Repositories {
            get { return GetProperty(() => Repositories); }
            set { SetProperty(() => Repositories, value); }
        }
        public RepositoryViewModel SelectedRepository {
            get { return this.selectedRepository; }
            set { SetProperty(ref this.selectedRepository, value, () => SelectedRepository); }
        }

        public MergeRequestsViewModel(GitLabWrapper gitLabWrapper, GitReaderWrapper gitReader) {
            this.gitReader = gitReader;
            this.gitLabWrapper = gitLabWrapper;
            UpdateCommand = DelegateCommandFactory.Create(Update, CanUpdate);
            SettingsCommand = DelegateCommandFactory.Create(ShowSettings, CanShowSettings);
            Users = gitLabWrapper.GetUsers().Select(x => new UserViewModel(x)).ToList();
            Config = ConfigSerializer.GetConfig();

            Update();

        }
        void ShowSettings() {
            DXDialogWindow dialog = new DXDialogWindow("Settings", MessageBoxButton.OKCancel);
            EditConfigViewModel config = new EditConfigViewModel(Config);
            dialog.Content = new EditConfigControl() { DataContext =  config};
            if (dialog.ShowDialog() == true) {
                Config = config.CreateConfig();
                ConfigSerializer.SaveConfig(Config);
                Update();
            }
        }
        bool CanShowSettings() {
            return true;
        }
        public void Update() {
            Repositories = Config.Repositories.With(x => x.Where(repo => repo.Watch).Select(repo => new RepositoryViewModel(repo.Name, this.gitLabWrapper, new GitReaderWrapper(repo.LocalPath), this))).With(x => x.ToList());
            SelectedRepository = Repositories.With(x => x.FirstOrDefault());
            Refresh();
        }
        public void Refresh() {
            if (Repositories == null)
                return;
            Repositories.ForEach(x => x.Refresh());
        }
        bool CanUpdate() {
            return true;
        }
        public void ForceMerge() {
            GitRepoConfig config = Serializer.Deserialize<GitRepoConfig>(GetConfigPath());
            if (config != null)
                FarmIntegrator.ForceBuild(config.FarmTaskName);
        }
        string GetConfigPath() {
            return Path.Combine(this.gitReader.GetLocalRepoPath(), GitRepoConfig.ConfigFileName);
        }
        public bool CanForceMerge() {
            return FarmIntegrator.CanForceBuild("XPF DXVcs2Git sync task v15.2");
        }
    }
}
