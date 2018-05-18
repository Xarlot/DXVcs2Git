using Prism.Regions;
using ReactiveUI;

namespace DXVcs2Git.UI2.ViewModels {
    public class BranchViewModel : ReactiveObject {
        public BranchViewModel() {
            
        }
        public void OnNavigatedTo(NavigationContext navigationContext) {
        }
        public bool IsNavigationTarget(NavigationContext navigationContext) {
            return false;
        }
        public void OnNavigatedFrom(NavigationContext navigationContext) {
        }
    }
}