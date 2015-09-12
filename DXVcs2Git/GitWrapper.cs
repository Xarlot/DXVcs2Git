using System;
using System.Linq;
using DXVcs2Git.Core;
using LibGit2Sharp;

namespace DXVcs2Git {
    public class GitWrapper : IDisposable {
        readonly string path;
        readonly Credentials credentials;
        readonly string repoPath;
        readonly string gitPath;
        readonly Repository repo;
        public GitWrapper(string path, string gitPath, Credentials credentials) {
            this.path = path;
            this.credentials = credentials;
            this.gitPath = gitPath;
            this.repoPath = DirectoryHelper.IsGitDir(path) ? GitClone() : GitInit();
            repo = new Repository(repoPath);
        }
        public string GitInit() {
            return Repository.Init(path);
        }
        string GitClone() {
            CloneOptions options = new CloneOptions();
            options.CredentialsProvider += (url, fromUrl, types) => credentials;
            string clonedRepoPath = Repository.Clone(gitPath, path, options);
            Log.Message($"Git repo {clonedRepoPath} initialized");
            return clonedRepoPath;
        }
        public void Dispose() {
        }
        public void Fetch() {
            var network = repo.Network.Remotes.First();
            FetchOptions fetchOptions = new FetchOptions();
            fetchOptions.CredentialsProvider += (url, fromUrl, types) => credentials;
            repo.Fetch(network.Name, fetchOptions);
            Log.Message("Git fetch performed");
        }
    }
}
