using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using DXVcs2Git.Core;
using LibGit2Sharp;

namespace DXVcs2Git {
    public class GitWrapper : IDisposable {
        readonly string path;
        readonly Credentials credentials;
        readonly string repoPath;
        readonly string gitPath;
        readonly Repository repo;
        public bool IsEmpty {
            get { return !repo.Branches.Any(); }
        }
        public string GitDirectory {
            get { return repoPath; }
        }
        public Credentials Credentials {
            get { return credentials; }
        }

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
            var network = repo.Network.Remotes.FirstOrDefault();
            repo.Fetch(network.Name, fetchOptions);
        }
        public void Stage(string path) {
            repo.Stage(path);
            Log.Message($"Git stage performed.");
        }
        public void Commit(string comment, string user, string committerName, DateTime timeStamp, bool allowEmpty = true) {
            CommitOptions commitOptions = new CommitOptions();
            commitOptions.AllowEmptyCommit = allowEmpty;
            DateTime localTime = timeStamp.ToLocalTime();
            var author = new Signature(user, "test@mail.com", localTime);
            var comitter = new Signature(committerName, "test@mail.com", localTime);
            repo.Commit(comment, author, comitter, commitOptions);
            Log.Message($"Git commit performed for {user} {localTime}");
        }
        public void Push(string branch) {
            PushOptions options = new PushOptions();
            options.CredentialsProvider += (url, fromUrl, types) => credentials;
            Remote remote = this.repo.Network.Remotes["origin"];
            repo.Network.Push(remote, "HEAD", $@"refs/heads/{branch}", options);
            Log.Message($"Push to branch {branch} completed");
        }
        public void EnsureBranch(string name, Commit whereCreateBranch) {
            Fetch();
            Branch localBranch = this.repo.Branches[name];
            if (localBranch == null) {
                Branch remoteBranch = this.repo.Branches[GetOriginName(name)];
                if (remoteBranch != null) {
                    InitLocalBranch(name, remoteBranch);
                    return;
                }
                if (whereCreateBranch == null) {
                    repo.CreateBranch(name);
                    Push(name);
                }
                else {
                    CreateBranchFromCommit(name, whereCreateBranch);
                }
            }
        }
        void InitLocalBranch(string name, Branch remoteBranch) {
            this.repo.CreateBranch(name, remoteBranch.CanonicalName);
        }
        void CreateBranchFromCommit(string name, Commit whereCreateBranch) {
            CheckOut(whereCreateBranch);
            repo.CreateBranch(name);
        }
        string GetOriginName(string name) {
            return $"origin/{name}";
        }
        public Commit FindCommit(string branchName, string comment) {
            var branch = repo.Branches[branchName];
            return branch.Commits.FirstOrDefault(x => x.Message?.StartsWith(comment) ?? false);
        }

        public DateTime CalcLastCommitDate(string branchName, string user) {
            var tags = repo.Tags.Where(x => {
                bool isAnnotated = x.IsAnnotated;
                return isAnnotated && x.FriendlyName.ToLowerInvariant().StartsWith("dxvcs2gitservice_sync_");
            });
            if (!tags.Any())
                return GuessLastCommitTime(branchName, user);
            var branchTags = tags.Where(x => x.Annotation.Name.ToLowerInvariant().Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Any(chunk => chunk == branchName));
            if (!branchTags.Any())
                return GuessLastCommitTime(branchName, user);
            var lastSyncTagTicks = branchTags.Max(x => {
                var tagAnnotation = x.Annotation;
                long ticks = Convert.ToInt64(tagAnnotation.Message);
                return ticks;
            });
            if (lastSyncTagTicks > 0)
                return new DateTime(lastSyncTagTicks);
            return GuessLastCommitTime(branchName, user);
        }
        DateTime GuessLastCommitTime(string branchName, string user) {
            var branch = this.repo.Branches[branchName];
            var commit = branch.Commits.FirstOrDefault(x => x.Committer.Name == user);
            return commit?.Author.When.DateTime.ToUniversalTime() ?? DateTime.MinValue;
        }
        DateTime CalcLastCommitTime(string message) {
            long ticks = Convert.ToInt64(message);
            return new DateTime(ticks);
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
        public void CheckOut(Commit commit) {
            CheckoutOptions options = new CheckoutOptions();
            options.CheckoutModifiers = CheckoutModifiers.Force;
            repo.Checkout(commit, options);
            Log.Message($"Checkout commit {commit.Id} completed");
        }
        public void AddTag(string tagName, GitObject target, string commiterName, DateTime timeStamp, string message) {
            repo.Tags.Add(tagName, target, new Signature(commiterName, "test@mail.com", timeStamp), message, true);
            Log.Message($"Apply tag commit {tagName} completed");
        }
        public Commit GetHead(string branchName) {
            var branch = repo.Branches[branchName];
            return branch.Commits.First();
        }
    }
}
