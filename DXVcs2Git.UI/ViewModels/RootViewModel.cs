using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core;
using DXVcs2Git.UI.Farm;
using DXVcs2Git.Core.Configuration;
using Microsoft.Practices.ServiceLocation;
using DevExpress.Xpf.Core;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : ViewModelBase {
        const string DefaultThemeName = "Office2013";
        public RepositoriesViewModel Repositories { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand ShowLogCommand { get; private set; }
        public ICommand DownloadNewVersionCommand { get; private set; }
        public ICommand InitializeCommand { get; private set; }
        public ICommand UpdateCommand { get; private set; }
        public INotificationService NotificationService => GetService<INotificationService>("notificationService");
        public IDialogService SettingsDialogService => GetService<IDialogService>("settingsDialogService");
        public IDialogService DownloaderDialogService => GetService<IDialogService>("downloaderDialogService");
        public Config Config { get; private set; }
        public string Version { get; private set; }
        public LoggingViewModel LogViewModel { get; private set; }
        public bool ShowLog {
            get { return GetProperty(() => ShowLog); }
            set { SetProperty(() => ShowLog, value, ShowLogChanged); }
        }
        public RootViewModel() {
            Config = ConfigSerializer.GetConfig();
            UpdateDefaultTheme();

            FarmIntegrator.Start(Dispatcher.CurrentDispatcher, FarmRefreshed);
            AtomFeed.FeedWorker.Initialize();

            UpdateCommand = DelegateCommandFactory.Create(PerformUpdate, CanPerformUpdate);
            SettingsCommand = DelegateCommandFactory.Create(ShowSettings, CanShowSettings);
            ShowLogCommand = DelegateCommandFactory.Create(PerformShowLog);
            DownloadNewVersionCommand = DelegateCommandFactory.Create(DownloadNewVersion, CanDownloadNewVersion);
            InitializeCommand = DelegateCommandFactory.Create( PerformInitialize, CanPerformInitialize);
            LogViewModel = new LoggingViewModel();
            Version = $"Git tools {VersionInfo.Version}";
        }
        bool CanPerformUpdate() {
            return true;
        }
        void PerformUpdate() {
            Update();
        }
        bool CanPerformInitialize() {
            return true;
        }
        void PerformInitialize() {
            Initialize();
        }
        void PerformShowLog() {
        }
        void DownloadNewVersion() {
            var model = new UriDownloaderViewModel(AtomFeed.FeedWorker.NewVersionUri, AtomFeed.FeedWorker.NewVersion);
            DownloaderDialogService.ShowDialog(new[] { model.RestartCommand, model.CancelCommand }, "Downloading new version...", model);
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
            Repositories.Repositories.ForEach(x => x.RefreshFarm());
            Messenger.Default.Send(new Message(MessageType.RefreshFarm));
        }
        public void Initialize() {
            Repositories = ServiceLocator.Current.GetInstance<RepositoriesViewModel>();
            Update();
        }
        void UpdateDefaultTheme() => ApplicationThemeHelper.ApplicationThemeName = Config?.DefaultTheme ?? DefaultThemeName;
        public void Update() {
            Repositories.Update();
        }
        public void Refresh() {
        }
        void ShowSettings() {
            var viewModel = new EditConfigViewModel(Config);
            if (SettingsDialogService.ShowDialog(MessageButton.OKCancel, "Settings", viewModel) == MessageResult.OK) {
                viewModel.UpdateConfig();
                ConfigSerializer.SaveConfig(Config);
                Initialize();
                UpdateDefaultTheme();
            }
        }
        bool CanShowSettings() {
            return true;
        }
    }
}
