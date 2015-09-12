using System;
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
            InitEmptyRepo();
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
        void InitEmptyRepo() {
            if (repo.Branches.Any())
                return;
            File.WriteAllText(Path.Combine(path, ".gitignore"), string.Empty);
            Stage("*");
            Commit("initialize", "exmachina", new DateTime(2001, 1, 1));
            Push("master");
        }
        public void Dispose() {
        }
        public void Fetch() {
            FetchOptions fetchOptions = new FetchOptions();
            fetchOptions.CredentialsProvider += (url, fromUrl, types) => credentials;
            var network = repo.Network.Remotes.First();
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
            Remote remote = this.repo.Network.Remotes.First();
            repo.Network.Push(remote, $@"refs/heads/{branch}", options);
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
            }
            InitializePush(localBranch);
        }
        void InitializePush(Branch localBranch) {
            Remote remote = this.repo.Network.Remotes["origin"];
            this.repo.Branches.Update(localBranch,
                b => b.Remote = remote.Name,
                b => b.UpstreamBranch = localBranch.CanonicalName);
        }
        public Commit FindCommit(string branchName, DateTime timeStamp) {
            var branch = repo.Branches[branchName];
            return branch.Commits.FirstOrDefault(x => x.Author.When == timeStamp);
        }
        public DateTime CalcLastCommitDate(string branchName, string user) {
            var branch = repo.Branches[branchName];
            var commit = branch.Commits.LastOrDefault(x => x.Author.Name == user);
            return commit?.Author.When.DateTime ?? DateTime.MinValue;
        }
    }
}
