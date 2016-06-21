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
using Microsoft.Win32;
using System.IO;

namespace DXVcs2Git.UI.ViewModels {
    public class EditConfigViewModel : ViewModelBase {
        readonly Config config;
        readonly RepoConfigsReader configsReader;

        public string DefaultTheme {
            get { return GetProperty(() => DefaultTheme); }
            set { SetProperty(() => DefaultTheme, value); }
        }
        public int UpdateDelay {
            get { return GetProperty(() => UpdateDelay); }
            set { SetProperty(() => UpdateDelay, value, OnUpdateDelayChanged); }
        }
        public bool StartWithWindows {
            get { return GetProperty(() => StartWithWindows); }
            set { SetProperty(() => StartWithWindows, value, OnStartWithWindowsChanged); }
        }
        public string KeyGesture {
            get { return GetProperty(() => KeyGesture); }
            set { string oldValue = KeyGesture; SetProperty(() => KeyGesture, value, () => OnKeyGestureChanged(oldValue)); }
        }
        public bool AlwaysSure1 {
            get { return GetProperty(() => AlwaysSure1); }
            set { SetProperty(() => AlwaysSure1, value, () => { AlwaysSure2 &= AlwaysSure1; AlwaysSure3 &= AlwaysSure2; AlwaysSure4 &= AlwaysSure3; }); }
        }
        public bool AlwaysSure2 {
            get { return GetProperty(() => AlwaysSure2); }
            set { SetProperty(() => AlwaysSure2, value, () => { AlwaysSure3 &= AlwaysSure2; AlwaysSure4 &= AlwaysSure3; }); }
        }
        public bool AlwaysSure3 {
            get { return GetProperty(() => AlwaysSure3); }
            set { SetProperty(() => AlwaysSure3, value, () => { AlwaysSure4 &= AlwaysSure3; }); }
        }
        public bool AlwaysSure4 {
            get { return GetProperty(() => AlwaysSure4); }
            set { SetProperty(() => AlwaysSure4, value); }
        }
        public bool CommonXaml {
            get { return GetProperty(() => CommonXaml); }
            set { SetProperty(() => CommonXaml, value); }
        }
        public bool DiagramXaml {
            get { return GetProperty(() => DiagramXaml); }
            set { SetProperty(() => DiagramXaml, value); }
        }
        public bool XPFGITXaml {
            get { return GetProperty(() => XPFGITXaml); }
            set { SetProperty(() => XPFGITXaml, value); }
        }
        public ICommand UpdateWpf2SLProperties { get; private set; }

        async System.Threading.Tasks.Task UpdateCommonXamlProperty() {
            if (CommonXaml)
                await NativeMethods.AdministratorMethods.SetWpf2SlKeyAsync("Common");
            else
                await NativeMethods.AdministratorMethods.ResetWpf2SlKeyAsync("Common");
        }
        async System.Threading.Tasks.Task UpdateDiagramXamlProperty() {
            if (DiagramXaml)
                await NativeMethods.AdministratorMethods.SetWpf2SlKeyAsync("Diagram");
            else
                await NativeMethods.AdministratorMethods.ResetWpf2SlKeyAsync("Diagram");
        }
        async System.Threading.Tasks.Task UpdateXPFGITXamlProperty() {
            if (XPFGITXaml)
                await NativeMethods.AdministratorMethods.SetWpf2SlKeyAsync("XPF");
            else
                await NativeMethods.AdministratorMethods.ResetWpf2SlKeyAsync("XPF");
        }
        bool GetWpf2SlKey(string value) {
            var ev = Environment.GetEnvironmentVariable("wpf2slkey", EnvironmentVariableTarget.Machine);
            return ev?.Contains(value) ?? false;
        }

        public IEnumerable<GitRepoConfig> Configs { get; }
        public ICommand RefreshUpdateCommand { get; private set; }
        public bool HasUIValidationErrors {
            get { return GetProperty(() => HasUIValidationErrors); }
            set { SetProperty(() => HasUIValidationErrors, value); }
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
        void OnUpdateDelayChanged() {
            AtomFeed.FeedWorker.UpdateDelay = UpdateDelay;
            StartWithWindows = GetStartWithWindows();
        }
        const string registryValueName = "DXVcs2Git";
        bool GetStartWithWindows() {
            return Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false).GetValue("DXVcs2Git") != null;
        }
        void OnStartWithWindowsChanged() {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (StartWithWindows) {
                if (rkApp.GetValue(registryValueName) != null)
                    return;
                var config = ConfigSerializer.GetConfig();
                var launcher = Path.Combine(ConfigSerializer.SettingsPath, "DXVcs2Git.Launcher.exe");
                if (File.Exists(launcher))
                    rkApp.SetValue("DXVcs2Git", String.Format("\"{0}\" -h", launcher));
            }
            else {
                if (rkApp.GetValue(registryValueName) == null)
                    return;
                rkApp.DeleteValue(registryValueName);
            }
        }
        void OnKeyGestureChanged(string oldValue) {
            NativeMethods.HotKeyHelper.UnregisterHotKey();
            NativeMethods.HotKeyHelper.RegisterHotKey(KeyGesture);
        }

        public EditConfigViewModel(Config config) {
            this.config = config;
            this.configsReader = new RepoConfigsReader();
            KeyGesture = config.KeyGesture;
            DefaultTheme = config.DefaultTheme;
            Configs = this.configsReader.RegisteredConfigs;
            UpdateDelay = AtomFeed.FeedWorker.UpdateDelay;
            RefreshUpdateCommand = DelegateCommandFactory.Create(AtomFeed.FeedWorker.Update);
            CommonXaml = GetWpf2SlKey("Common");
            DiagramXaml = GetWpf2SlKey("Diagram");
            XPFGITXaml = GetWpf2SlKey("XPF");
            Repositories = CreateEditRepositories(config);
            Repositories.CollectionChanged += RepositoriesOnCollectionChanged;
            AlwaysSure4 = AlwaysSure3 = AlwaysSure2 = AlwaysSure1 = config.AlwaysSure;
            UpdateWpf2SLProperties = new AsyncCommand(OnUpdateWpf2SLProperties);
            UpdateTokens();
        }
        async System.Threading.Tasks.Task OnUpdateWpf2SLProperties() {
            await UpdateCommonXamlProperty();
            await UpdateDiagramXamlProperty();
            await UpdateXPFGITXamlProperty();
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
                RepoConfig = repo.ConfigName != null ? this.configsReader[repo.ConfigName] : null,
                Token = repo.Token,
                LocalPath = repo.LocalPath,
            };
        }
        public void UpdateConfig() {
            config.Repositories = Repositories.With(x => x.Select(repo => new TrackRepository() { Name = repo.Name, ConfigName = repo.ConfigName, LocalPath = repo.LocalPath, Server = repo.RepoConfig.Server, Token = repo.Token }).ToArray());
            config.UpdateDelay = UpdateDelay;
            config.KeyGesture = KeyGesture;
            config.AlwaysSure = AlwaysSure4;
            this.config.DefaultTheme = DefaultTheme;
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
