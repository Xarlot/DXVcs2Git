using System;
using Prism.Regions;

namespace DXVcs2Git.UI2 {
    public static class RegionManagerExtensions {
        public static void Navigate(this IRegionManager regionManager, string regionName, Type type) {
            regionManager.Regions[regionName].RemoveAll();
            regionManager.RegisterViewWithRegion(regionName, type);
            regionManager.RequestNavigate(regionName, new Uri(type.Name, UriKind.Relative));
        }
    }
}