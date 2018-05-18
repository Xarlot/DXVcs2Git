using DXVcs2Git.UI2.Core;
using DXVcs2Git.UI2.ViewModels;
using DXVcs2Git.UI2.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

namespace DXVcs2Git.UI2 {
    public class ModuleInjector : IModule {
        readonly IRegionManager regionManager;
        public ModuleInjector(IRegionManager regionManager) {
            this.regionManager = regionManager;
            this.regionManager.RegisterViewWithRegion(Regions.Main, typeof(MainView));
            this.regionManager.RegisterViewWithRegion(Regions.Repositories, typeof(RepositoriesView));
            this.regionManager.RegisterViewWithRegion(Regions.SelectedBranch, typeof(EmptyBranchView));
            this.regionManager.RegisterViewWithRegion(Regions.SelectedBranch, typeof(BranchView));
        }
        public void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterSingleton(typeof(IRepositoriesStorage), typeof(RepositoriesStorage));
            containerRegistry.RegisterSingleton(typeof(IBranchSelector), typeof(BranchSelector));
            containerRegistry.RegisterSingleton(typeof(ISettings), typeof(Settings));
            containerRegistry.Register(typeof(BranchViewModel));
        }
        public void OnInitialized(IContainerProvider containerProvider) {
        }
    }
}