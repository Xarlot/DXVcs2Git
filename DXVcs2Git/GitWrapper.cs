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
        public void Fetch(bool updateTags = false) {
            FetchOptions options = new FetchOptions();
            options.CredentialsProvider += (url, fromUrl, types) => credentials;
            if (updateTags)
                options.TagFetchMode = TagFetchMode.All;
            var network = repo.Network.Remotes.FirstOrDefault();
            repo.Fetch(network.Name, options);
        }
        public void Stage(string path) {
            repo.Stage(path);
            Log.Message($"Git stage performed.");
        }
        public Commit Commit(string comment, string user, string committerName, DateTime timeStamp, bool allowEmpty = true) {
            CommitOptions commitOptions = new CommitOptions();
            commitOptions.AllowEmptyCommit = allowEmpty;
            DateTime localTime = timeStamp.ToLocalTime();
            var author = new Signature(user, "test@mail.com", localTime);
            var comitter = new Signature(committerName, "test@mail.com", localTime);
            var commit = repo.Commit(comment, author, comitter, commitOptions);
            Log.Message($"Git commit performed for {user} {localTime}");
            return commit;
        }
        public void Push(string branch) {
            Push($@"refs/heads/{branch}", false);
        }
        public void Push(string refspec, bool force) {
            PushOptions options = new PushOptions();
            options.CredentialsProvider += (url, fromUrl, types) => credentials;
            options.OnPushStatusError += errors => {
                Log.Message($"Push to refspec {refspec} failed.");
                Log.Error($"Error: {errors.Message} in repo {errors.Reference}.");
                throw new ArgumentException("error while push");
            };
            Remote remote = this.repo.Network.Remotes["origin"];
            repo.Network.Push(remote, force ? $@"+{refspec}" : refspec, refspec, options);
            Log.Message($"Push to refspec {refspec} completed.");
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
        public Commit FindCommit(string branchName, Func<Commit, bool> handler = null) {
            var branch = repo.Branches[branchName];
            var checkHandler = handler ?? (x => true);
            return branch.Commits.FirstOrDefault(checkHandler);
        }
        public Commit FindCommit(string branchName, string comment) {
            return FindCommit(branchName, x => x.Message?.StartsWith(comment) ?? false);
        }
        public DateTime CalcLastCommitDate(string branchName, string user) {
            var tags = repo.Tags.Where(x => {
                bool isAnnotated = x.IsAnnotated;
                return isAnnotated && x.FriendlyName.ToLowerInvariant().StartsWith("dxvcs2gitservice_sync_");
            });
            if (!tags.Any())
                return GetLastCommitTime(branchName, user);
            var branchTags = tags.Where(x => x.Annotation.Name.ToLowerInvariant().Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Any(chunk => chunk == branchName));
            if (!branchTags.Any()) {
                return GetLastCommitTime(branchName, user);
            }
            var lastSyncTagTicks = branchTags.Max(x => {
                var tagAnnotation = x.Annotation;
                DateTime dt = Convert.ToDateTime(tagAnnotation.Message);
                return dt.Ticks;
            });
            if (lastSyncTagTicks > 0)
                return new DateTime(lastSyncTagTicks);
            return GetLastCommitTime(branchName, user);
        }
        DateTime GetLastCommitTime(string branchName, string user) {
            var timeStamp = GetLastCommitTimeStamp(branchName, user);
            if (timeStamp != null)
                return timeStamp.Value;
            return GuessLastCommitTime(branchName, user);
        }
        DateTime? GetLastCommitTimeStamp(string branchName, string user) {
            var branch = this.repo.Branches[branchName];
            var commit = branch.Commits.FirstOrDefault();
            if (commit.Author.Name == user)
                return GetCommitTimeStampFromComment(commit);
            return null;
        }
        DateTime? GetCommitTimeStampFromComment(Commit commit) {
            var chunks = commit.Message.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var autoSync = chunks.FirstOrDefault(x => x.StartsWith("AutoSync: "));
            if (autoSync == null)
                return null;
            return Convert.ToDateTime(autoSync.Remove(0, "AutoSync: ".Length));
        }
        DateTime GuessLastCommitTime(string branchName, string user) {
            var branch = this.repo.Branches[branchName];
            var commit = branch.Commits.FirstOrDefault(x => x.Committer.Name == user || x.Committer.Name == "Administrator");
            return commit?.Author.When.DateTime.ToUniversalTime() ?? DateTime.MinValue;
        }
        public bool CalcHasModification() {
            RepositoryStatus status = repo.RetrieveStatus();
            return status.IsDirty;
        }
        public void CheckOut(string branchName) {
            CheckoutOptions options = new CheckoutOptions();
            options.CheckoutModifiers = CheckoutModifiers.Force;
            repo.Checkout(repo.Branches[branchName], options);
            Log.Message($"Checkout branch {branchName} completed");
        }
        public void CheckOut(Commit commit) {
            CheckoutOptions options = new CheckoutOptions();
            options.CheckoutModifiers = CheckoutModifiers.Force;
            repo.Checkout(commit, options);
            Log.Message($"Checkout commit {commit.Id} completed");
        }
        public void AddTag(string tagName, GitObject target, string commiterName, DateTime timeStamp, string message) {
            repo.Tags.Add(tagName, target, new Signature(commiterName, "test@mail.com", timeStamp), message, true);
            Push($@"refs/tags/{tagName}", true);
            Log.Message($"Apply tag commit {tagName} completed");
        }
        public Tag GetTag(string tagName) {
            return repo.Tags[tagName];
        }
        public Commit GetHead(string branchName) {
            var branch = repo.Branches[branchName];
            return branch.Commits.First();
        }
        public IEnumerable<TreeEntryChanges> GetChanges(Commit commit, Commit parent) {
            var treeChanges = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
            var changes = Enumerable.Empty<TreeEntryChanges>();
            changes = changes.Concat(treeChanges.Added);
            changes = changes.Concat(treeChanges.Deleted);
            changes = changes.Concat(treeChanges.Modified);
            changes = changes.Concat(treeChanges.Renamed);
            return changes;
        }
        public void Reset(Commit commit) {
            repo.Reset(ResetMode.Hard, commit);
        }
    }
}
