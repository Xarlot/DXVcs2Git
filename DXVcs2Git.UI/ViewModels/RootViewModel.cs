using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DXVcs2Git.Core;
using DXVcs2Git.Git;
using DXVcs2Git.UI.ViewModels;
using NGitLab.Models;

namespace DXVcs2Git.UI.ViewModels {
    public class RootViewModel : BindableBase {
        public IEnumerable<MergeRequestViewModel> MergeRequests { get; private set; }
        public Options Options { get; private set; }
        public RootViewModel(Options options) {
            Options = options;
        }

        public void Initialize() {
            GitLabWrapper wrapper = new GitLabWrapper(Options.gitServer, Options.Token);
            Project project = wrapper.FindProject(Options.Repo);
            if (project == null) {
                Log.Error("Can`t find project");
                return;
            }
            var mergeRequests = wrapper.GetMergeRequests(project);
            MergeRequests = mergeRequests.Select(x => new MergeRequestViewModel(x)).ToList();
            //MergeRequests = new MergeRequestsViewModel();
        }
    }
}
