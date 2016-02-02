using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CommandLine;
using DevExpress.Xpf.Core;
using DXVcs2Git.Core;
using DXVcs2Git.UI.Farm;
using DXVcs2Git.UI.ViewModels;
using System.Windows.Interop;

namespace DXVcs2Git.UI {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static RootViewModel RootModel { get; private set; }
        public static UIStartupOptions StartupOptions { get; private set; }
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            StartupOptions = Parser.Default.ParseArguments<UIStartupOptions>(e.Args).MapResult(x => x, x => UIStartupOptions.GenerateDefault());
            Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            RunWindow();
        }

        void RunWindow() {
            MainWindow = new MainWindow();
            MainWindow.WindowState = StartupOptions.State;
            if (StartupOptions.Hidden)
                return;
            MainWindow.Show();
        }

        void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            Log.Error("Unhandled exception, ", (Exception)e.Exception);
            DXMessageBox.Show("Ooooops, some shit happens :(" + Environment.NewLine + "See log for details.", "Unhandled exception", MessageBoxButton.OK);
            e.Handled = true;
        }
    }
}
