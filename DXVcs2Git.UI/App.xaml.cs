using System;
using System.Windows;
using CommandLine;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public Options Options { get; private set; }
        public RootViewModel RootModel { get; private set; }
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            var result = Parser.Default.ParseArguments<Options>(e.Args);
            var hasErrors = result.MapResult(clo => {
                Options = clo;
                return 0;
            },
            errors => 1);
            if (hasErrors != 0)
                Environment.Exit(hasErrors);
            RootModel = new RootViewModel(Options);
        }
    }
}
