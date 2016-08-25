using System;
using System.Windows;
using System.Windows.Threading;
using CommandLine;
using DevExpress.Logify.WPF;
using DevExpress.Xpf.Core;
using DXVcs2Git.Core;
using LibGit2Sharp;

namespace DXVcs2Git.UI {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static UIStartupOptions StartupOptions { get; private set; }
        LogifyClient logifyClient;
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            DevExpress.Data.ShellHelper.TryCreateShortcut("dxvcs2git.ui",  "dxvcs2git.ui");

            DefaultInitializer.Initialize();

            StartupOptions = Parser.Default.ParseArguments<UIStartupOptions>(e.Args).MapResult(x => x, x => UIStartupOptions.GenerateDefault());

            logifyClient = new LogifyClient();
            logifyClient.ApiKey = "9F13F4F0568643A3BCAE34E9B0C4A1B1";
            logifyClient.Run();
            logifyClient.ConfirmSendReport = true;
            Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            RunWindow();
        }

        void RunWindow() {
            MainWindow = new RootWindow();
            MainWindow.WindowState = StartupOptions.State;
            if (StartupOptions.Hidden)
                return;
            MainWindow.Show();
        }

        void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            Log.Error("Unhandled exception, ", (Exception)e.Exception);
            DXMessageBox.Show("Ooooops, some funny shit happens :(" + Environment.NewLine + "See log for details.", "Unhandled exception", MessageBoxButton.OK);
            e.Handled = true;
        }
    }
}
