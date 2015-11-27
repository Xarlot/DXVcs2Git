using System;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core;
using DXVcs2Git.UI.Farm;
using DXVcs2Git.Core.Configuration;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : ViewModelBase {
        public RepositoriesViewModel Repositories { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand DownloadNewVersionCommand { get; private set; }
        public IDialogService SettingsDialogService { get { return GetService<IDialogService>("settingsDialogService"); } }
        public IDialogService DownloaderDialogService { get { return GetService<IDialogService>("downloaderDialogService"); } }
        public Config Config { get; private set; }
        public string Version { get; private set; }        

        public RootViewModel() {
            Repositories = new RepositoriesViewModel();
            ISupportParentViewModel supportParent = Repositories;
            supportParent.ParentViewModel = this;
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(Initialize));
            FarmIntegrator.Start(Dispatcher.CurrentDispatcher, FarmRefreshed);
            AtomFeed.FeedWorker.Initialize();
            SettingsCommand = DelegateCommandFactory.Create(ShowSettings, CanShowSettings);
            DownloadNewVersionCommand = DelegateCommandFactory.Create(DownloadNewVersion, CanDownloadNewVersion);
            Config = ConfigSerializer.GetConfig();
            Version = $"Git tools {VersionInfo.Version}";
        }

        void DownloadNewVersion() {
            var model = new UriDownloaderViewModel(AtomFeed.FeedWorker.NewVersionUri, AtomFeed.FeedWorker.NewVersion);
            DownloaderDialogService.ShowDialog(new UICommand[] { model.OKCommand, model.CancelCommand }, "Downloading new version...", model);
        }
        bool CanDownloadNewVersion() {
            return AtomFeed.FeedWorker.HasNewVersion;
        }

        void FarmRefreshed() {
            Repositories.RefreshFarm();
        }
        public void Initialize() {
            Repositories.Update();
        }
        public void Refresh() {
            Repositories.Refresh();
        }
        void ShowSettings() {
            var viewModel = new EditConfigViewModel(Config);
            if (SettingsDialogService.ShowDialog(MessageButton.OKCancel, "Settings", viewModel) == MessageResult.OK) {
                Config = viewModel.CreateConfig();
                ConfigSerializer.SaveConfig(Config);
                Initialize();
            }
        }
        bool CanShowSettings() {
            return true;
        }
    }
}
