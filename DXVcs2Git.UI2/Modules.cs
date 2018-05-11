using DXVcs2Git.UI2.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

namespace DXVcs2Git.UI2 {
    public class ModuleInjector : IModule {
        readonly IRegionManager regionManager;
        public ModuleInjector(IRegionManager regionManager) {
            this.regionManager = regionManager;
        }
        public void RegisterTypes(IContainerRegistry containerRegistry) {
        }
        public void OnInitialized(IContainerProvider containerProvider) {
            this.regionManager.RegisterViewWithRegion(Regions.Main, typeof(MainView));
            this.regionManager.RegisterViewWithRegion(Regions.Repositories, typeof(RepositoriesView));
        }
    }
}