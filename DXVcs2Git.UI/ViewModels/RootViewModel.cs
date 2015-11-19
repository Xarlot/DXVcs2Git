using DevExpress.Mvvm;
using DXVcs2Git.Core.Git;
using DXVcs2Git.Git;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : BindableBase {
        public MergeRequestsViewModel MergeRequests { get; private set; }
        public Options Options { get; private set; }
        public GitRepoConfig RepoConfig { get; private set; }
        public RootViewModel(Options options) {
            Options = options;
        }

        public void Initialize() {
            string gitDir = Options.LocalFolder;
            GitReaderWrapper gitReader = new GitReaderWrapper(gitDir);
            GitLabWrapper wrapper = new GitLabWrapper(Options.GitServer, Options.Token);
            MergeRequests = new MergeRequestsViewModel(wrapper, gitReader);
        }
    }
}
