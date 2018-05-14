using System.Windows;
using CommonServiceLocator;
using DevExpress.Xpf.Core;
using DXVcs2Git.UI2.Core;
using DXVcs2Git.UI2.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Unity;

namespace DXVcs2Git.UI2 {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {
        static App() {
            ApplicationThemeHelper.UseLegacyDefaultTheme = true;
            ApplicationThemeHelper.ApplicationThemeName = "Super";
        }

        public App() {
        }
        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterSingleton(typeof(IRepositoriesStorage), typeof(RepositoriesStorage));
            containerRegistry.RegisterSingleton(typeof(ISettings), typeof(Settings));
        }
        protected override Window CreateShell() {
            return new MainWindow();
        }
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule(typeof(ModuleInjector));
        }
        protected override async void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            Container.Resolve<ISettings>().Initialize();
            await Container.Resolve<IRepositoriesStorage>().Initialize();
        }
    }
}