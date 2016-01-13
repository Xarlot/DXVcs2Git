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

namespace DXVcs2Git.UI {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static RootViewModel RootModel { get; private set; }
        protected override void OnStartup(StartupEventArgs e) {            
            base.OnStartup(e);
            Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            ThemeManager.ApplicationThemeName = "Office2013";
        }
        void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            Log.Error("Unhandled exception, ", (Exception)e.Exception);
            DXMessageBox.Show("Ooooops, some shit happens :(" + Environment.NewLine + "See log for details.", "Unhandled exception", MessageBoxButton.OK);
            e.Handled = true;
        }
    }
}
