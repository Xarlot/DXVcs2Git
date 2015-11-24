using System;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DXVcs2Git.UI.Farm;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : ViewModelBase {
        public MergeRequestsViewModel MergeRequests { get; private set; }
        public RootViewModel() {
            MergeRequests = new MergeRequestsViewModel();
            ISupportParentViewModel supportParent = MergeRequests;
            supportParent.ParentViewModel = this;
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(Initialize));
            FarmIntegrator.Start(Dispatcher.CurrentDispatcher, Refresh);
        }
        public void Initialize() {
            MergeRequests.Update();
        }
        public void Refresh() {
            MergeRequests.Refresh();
        }
    }
}
