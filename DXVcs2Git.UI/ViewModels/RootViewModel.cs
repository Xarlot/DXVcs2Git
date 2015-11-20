using System.Windows.Input;
using DevExpress.Mvvm;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : BindableBase {
        public MergeRequestsViewModel MergeRequests { get; private set; }
        public RootViewModel() {
            MergeRequests = new MergeRequestsViewModel();
        }
        public void Initialize() {
            MergeRequests.Update();
        }
        public void Refresh() {
            MergeRequests.Refresh();
        }
    }
}
