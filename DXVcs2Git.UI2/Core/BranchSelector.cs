using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DXVcs2Git.UI2.Views;
using Prism.Regions;

namespace DXVcs2Git.UI2.Core {
    public interface IBranchSelector {
        IObservable<IBranchModel> SelectedBranchObservable { get; }
        void Initialize();
        void Select(IBranchModel branch);
    }

    public class BranchSelector : IBranchSelector {
        readonly IRegionManager regionManager;
        readonly BehaviorSubject<IBranchModel> selectedBranchSubject = new BehaviorSubject<IBranchModel>(null);

        public IObservable<IBranchModel> SelectedBranchObservable => this.selectedBranchSubject.AsObservable();

        public BranchSelector(IRegionManager regionManager) {
            this.regionManager = regionManager;
        }
        
        public void Initialize() {
        }

        public void Select(IBranchModel selectedBranch) {
            this.selectedBranchSubject.OnNext(selectedBranch);
            //this.regionManager.AddToRegion(Regions.SelectedBranch, new BranchView());
            this.regionManager.Navigate(Regions.SelectedBranch, selectedBranch != null ? typeof(BranchView) : typeof(EmptyBranchView));
        }
    }
}