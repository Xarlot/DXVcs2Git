using Prism.Regions;
using ReactiveUI;


namespace DXVcs2Git.UI2.ViewModels {
    public class EmptyBranchViewModel : ReactiveObject {
        public EmptyBranchViewModel() {
            
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