using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core;
using DXVcs2Git.UI.Farm;
using DXVcs2Git.Core.Configuration;
using Microsoft.Practices.ServiceLocation;
using DevExpress.Xpf.Core;
using DXVcs2Git.Core.GitLab;
using NGitLab.Models;
using ProjectHookType = DXVcs2Git.Core.GitLab.ProjectHookType;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : ViewModelBase {
        public const string DefaultThemeName = "Super";
        public RepositoriesViewModel Repositories { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand ShowLogCommand { get; private set; }
        public ICommand InitializeCommand { get; private set; }
        public ICommand UpdateCommand { get; private set; }
        public ICommand ActivateCommand { get; private set; }
        public ICommand UpdateAppCommand { get; private set; }
        public ICommand LoadTestLogCommand { get; private set; }
        public INotificationService NotificationService => GetService<INotificationService>("notificationService");
        public IDialogService SettingsDialogService => GetService<IDialogService>("settingsDialogService");
        public IDialogService DownloaderDialogService => GetService<IDialogService>("downloaderDialogService");
        public IDialogService LoadLogService => GetService<IDialogService>("loadTestLog");
        public IWindowService ShowLogsService => GetService<IWindowService>("showTestLog");
        public IMessageBoxService MessageBoxService => GetService<IMessageBoxService>("MessageBoxService");
        public Config Config { get; private set; }
        public string Version { get; private set; }
        public LoggingViewModel LogViewModel { get; private set; }
        readonly Dispatcher dispatcher;
        public bool ShowLog {
            get { return GetProperty(() => ShowLog); }
            set { SetProperty(() => ShowLog, value, ShowLogChanged); }
        }
        public ScrollBarMode ScrollBarMode {
            get { return GetProperty(() => ScrollBarMode); }
            set { SetProperty(() => ScrollBarMode, value); }
        }
        public RootViewModel() {
            Config = ConfigSerializer.GetConfig();
            UpdateAppearance();
            dispatcher = Dispatcher.CurrentDispatcher;
            FarmIntegrator.Start(FarmRefreshed);
            UpdateCommand = DelegateCommandFactory.Create(PerformUpdate, CanPerformUpdate);
            SettingsCommand = DelegateCommandFactory.Create(ShowSettings, CanShowSettings);
            ShowLogCommand = DelegateCommandFactory.Create(PerformShowLog);
            LoadTestLogCommand = DelegateCommandFactory.Create(PerformLoadTestLog, CanPerformLoadTestLog);
            InitializeCommand = DelegateCommandFactory.Create(PerformInitialize, CanPerformInitialize);
            ActivateCommand = DelegateCommandFactory.Create(PerformActivate, CanPerformActivate);
            UpdateAppCommand = DelegateCommandFactory.Create(UpdateApp, () => true);
            LogViewModel = new LoggingViewModel();
            Version = $"Git tools {VersionInfo.Version}";
        }
        bool CanPerformLoadTestLog() {
            return Repositories?.SelectedBranch != null;
        }
        readonly Regex parseBuildRegex = new Regex(@"http://(?<server>[\w\._-]+)/(?<nspace>[\w\._-]+)/(?<name>[\w\._-]+)/builds/(?<build>\d+)", RegexOptions.Compiled);
        void PerformLoadTestLog() {
            var log = new LoadLogViewModel();
            if (LoadLogService.ShowDialog(MessageButton.OKCancel, "Load log", log) == MessageResult.OK) {
                string url = log.Url;
                if (string.IsNullOrEmpty(url)) {
                    MessageBoxService.Show("Build url is not specified.", "Build log error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var match = parseBuildRegex.Match(url);
                if (!match.Success) {
                    MessageBoxService.Show("Specified url doesn`t match the pattern:\r\nhttp://{server}/{namespace}/{name}/builds/{buildId}", "Build log error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var repo = $@"http://{match.Groups["server"]}/{match.Groups["nspace"]}/{match.Groups["name"]}.git";
                var artifacts = Repositories.SelectedBranch.DownloadArtifacts(repo, new Job() { Id = Convert.ToInt32(match.Groups["build"].Value) });
                if (artifacts == null) {
                    MessageBoxService.Show("Build artifacts not found", "Build log error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                ArtifactsViewModel model = new ArtifactsViewModel(new ArtifactsFile() { FileName = "test.zip" }, artifacts);
                ShowLogsService.Show(model);
            }
        }
        Stopwatch sw = Stopwatch.StartNew();
        void PerformActivate() {
            var selectedBranch = Repositories.SelectedBranch;
            if (selectedBranch == null)
                return;
            if (sw.ElapsedMilliseconds < 5000)
                return;
            selectedBranch.RefreshMergeRequest();
            RepositoriesViewModel.RaiseRefreshSelectedBranch();
            sw.Restart();
        }
        bool CanPerformActivate() {
            return (Repositories?.IsInitialized ?? false) && Repositories.SelectedBranch != null;
        }
        void ProcessNotification(string message) {
            if (string.IsNullOrEmpty(message) || !message.StartsWith("{")) {
                Log.Message("Slack message is not json string.");
                return;
            }
            var hookType = ProjectHookClient.ParseHookType(message);
            if (hookType == null)
                return;
            Log.Message($"Web hook received.");
            Log.Message($"Web hook type: {hookType.HookType}.");

            var hook = ProjectHookClient.ParseHook(hookType);
            if (hook.HookType == ProjectHookType.push)
                ProcessPushHook((PushHookClient)hook);
            else if (hook.HookType == ProjectHookType.merge_request)
                ProcessMergeRequestHook((MergeRequestHookClient)hook);
            else if (hook.HookType == ProjectHookType.build)
                ProcessBuildHook((BuildHookClient)hook);
        }
        void ProcessBuildHook(BuildHookClient hook) {
            var selectedBranch = Repositories?.SelectedBranch;
            var mergeRequest = selectedBranch?.MergeRequest;
            if (mergeRequest == null)
                return;
            if (mergeRequest.SourceBranch != hook.Branch)
                return;
            if (selectedBranch.Repository?.Origin?.Id != hook.ProjectId)
                return;
            selectedBranch.RefreshMergeRequest();
            RepositoriesViewModel.RaiseRefreshSelectedBranch();
        }

        void PerformClick(NotificationResult result) {
        }
        void ProcessMergeRequestHook(MergeRequestHookClient hook) {
            if (Repositories == null)
                return;
            int mergeRequestId = hook.Attributes.Id;
            var selectedBranch = Repositories.SelectedBranch;
            if (selectedBranch != null) {
                var mergeRequest = selectedBranch.MergeRequest;
                if (mergeRequest != null) {
                    if (mergeRequest.MergeRequestId == mergeRequestId) {
                        selectedBranch.RefreshMergeRequest();
                        RepositoriesViewModel.RaiseRefreshSelectedBranch();
                        Log.Message("Selected branch refreshed.");
                    }
                }
            }
            if (Repositories.Repositories == null)
                return;
            foreach (var repo in Repositories.Repositories) {
                var branch = repo.Branches.Where(x => x.MergeRequest != null).FirstOrDefault(x => x.MergeRequest.MergeRequestId == mergeRequestId);
                if (branch != null)
                    ShowMergeRequestNotification(branch, hook);
                break;
            }
        }
        void ShowMergeRequestNotification(BranchViewModel branchViewModel, MergeRequestHookClient hook) {
            var mergeStatus = hook.Attributes.State;
            if (mergeStatus == MergerRequestState.merged) {
                string message = $"Merge request {hook.Attributes.Title} for branch {hook.Attributes.SourceBranch} was merged.";
                var notification = NotificationService.CreatePredefinedNotification(message, null, null, null);
                var task = notification.ShowAsync();
                task.ContinueWith(x => PerformClick(x.Result));
                return;
            }
            if (mergeStatus == MergerRequestState.closed) {
                string message = $"Merge request {hook.Attributes.Title} for branch {hook.Attributes.SourceBranch} was closed.";
                var notification = NotificationService.CreatePredefinedNotification(message, null, null, null);
                var task = notification.ShowAsync();
                task.ContinueWith(x => PerformClick(x.Result));
                return;
            }
            if (branchViewModel.MergeRequest.AssigneeId != hook.Attributes.AssigneeId) {
                string message = $"Assignee for merge request {hook.Attributes.Title} for branch {hook.Attributes.SourceBranch} was changed.";
                var notification = NotificationService.CreatePredefinedNotification(message, null, null, null);
                var task = notification.ShowAsync();
                task.ContinueWith(x => PerformClick(x.Result));
                return;
            }
        }
        void ProcessPushHook(PushHookClient hook) {
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
        void FarmRefreshed(FarmRefreshedEventArgs args) {
            if (args == null) {
                dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                    Repositories?.Repositories?.ForEach(x => x.RefreshFarm());
                    Messenger.Default.Send(new Message(MessageType.RefreshFarm));
                }));
                return;
            }
            if (args.RefreshType == FarmRefreshType.notification) {
                dispatcher.BeginInvoke(() => {
                    var notification = (NotificationReceivedEventArgs)args;
                    try {
                        notification.Parse();
                    }
                    catch (Exception ex) {
                        Log.Error("Can`t convert message from base64 string", ex);
                        return;
                    }
                    if (!notification.IsServiceUser)
                        return;
                    ProcessNotification(notification.Message);
                });
            }
        }
        public void Initialize() {
            Repositories = ServiceLocator.Current.GetInstance<RepositoriesViewModel>();
            Update();
        }
        void UpdateAppearance() {
            ScrollBarMode = (ScrollBarMode)Config.ScrollBarMode;
            RegisterTheme("Super", $"DevExpress.Xpf.Themes.Super.v{AssemblyInfo.VersionShort}");
            ApplicationThemeHelper.ApplicationThemeName = Config?.DefaultTheme ?? DefaultThemeName;
        }
        static void RegisterTheme(string themeName, string fullName) {
            var isRegistered = Theme.FindTheme(themeName);
            if (isRegistered != null)
                Theme.RemoveTheme(themeName);
            var theme = new Theme(themeName);
            theme.AssemblyName = fullName;
            Theme.RegisterTheme(theme);
        }
        public void Update() {
            Repositories.Update();
        }
        public void Refresh() {
        }
        void UpdateApp() {
            Services.UpdateAppService.Update(MessageBoxService);
        }
        void ShowSettings() {
            var viewModel = new EditConfigViewModel(Config);
            if (SettingsDialogService.ShowDialog(MessageButton.OKCancel, "Settings", viewModel) == MessageResult.OK) {
                viewModel.UpdateConfig();
                ConfigSerializer.SaveConfig(Config);
                Initialize();
                UpdateAppearance();
            }
        }
        bool CanShowSettings() {
            return true;
        }
    }

    public enum RefreshActions {
        nothing,
        mergerequest,
        commits,
        builds,
    }
    public class SlackRefresh {
        public RefreshActions RefreshAction { get; }
        public object Action { get; }

        public SlackRefresh(RefreshActions refreshAction, object action = null) {
            RefreshAction = refreshAction;
            Action = action;
        }
    }

    public class MergeRequestRefreshAction {
        public int Id { get; set; }
    }
}
