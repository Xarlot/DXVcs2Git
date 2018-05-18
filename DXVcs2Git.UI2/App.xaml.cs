using System.Windows;
using CommonServiceLocator;
using DevExpress.Xpf.Core;
using DXVcs2Git.UI2.Core;
using DXVcs2Git.UI2.ViewModels;
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
            Container.Resolve<IBranchSelector>().Initialize();
            Container.Resolve<IRegionManager>().RequestNavigate(Regions.SelectedBranch, nameof(EmptyBranchView));
            Container.Resolve<ISettings>().Initialize();

            await Container.Resolve<IRepositoriesStorage>().Initialize();
        }
    }
}