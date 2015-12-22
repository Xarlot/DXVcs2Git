using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.Configuration;
using System.Windows.Input;
using DXVcs2Git.Core.Git;

namespace DXVcs2Git.UI.ViewModels {
    public class EditConfigViewModel : ViewModelBase {
        readonly Config config;
        readonly RepoConfigsReader configsReader;
        public int UpdateDelay {
            get { return GetProperty(() => UpdateDelay); }
            set { SetProperty(() => UpdateDelay, value, OnUpdateDelayChanged); }
        }
        public IEnumerable<GitRepoConfig> Configs { get; }
        public ICommand RefreshUpdateCommand { get; private set; }
        public bool HasUIValidationErrors {
            get { return GetProperty(() => HasUIValidationErrors); }
            set { SetProperty(() => HasUIValidationErrors, value); }
        }

        void OnUpdateDelayChanged() {
            AtomFeed.FeedWorker.UpdateDelay = UpdateDelay;
        }

        public ObservableCollection<EditTrackRepository> Repositories { get; private set; }
        public ObservableCollection<string> AvailableTokens {
            get { return GetProperty(() => AvailableTokens); }
            private set { SetProperty(() => AvailableTokens, value); }
        }
        public ObservableCollection<string> AvailableConfigs {
            get { return GetProperty(() => AvailableConfigs); }
            private set { SetProperty(() => AvailableConfigs, value); }
        }

        public EditConfigViewModel(Config config) {
            this.config = config;
            this.configsReader = new RepoConfigsReader();
            Configs = this.configsReader.RegisteredConfigs;
            UpdateDelay = AtomFeed.FeedWorker.UpdateDelay;
            RefreshUpdateCommand = DelegateCommandFactory.Create(AtomFeed.FeedWorker.Update);
            Repositories = CreateEditRepositories(config);
            Repositories.CollectionChanged += RepositoriesOnCollectionChanged;
            UpdateTokens();
        }
        void RepositoriesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            UpdateTokens();
        }
        ObservableCollection<EditTrackRepository> CreateEditRepositories(Config config) {
            return config.Repositories.Return(x => new ObservableCollection<EditTrackRepository>(config.Repositories.Select(CreateEditRepository)), () => new ObservableCollection<EditTrackRepository>());
        }
        EditTrackRepository CreateEditRepository(TrackRepository repo) {
            return new EditTrackRepository() {
                Name = repo.Name,
                ConfigName = repo.ConfigName,
                RepoConfig = this.configsReader[repo.ConfigName],
                Token = repo.Token,
                LocalPath = repo.LocalPath,
            };
        }
        public void UpdateConfig() {
            config.Repositories = Repositories.With(x => x.Select(repo => new TrackRepository() { Name = repo.Name, ConfigName  = repo.ConfigName, LocalPath = repo.LocalPath, Server = repo.RepoConfig.Server, Token = repo.Token}).ToArray());
            config.UpdateDelay = UpdateDelay;
        }
        public void UpdateTokens() {
            AvailableTokens = Repositories.Return(x => new ObservableCollection<string>(x.Select(repo => repo.Token).Distinct()), () => new ObservableCollection<string>());
            var userConfigs = Repositories.Return(x => new ObservableCollection<string>(x.Select(repo => repo.ConfigName).Distinct()), () => new ObservableCollection<string>());
            AvailableConfigs = new ObservableCollection<string>(this.configsReader.RegisteredConfigs.Select(config => config.Name).Except(userConfigs));
        }
        public GitRepoConfig GetConfig(string name) {
            return this.configsReader[name];
        }
    }
}
