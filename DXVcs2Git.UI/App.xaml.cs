using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using CommandLine;
using DevExpress.Xpf.Core;
using DXVcs2Git.UI.Farm;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public Options Options { get; private set; }
        public static RootViewModel RootModel { get; private set; }
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            ThemeManager.ApplicationThemeName = "Office2013";

            var result = Parser.Default.ParseArguments<Options>(e.Args);
            var hasErrors = result.MapResult(clo => {
                Options = clo;
                return 0;
            },
            errors => 1);
            if (hasErrors != 0)
                Environment.Exit(hasErrors);

            RootModel = new RootViewModel(Options);
            RootModel.Initialize();
            FarmIntegrator.Start(Dispatcher, RootModel.Refresh);
        }
    }
}
