using System;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DXVcs2Git.Core;
using DXVcs2Git.UI.Farm;
using DXVcs2Git.Core.Configuration;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : ViewModelBase {
        const string DefaultThemeName = "Office2013";
        public RepositoriesViewModel Repositories { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand ShowLogCommand { get; private set; }
        public ICommand DownloadNewVersionCommand { get; private set; }
        public INotificationService NotificationService { get { return GetService<INotificationService>("notificationService"); } }
        public IDialogService SettingsDialogService { get { return GetService<IDialogService>("settingsDialogService"); } }
        public IDialogService DownloaderDialogService { get { return GetService<IDialogService>("downloaderDialogService"); } }
        public Config Config { get; private set; }
        public string Version { get; private set; }
        public LoggingViewModel LogViewModel { get; private set; }
        public bool ShowLog {
            get { return GetProperty(() => ShowLog); }
            set { SetProperty(() => ShowLog, value, ShowLogChanged); }
        }
        public RootViewModel() {
            Repositories = new RepositoriesViewModel();
            ISupportParentViewModel supportParent = Repositories;
            supportParent.ParentViewModel = this;
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(Initialize));
            FarmIntegrator.Start(Dispatcher.CurrentDispatcher, FarmRefreshed);
            AtomFeed.FeedWorker.Initialize();
            SettingsCommand = DelegateCommandFactory.Create(ShowSettings, CanShowSettings);
            ShowLogCommand = DelegateCommandFactory.Create(PerformShowLog);
            DownloadNewVersionCommand = DelegateCommandFactory.Create(DownloadNewVersion, CanDownloadNewVersion);
            Config = ConfigSerializer.GetConfig();
            UpdateDefaultTheme();
            LogViewModel = new LoggingViewModel();
            Version = $"Git tools {VersionInfo.Version}";
        }
        void PerformShowLog() {
        }
        void DownloadNewVersion() {
            var model = new UriDownloaderViewModel(AtomFeed.FeedWorker.NewVersionUri, AtomFeed.FeedWorker.NewVersion);
            DownloaderDialogService.ShowDialog(new UICommand[] { model.RestartCommand, model.CancelCommand }, "Downloading new version...", model);
        }
        bool CanDownloadNewVersion() {
            return AtomFeed.FeedWorker.HasNewVersion;
        }
        void ShowLogChanged() {
            if (ShowLog) {
                LogIntegrator.Start(Dispatcher.CurrentDispatcher, RefreshLog);
                RefreshLog();
            }
            else {
                LogIntegrator.Stop();
            }
        }
        void RefreshLog() {
            LogViewModel.Text = Log.GetLog();
        }
        void FarmRefreshed() {
            Repositories.RefreshFarm();
        }
        public void Initialize() {
            Repositories.Update();
            UpdateDefaultTheme();
        }
        void UpdateDefaultTheme() {
            ThemeManager.ApplicationThemeName = Config?.DefaultTheme ?? DefaultThemeName;
        }
        public void Refresh() {
            Repositories.Refresh();
        }
        void ShowSettings() {
            var viewModel = new EditConfigViewModel(Config);
            if (SettingsDialogService.ShowDialog(MessageButton.OKCancel, "Settings", viewModel) == MessageResult.OK) {
                viewModel.UpdateConfig();
                ConfigSerializer.SaveConfig(Config);
                Initialize();
            }
        }
        bool CanShowSettings() {
            return true;
        }
    }
}
