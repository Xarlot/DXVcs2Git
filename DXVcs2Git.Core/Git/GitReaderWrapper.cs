using System.Linq;
using LibGit2Sharp;

namespace DXVcs2Git.Core.Git {
    public class GitReaderWrapper {
        readonly Repository repo;
        public GitReaderWrapper(string repoPath) {
            this.repo = new Repository(repoPath);
        }
        public string GetRemoteRepoPath() {
            var remote = this.repo.Network.Remotes.FirstOrDefault();
            return remote?.PushUrl;
        }
        public Branch GetCheckoutBranch() {
            return this.repo.Head;
        }
    }
}
