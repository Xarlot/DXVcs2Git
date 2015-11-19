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
            //Project = gitLabWrapper.FindProject(this.gitReader.GetRemoteRepoPath());
            //if (Project == null) {
            //    Log.Error("Can`t find project");
            //    return;
            //}

            //var mergeRequests = this.gitLabWrapper.GetMergeRequests(Project);
            //var branches = this.gitLabWrapper.GetBranches(Project).ToList();
            //ProtectedBranches = branches.Where(x => x.Protected).ToList();
            //Branches = branches.Where(x => !x.Protected)
            //    .Select(x => new BranchViewModel(gitLabWrapper, this.gitReader, this, mergeRequests.FirstOrDefault(mr => mr.SourceBranch == x.Name), x)).ToList();
            //SelectedBranch = Branches.FirstOrDefault();
            //HasEditableMergeRequest = SelectedBranch.If(x => x.IsInEditingMergeRequest).ReturnSuccess();
        }
        bool CanUpdate() {
            return true;
        }
        public void ForceMerge() {
            GitRepoConfig config = Serializer.Deserialize<GitRepoConfig>(GetConfigPath());
            if (config != null)
                FarmHelper.ForceBuild(config.FarmTaskName);
        }
        string GetConfigPath() {
            return Path.Combine(this.gitReader.GetLocalRepoPath(), GitRepoConfig.ConfigFileName);
        }
        public bool CanForceMerge() {
            return FarmHelper.CanForceBuild("XPF DXVcs2Git sync task v15.2");
        }
    }
}
