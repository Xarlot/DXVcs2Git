using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DXVcs2Git.Core;
using DXVcs2Git.Git;
using DXVcs2Git.UI.ViewModels;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : BindableBase {
        public MergeRequestsViewModel MergeRequests { get; private set; }
        public Options Options { get; private set; }
        public RootViewModel(Options options) {
            Options = options;
        }

        public void Initialize() {
            GitLabWrapper wrapper = new GitLabWrapper(Options.GitServer, Options.Token);
            MergeRequests = new MergeRequestsViewModel(wrapper, Options.Repo);
        }
    }
}
