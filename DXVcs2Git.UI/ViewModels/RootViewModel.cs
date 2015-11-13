using DevExpress.Mvvm;
using DXVcs2Git.Git;
using DXVcs2Git.UI.ViewModels;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : BindableBase {
        public MergeRequestsViewModel MergeRequests { get; private set; }
        public Options Options { get; private set; }
        public RootViewModel(Options options) {
            Options = options;
        }

        public void Initialize() {
            GitLabWrapper wrapper = new GitLabWrapper(Options.gitServer, Options.Token);
            //MergeRequests = new MergeRequestsViewModel();
        }
    }
}
