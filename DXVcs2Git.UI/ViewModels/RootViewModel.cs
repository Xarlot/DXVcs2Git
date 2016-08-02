using System;
using System.Collections.Generic;
using System.Reflection;
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
using DXVcs2Git.Core.Slack;
using NGitLab.Impl;
using NGitLab.Models;
using RestSharp;
using ProjectHookType = NGitLab.Models.ProjectHookType;

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
            SlackIntegrator.Start(@"xoxb-65373042855-WQEpKGmQabTlfc2GNyzlN6GU", Dispatcher.CurrentDispatcher, SlackRefreshed);

            SlackIntegrator.SendMessage("test");
            UpdateCommand = DelegateCommandFactory.Create(PerformUpdate, CanPerformUpdate);
            SettingsCommand = DelegateCommandFactory.Create(ShowSettings, CanShowSettings);
            ShowLogCommand = DelegateCommandFactory.Create(PerformShowLog);
            DownloadNewVersionCommand = DelegateCommandFactory.Create(DownloadNewVersion, CanDownloadNewVersion);
            InitializeCommand = DelegateCommandFactory.Create(PerformInitialize, CanPerformInitialize);
            LogViewModel = new LoggingViewModel();
            Version = $"Git tools {VersionInfo.Version}";
        }
        void SlackRefreshed(string message) {
            var hookType = ProjectHookClient.ParseHookType(message);
            if (hookType == null)
                return;
            Log.Message($"Web hook received.");
            Log.Message($"Web hook type: {hookType.HookType}.");

            if (string.IsNullOrEmpty(message) || !message.StartsWith("{")) {
                Log.Message("Slack message is not json string.");
                return;
            }
            var hook = ProjectHookClient.ParseHook(hookType);
            if (hook.HookType == Core.GitLab.ProjectHookType.push)
                ProcessPushHook((PushHookClient)hook);
            else if (hook.HookType == Core.GitLab.ProjectHookType.merge_request)
                ProcessMergeRequestHook((MergeRequestHookClient)hook);
            else if (hook.HookType == Core.GitLab.ProjectHookType.build)
                ProcessBuildHook((BuildHookClient)hook);
        }
        void ProcessBuildHook(BuildHookClient hook) {
            var selectedBranch = Repositories.SelectedBranch;
            var mergeRequest = selectedBranch?.MergeRequest;
            if (mergeRequest == null)
                return;
            if (mergeRequest.SourceBranch != hook.Branch)
                return;
            if (selectedBranch.Repository.Origin.Id != hook.ProjectId)
                return;
            selectedBranch.RefreshMergeRequest();
            RepositoriesViewModel.RaiseRefreshSelectedBranch();

            //if (hook.Status == BuildStatus.success) {
            //    var mergeRequest = CalcMergeRequest(gitLabWrapper, hook, project);
            //    if (mergeRequest == null) {
            //        Log.Message("Can`t find merge request.");
            //        return;
            //    }
            //    if (mergeRequest.State == "opened" || mergeRequest.State == "reopened") {
            //        var latestCommit = gitLabWrapper.GetMergeRequestCommits(mergeRequest).FirstOrDefault();
            //        if (latestCommit == null) {
            //            Log.Message("Wrong merge request found.");
            //            return;
            //        }
            //        if (!latestCommit.Id.Equals(hook.Commit.Id)) {
            //            Log.Message("Additional commits has been added.");
            //            return;
            //        }

            //        var xmlComments = gitLabWrapper.GetComments(mergeRequest).Where(x => IsXml(x.Note));
            //        var options = xmlComments.Select(x => MergeRequestOptions.ConvertFromString(x.Note)).FirstOrDefault();
            //        if (options?.ActionType == MergeRequestActionType.sync) {
            //            Log.Message("Sync options found.");
            //            var syncOptions = (MergeRequestSyncAction)options.Action;
            //            Log.Message($"Sync options perform testing is {syncOptions.PerformTesting}");
            //            Log.Message($"Sync options assign to service is {syncOptions.AssignToSyncService}");
            //            Log.Message($"Sync options sync task is {syncOptions.SyncTask}");
            //            Log.Message($"Sync options sync service is {syncOptions.SyncService}");
            //            if (syncOptions.PerformTesting && syncOptions.AssignToSyncService) {
            //                gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, syncOptions.SyncService);
            //                ForceBuild(syncOptions.SyncTask);
            //            }
            //            return;
            //        }
            //        Log.Message("Sync options not found.");
            //    }
            //}

        }
        void ProcessMergeRequestHook(MergeRequestHookClient hook) {
            int mergeRequestId = hook.Attributes.Id;
            var selectedBranch = Repositories.SelectedBranch;
            var mergeRequest = selectedBranch?.MergeRequest;
            if (mergeRequest?.MergeRequestId == mergeRequestId) {
                selectedBranch.RefreshMergeRequest();
                RepositoriesViewModel.RaiseRefreshSelectedBranch();
                Log.Message("Selected branch refreshed.");
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
