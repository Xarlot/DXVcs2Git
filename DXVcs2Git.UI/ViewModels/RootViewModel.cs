using System.Windows.Input;
using DevExpress.Mvvm;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : BindableBase {
        public MergeRequestsViewModel MergeRequests { get; private set; }
        public Config Config { get; private set; }
        public RootViewModel() {
            Config = ConfigSerializer.GetConfig();
            MergeRequests = new MergeRequestsViewModel();
        }
        public void Refresh() {
            MergeRequests.Refresh();
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
