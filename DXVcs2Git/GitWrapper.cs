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
        public string GitDirectory {
            get { return repoPath; }
        }

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
        public void Stage(string path) {
            repo.Stage(path);
        }
        public void Commit(string comment, string user, DateTime timeStamp) {
            CommitOptions commitOptions = new CommitOptions();
            commitOptions.AllowEmptyCommit = true;
            var author = new Signature(user, "test@mail.com", timeStamp);
            repo.Commit(comment, author, author, commitOptions);
            Log.Message($"Git commit performed for {user} {timeStamp}");
        }
        public void Push(string branch) {
            PushOptions options = new PushOptions();
            options.CredentialsProvider += (url, fromUrl, types) => credentials;
            repo.Network.Push(repo.Branches[branch], options);
            Log.Message($"Push to branch {branch} completed");
        }
        public void EnsureBranch(string name, string targetName) {
            if (repo.Branches[name] != null)
                return;
            var branch = repo.CreateBranch(name);
        }
    }
}
