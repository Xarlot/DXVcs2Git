using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CommandLine;
using DevExpress.Xpf.Core;
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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            ThemeManager.ApplicationThemeName = "Office2013";
        }
        void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
            DXMessageBox.Show(e.ExceptionObject.ToString(), "Unhandled exception", MessageBoxButton.OK);
        }
    }
}
