using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using System;

namespace DXVcs2Git.Core.Git {
    public class GitReaderWrapper {
        readonly string localRepoPath;
        readonly Repository repo;
        public GitReaderWrapper(string repoPath) {
            this.localRepoPath = repoPath;
            this.repo = new Repository(repoPath);
        }
        Remote GetRemoteByName(string name) {
            return repo.Network.Remotes.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }
        public string GetOriginRepoPath() {
            var remote = GetRemoteByName("origin") ?? repo.Network.Remotes.FirstOrDefault();
            return remote?.PushUrl;
        }
        public string GetUpstreamRepoPath() {
            var remote = GetRemoteByName("upstream") ?? repo.Network.Remotes.LastOrDefault();
            return remote?.PushUrl;
        }
        public Branch GetCheckoutBranch() {
            return this.repo.Head;
        }
        public IEnumerable<Branch> GetLocalBranches() {
            return this.repo.Branches;
        }
        public string GetLocalRepoPath() {
            return this.localRepoPath;
        }
    }
}
