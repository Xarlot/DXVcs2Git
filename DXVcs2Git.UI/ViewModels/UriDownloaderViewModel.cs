using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DXVcs2Git.Core.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace DXVcs2Git.UI.ViewModels {
    public enum UpdaterStatus {
        Error = -1,
        Initializing = 0,        
        Downloading = 1,
        Installing = 2,
        Restarting = 3,        
    }
    public class UriDownloaderViewModel : ViewModelBase {
        readonly Uri uri;
        readonly Version version;
        readonly WebClient client;
        Process vsixInstallerProcess;
        UpdaterStatus status;
        int progress;
        int overallProgress;
        public int OverallProgress {
            get { return overallProgress; }
            private set { SetProperty(ref overallProgress, value, () => OverallProgress); }
        }
        public int Progress {
            get { return progress; }
            private set { SetProperty(ref progress, value, () => Progress); }
        }
        public UpdaterStatus Status {
            get { return status; }
            private set { SetProperty(ref status, value, () => Status, OnStatusChanged); }
        }
        public UICommand RestartCommand { get; private set; }
        public UICommand CancelCommand { get; private set; }

        void OnStatusChanged() {
            OverallProgress = (int)Status;
            if (Status == UpdaterStatus.Installing)
                BeginInstall();
        }        
        public ICommand StartDownloadCommand { get; private set; }

        public UriDownloaderViewModel(Uri uri, Version version) {
            this.uri = uri;
            this.version = version;
            Status = UpdaterStatus.Initializing;
            StartDownloadCommand = DelegateCommandFactory.Create(StartDownload);
            RestartCommand = new UICommand(new object(), "Restart", DelegateCommandFactory.Create(new Action(Restart), new Func<bool>(CanRestart)), true, false);
            CancelCommand = new UICommand(new object(), "Cancel", DelegateCommandFactory.Create(new Action(Cancel), new Func<bool>(CanCancel)), false, true);
            client = new WebClient();
            client.DownloadProgressChanged += OnDownloadProgressChanged;
            client.DownloadDataCompleted += DownloadDataCompleted;
        }

        bool CanCancel() { return Status == UpdaterStatus.Downloading || Status == UpdaterStatus.Error; }
        void Cancel() { if (Status == UpdaterStatus.Downloading) client.CancelAsync(); }
        bool CanRestart() { return Status == UpdaterStatus.Restarting; }
        void Restart() { if(LauncherHelper.StartLauncher(-1)) Application.Current.Shutdown(); }  

        void StartDownload() {            
            try {
                client.DownloadDataAsync(uri);
            } catch {
                Status = UpdaterStatus.Error;
            }
        }

        void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e) {            
            if (e.Error!=null)
                Status = UpdaterStatus.Error;
            else {
                var targetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "installer_DXVcs2Git.GitTools.vsix");
                if (File.Exists(targetPath))
                    File.Delete(targetPath);
                File.WriteAllBytes(targetPath, e.Result);
                Status = UpdaterStatus.Installing;
            }                
        }
        void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            Status = UpdaterStatus.Downloading;
            Progress = e.ProgressPercentage;
        }
        void BeginInstall() {
            var vsToolspath = Environment.ExpandEnvironmentVariables("%VS140COMNTOOLS%");
            var binpath = Path.GetFullPath(Path.Combine(vsToolspath, @"..\IDE\VSIXInstaller.exe"));
            var vsixpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "installer_DXVcs2Git.GitTools.vsix");
            vsixInstallerProcess = new Process();
            vsixInstallerProcess.StartInfo = new ProcessStartInfo(binpath, String.Format("{0} \"{1}\"", "/q", vsixpath));
            vsixInstallerProcess.Exited += VsixInstalCompleted;
            vsixInstallerProcess.EnableRaisingEvents = true;
            vsixInstallerProcess.Start();            
        }

        void VsixInstalCompleted(object sender, EventArgs e) {
            InstallCompleted();
        }

        void InstallCompleted() {
            vsixInstallerProcess.Exited -= VsixInstalCompleted;
            var hasError = vsixInstallerProcess.ExitCode != 0;
            vsixInstallerProcess.Dispose();
            if (hasError) {
                Status = UpdaterStatus.Error;
                return;
            }
            FindAndInstallExtension();
        }

        void FindAndInstallExtension() {
            var vsExtensionspath = Path.GetFullPath(Path.Combine(Environment.ExpandEnvironmentVariables("%VS140COMNTOOLS%"), @"..\IDE\Extensions\"));
            var userExtensionspath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\VisualStudio\14.0\Extensions\"));
            string path;
            if(!FindExtensionFolder(userExtensionspath, out path) && !FindExtensionFolder(vsExtensionspath, out path)) {
                Status = UpdaterStatus.Error;
                return;
            }
            if (!Directory.Exists(path)) {
                Status = UpdaterStatus.Error;
                return;
            }
            var config = ConfigSerializer.GetConfig();
            config.InstallPath = path;
            ConfigSerializer.SaveConfig(config);
            if (LauncherHelper.UpdateLauncher(version: version))
                Status = UpdaterStatus.Restarting;
            else
                Status = UpdaterStatus.Error;
        }

        bool FindExtensionFolder(string rootpath, out string path) {
            path = null;
            foreach(var file in Directory.EnumerateFiles(rootpath, "extension.vsixmanifest", SearchOption.AllDirectories)) {
                var reader = XDocument.Load(file);
                var identity = reader.Descendants(XName.Get("Identity", "http://schemas.microsoft.com/developer/vsx-schema/2011")).FirstOrDefault();
                if (identity == null)
                    continue;
                var id = identity.Attribute(XName.Get("Id")).Value;
                if (id != AtomFeed.FeedWorker.VSIXId)
                    continue;
                var version = Version.Parse(identity.Attribute(XName.Get("Version")).Value);
                if (version == this.version) {
                    path = Path.GetDirectoryName(file);
                    return true;
                }
            }
            return false;
        }
    }
}
