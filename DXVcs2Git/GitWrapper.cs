using System;
using System.Collections.Generic;
using System.IO;
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
        public bool IsEmpty { get { return !repo.Branches.Any(); } }
        public string GitDirectory {
            get { return repoPath; }
        }
        public Credentials Credentials { get { return credentials; } }

        public GitWrapper(string path, string gitPath, Credentials credentials) {
            this.path = path;
            this.credentials = credentials;
            this.gitPath = gitPath;
            this.repoPath = DirectoryHelper.IsGitDir(path) ? GitInit() : GitClone();
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
            FetchOptions fetchOptions = new FetchOptions();
            fetchOptions.CredentialsProvider += (url, fromUrl, types) => credentials;
            var network = repo.Network.Remotes.First();
            repo.Fetch(network.Name, fetchOptions);
        }
        public void Stage(string path) {
            repo.Stage(path);
        }
        public void Commit(string comment, string user, string committerName, DateTime timeStamp) {
            CommitOptions commitOptions = new CommitOptions();
            commitOptions.AllowEmptyCommit = true;
            var author = new Signature(user, "test@mail.com", timeStamp);
            var comitter = new Signature(committerName, "test@mail.com", timeStamp);
            repo.Commit(comment, author, comitter, commitOptions);
            Log.Message($"Git commit performed for {user} {timeStamp}");
        }
        public void Push(string branch) {
            PushOptions options = new PushOptions();
            options.CredentialsProvider += (url, fromUrl, types) => credentials;
            Remote remote = this.repo.Network.Remotes["origin"];
            repo.Network.Push(remote, "HEAD", $@"refs/heads/{branch}", options);
            Log.Message($"Push to branch {branch} completed");
        }
        public void EnsureBranch(string name, Commit whereCreateBranch) {
            Branch localBranch = this.repo.Branches[name];
            if (localBranch == null) {
                if (whereCreateBranch == null)
                    localBranch = repo.CreateBranch(name);
                else {
                    localBranch = repo.CreateBranch(name, whereCreateBranch);
                }
                Push(name);
            }
        }
        void InitializePush(Branch localBranch) {
            Remote remote = this.repo.Network.Remotes["origin"];
            this.repo.Branches.Update(localBranch,
                b => b.Remote = remote.Name,
                b => b.UpstreamBranch = localBranch.CanonicalName);
        }
        public Commit FindCommit(string branchName, string comment) {
            var branch = repo.Branches[branchName];

            return branch.Commits.FirstOrDefault(x => x.Message == comment);
        }

        public DateTime CalcLastCommitDate(string branchName, string user) {
            var branch = repo.Branches["origin/" + branchName];
            var commit = branch.Commits.FirstOrDefault(x => x.Committer.Name == user);
            return commit?.Author.When.DateTime ?? DateTime.MinValue;
        }
        public bool CalcHasModification() {
            RepositoryStatus status = repo.RetrieveStatus();
            return status.IsDirty;
        }
        public void CheckOut(string name) {
            CheckoutOptions options = new CheckoutOptions();
            options.CheckoutModifiers = CheckoutModifiers.Force;
            repo.Checkout(repo.Branches[name], options);
            Log.Message($"Checkout branch {name} completed");
        }
    }
}
