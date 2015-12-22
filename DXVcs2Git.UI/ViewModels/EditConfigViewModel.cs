using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        void OnUpdateDelayChanged() {
            AtomFeed.FeedWorker.UpdateDelay = UpdateDelay;
        }

        public ObservableCollection<EditTrackRepository> Repositories { get; private set; }

        public EditConfigViewModel(Config config) {
            this.config = config;
            this.configsReader = new RepoConfigsReader();
            Configs = this.configsReader.RegisteredConfigs;
            UpdateDelay = AtomFeed.FeedWorker.UpdateDelay;
            RefreshUpdateCommand = DelegateCommandFactory.Create(AtomFeed.FeedWorker.Update);
            Repositories = CreateEditRepositories(config);
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
            //config.Repositories = Repositories.With(x => x.ToArray());
            config.UpdateDelay = UpdateDelay;
        }
    }
}
