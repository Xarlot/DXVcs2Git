using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Serialization;
using DXVcs2Git.DXVcs;
using DXVcs2Git.Git;
using LibGit2Sharp;
using NGitLab.Models;
using Comment = DXVcs2Git.Core.Comment;
using Commit = LibGit2Sharp.Commit;

namespace DXVcs2Git.Console {
    internal class Program {
        const string gitlabauthtoken = "X6XV2G_ycz_U4pi4m93K";
        const string AutoSyncTimeStampFormat = "{0:M/d/yyyy HH:mm:ss.ffffff}";
        const string AutoSyncAuthor = "autosync author: {0}";
        const string AutoSyncBranch = "autosync branch: {0}";
        const string AutoSyncSha = "autosync commit sha: {0}";
        const string AutoSyncShaSearchString = @"(?<=sha:\s*)[0-9a-f]+";
        const string AutoSyncTimeStamp = "autosync commit timestamp: {0}";
        const string AutoSyncTokenFormat = "autosync token: {0}";
        const string repoPath = "repo";
        const string gitServer = @"http://litvinov-lnx";
        const string vcsServer = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService";
        const string defaultUser = "dxvcs2gitservice";
        const string tagName = "dxvcs2gitservice_sync_{0}";
        const string startSyncTagName = "dxvcs2gitservice_start_sync_{0}";
        const string endSyncTagName = "dxvcs2gitservice_end_sync_{0}";
        const string AutoMergeFailedComment = "autosync merge failed";
        static void Main(string[] args) {
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            var exitCode = result.MapResult(clo => {
                return DoWork(clo);
            },
            errors => 1);
            Environment.Exit(exitCode);
        }

        static int DoWork(CommandLineOptions clo) {
            string localGitDir = clo.LocalFolder != null && Path.IsPathRooted(clo.LocalFolder) ? clo.LocalFolder : Path.Combine(Environment.CurrentDirectory, clo.LocalFolder ?? repoPath);
            EnsureGitDir(localGitDir);

            string gitRepoPath = clo.Repo;
            string username = clo.Login;
            string password = clo.Password;
            WorkMode workMode = clo.WorkMode;
            string branchName = clo.Branch;
            string trackerPath = clo.Tracker;
            DateTime from = clo.From;

            TrackBranch branch = FindBranch(branchName, trackerPath);
            if (branch == null)
                return 1;
            string historyPath = GetVcsSyncHistory(branch.HistoryPath);
            if (historyPath == null)
                return 1;
            SyncHistory history = SyncHistory.Deserialize(historyPath);
            if (history == null)
                return 1;
            SyncHistoryWrapper syncHistory = new SyncHistoryWrapper(history, vcsServer, branch.HistoryPath, historyPath);
            GitWrapper gitWrapper = CreateGitWrapper(gitRepoPath, localGitDir, branch, username, password);
            if (gitWrapper == null)
                return 1;

            if (workMode.HasFlag(WorkMode.initialize)) {
                int result = ProcessInitializeRepo(gitWrapper, gitRepoPath, localGitDir, branch, syncHistory, from);
                if (result != 0)
                    return result;
            }
            //if (workMode.HasFlag(WorkMode.directchanges)) {
            //    int result = ProcessDirectChanges(gitWrapper, gitRepoPath, localGitDir, branch, syncHistory);
            //    if (result != 0)
            //        return result;
            //}
            if (workMode.HasFlag(WorkMode.history)) {
                int result = ProcessHistory(gitWrapper, gitRepoPath, localGitDir, branch, clo.CommitsCount, syncHistory);
                if (result != 0)
                    return result;
            }
            if (workMode.HasFlag(WorkMode.mergerequests)) {
                int result = ProcessMergeRequests(gitWrapper, gitRepoPath, localGitDir, clo.Branch, clo.Tracker, syncHistory);
                if (result != 0)
                    return result;
            }
            return 0;
        }
        static string GetVcsSyncHistory(string historyPath) {
            string local = Path.GetTempFileName();
            return HistoryGenerator.GetFile(vcsServer, historyPath, local);
        }
        static void EnsureGitDir(string localGitDir) {
            if (Directory.Exists(localGitDir)) {
                KillProcess("tgitcache");
                DirectoryHelper.DeleteDirectory(localGitDir);
            }
        }
        static void KillProcess(string process) {
            Process[] procs = Process.GetProcessesByName(process);

            foreach (Process proc in procs) {
                proc.Kill();
                proc.WaitForExit();
            }
        }
        static int ProcessInitializeRepo(GitWrapper gitWrapper, string gitRepoPath, string localGitDir, TrackBranch branch, SyncHistoryWrapper syncHistory, DateTime from) {
            var commit = gitWrapper.FindCommit(branch.Name, x => IsAutoSyncComment(branch.Name, x.Message));
            if (commit != null) {
                Log.Message($"Branch {branch.Name} initialized already.");
                return 0;
            }
            var history = HistoryGenerator.GenerateHistory(vcsServer, branch, from);
            var commits = HistoryGenerator.GenerateCommits(history).Where(x => x.TimeStamp > from && !IsLabel(x));
            CommitItem startCommit = commits.FirstOrDefault();
            if (object.Equals(startCommit, null)) {
                Log.Error($"Repo has no commits since {from}. Initializing repo failed.");
                return 1;
            }
            ProcessHistoryInternal(gitWrapper, localGitDir, branch, new[] { startCommit }, syncHistory);
            Commit syncCommit = gitWrapper.FindCommit(branch.Name, x => IsAutoSyncComment(branch.Name, x.Message));
            string token = Guid.NewGuid().ToString();
            syncHistory.Add(syncCommit.Sha, startCommit.TimeStamp.Ticks, token);
            syncHistory.Save();
            return 0;
        }
        static GitWrapper CreateGitWrapper(string gitRepoPath, string localGitDir, TrackBranch branch, string username, string password) {
            GitWrapper gitWrapper = new GitWrapper(localGitDir, gitRepoPath, new UsernamePasswordCredentials() { Username = username, Password = password });
            if (gitWrapper.IsEmpty) {
                Log.Error($"Specified branch {branch.Name} in repo {gitRepoPath} is empty. Initialize repo properly.");
                return null;
            }

            gitWrapper.EnsureBranch(branch.Name, null);
            gitWrapper.CheckOut(branch.Name);
            gitWrapper.Fetch(updateTags:true);
            Log.Message($"Branch {branch.Name} initialized.");

            return gitWrapper;
        }
        static TrackBranch FindBranch(string branchName, string trackerPath) {
            string localPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string configPath = Path.Combine(localPath, trackerPath);
            var branch = GetBranch(branchName, configPath);
            if (branch == null) {
                Log.Error($"Specified branch {branchName} not found in track file.");
                return null;
            }
            return branch;
        }
        static int ProcessDirectChanges(GitWrapper gitWrapper, string gitRepoPath, string localGitDir, TrackBranch branch, SyncHistoryWrapper syncHistory) {
            var lastSync = syncHistory.GetHead();
            var lastSyncCommit = gitWrapper.FindCommit(branch.Name, x => x.Sha == lastSync.GitCommitSha);
            var lastCommit = gitWrapper.FindCommit(branch.Name);
            bool hasChangesInGit = lastCommit.Sha != lastSync.GitCommitSha;
            if (!hasChangesInGit) {
                Log.Message($"Branch {branch.Name} checked. There is no direct changes.");
                return 0;
            }
            Log.Message($"Branch {branch.Name} has local changes.");

            string token = Guid.NewGuid().ToString();
            var syncItems = GenerateDirectChangeSet(gitWrapper, localGitDir, branch, lastSyncCommit, lastCommit, token);
            if (ProcessGenericChangeSet(gitWrapper, branch, gitRepoPath, localGitDir, syncItems, token)) {
                CommitItem syncCommit = SetSyncLabel(lastCommit, branch, localGitDir, token);
                if (syncCommit == null)
                    throw new ArgumentException("set sync commit failed.");
                syncHistory.Add(lastCommit.Sha, syncCommit.TimeStamp.Ticks, token);
                syncHistory.Save();
            }
            return 1;
        }
        static CommitItem SetSyncLabel(Commit commit, TrackBranch branch, string localGitDir, string token) {
            DXVcsWrapper vcsWrapper = new DXVcsWrapper(vcsServer);
            var comment = CalcComment(commit, branch, token);
            vcsWrapper.CreateLabel(branch.RepoRoot, string.Format(tagName, branch.Name), VcsCommentsGenerator.Instance.ConvertToString(comment));
            var syncLabelItem = HistoryGenerator.FindCommit(vcsServer, branch, x => VcsCommentsGenerator.Instance.Parse(x.Comment).Token == token);
            var syncLabel = HistoryGenerator.GenerateCommits(new[] {syncLabelItem});
            return syncLabel.First();
        }
        static bool ProcessGenericChangeSet(GitWrapper gitWrapper, TrackBranch branch, string gitRepoPath, string localGitDir, IEnumerable<SyncItem> syncItems, string token) {
            DXVcsWrapper wrapper = new DXVcsWrapper(vcsServer);
            if (!wrapper.ProcessCheckout(syncItems)) {
                Log.Error("Checkout for sync failed");
                return false;
            }
            return wrapper.ProcessCheckIn(syncItems, string.Empty);
        }
        static IEnumerable<SyncItem> GenerateDirectChangeSet(GitWrapper gitWrapper, string localGitDir, TrackBranch branch, Commit lastSync, Commit lastChange, string token) {
            var changes = gitWrapper.GetChanges(lastSync, lastChange).Where(x => branch.TrackItems.FirstOrDefault(track => x.OldPath.StartsWith(track.ProjectPath)) != null);
            var genericChanges = changes.Select(x => ProcessDirectChanges(lastChange, branch, x, CalcVcsRoot(branch, x), localGitDir, token)).ToList();
            return genericChanges;
        }
        static SyncItem ProcessDirectChanges(Commit lastChange, TrackBranch branch, TreeEntryChanges changes, string vcsRoot, string localGitDir, string token) {
            SyncItem item = new SyncItem();
            item.SyncAction = SyncAction.Modify;
            item.LocalPath = Path.Combine(localGitDir, changes.OldPath);
            item.VcsPath = CalcVcsPath(vcsRoot, changes.OldPath);
            item.Comment = CalcComment(lastChange, branch, token);
            return item;
        }
        static bool IsAutoSyncComment(string branchName, string message) {
            if (string.IsNullOrEmpty(message))
                return false;
            var chunks = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return chunks.Any(x => x.StartsWith(string.Format(AutoSyncBranch, branchName)));
        }
        static int ProcessMergeRequests(GitWrapper gitWrapper, string gitRepoPath, string localGitDir, string branchName, string tracker, SyncHistoryWrapper syncHistory) {
            GitLabWrapper gitLabWrapper = new GitLabWrapper(gitServer, branchName, gitlabauthtoken);
            var project = gitLabWrapper.FindProject(gitRepoPath);
            TrackBranch branch = GetBranch(branchName, tracker);
            if (branch == null) {
                Log.Error($"Specified branch {branchName} not found in track file.");
                return 1;
            }
            var mergeRequests = gitLabWrapper.GetMergeRequests(project).ToList();
            if (!mergeRequests.Any()) {
                Log.Message("Zero registered merge requests.");
                return 0;
            }
            foreach (var mergeRequest in mergeRequests) {
                ProcessMergeRequest(gitWrapper, gitLabWrapper, localGitDir, branch, mergeRequest, syncHistory);
            }
            return 0;
        }
        static void ProcessMergeRequest(GitWrapper gitWrapper, GitLabWrapper gitLabWrapper, string localGitDir, TrackBranch branch, MergeRequest mergeRequest, SyncHistoryWrapper syncHistory) {
            switch (mergeRequest.State) {
                case "merged":
                    gitLabWrapper.RemoveMergeRequest(mergeRequest);
                    break;
                case "reopened":
                case "opened":
                    ProcessOpenedMergeRequest(gitWrapper, gitLabWrapper, localGitDir, branch, mergeRequest, syncHistory);
                    break;
            }
        }
        static bool ProcessOpenedMergeRequest(GitWrapper gitWrapper, GitLabWrapper gitLabWrapper, string localGitDir, TrackBranch branch, MergeRequest mergeRequest, SyncHistoryWrapper syncHistory) {
            string autoSyncToken = Guid.NewGuid().ToString();

            var changes = gitLabWrapper.GetMergeRequestChanges(mergeRequest).Where(x => branch.TrackItems.FirstOrDefault(track => x.OldPath.StartsWith(track.ProjectPath)) != null);
            var genericChange = changes.Select(x => ProcessMergeRequestChanges(mergeRequest, x, localGitDir, branch, autoSyncToken)).ToList();

            string sourceBranch = mergeRequest.SourceBranch;
            gitWrapper.EnsureBranch(sourceBranch, null);
            gitWrapper.Reset(sourceBranch);
            gitWrapper.Fetch(updateTags: true);
            Commit lastSource = gitWrapper.FindCommit(sourceBranch);

            string targetBranch = mergeRequest.TargetBranch;
            gitWrapper.EnsureBranch(targetBranch, null);
            gitWrapper.Reset(targetBranch);
            gitWrapper.Fetch(updateTags: true);

            var result = gitWrapper.Merge(sourceBranch, new Signature(defaultUser, "test@mail.com", DateTimeOffset.UtcNow));
            if (result != MergeStatus.Conflicts) {
                Comment comment = CalcComment(mergeRequest, branch, autoSyncToken);
                DXVcsWrapper vcsWrapper = new DXVcsWrapper(vcsServer);
                if (!vcsWrapper.ProcessCheckout(genericChange))
                    throw new ArgumentException("checkout changeset failed.");

                mergeRequest = gitLabWrapper.ProcessMergeRequest(mergeRequest, GitCommentsGenerator.Instance.ConvertToString(comment));
                if (mergeRequest.State == "merged") {
                    Log.Message("Merge request merged successfully.");
                    if (vcsWrapper.ProcessCheckIn(genericChange, VcsCommentsGenerator.Instance.ConvertToString(comment))) {
                        gitWrapper.Pull(defaultUser, branch.Name);

                        var gitCommit = gitWrapper.FindCommit(branch.Name, x => GitCommentsGenerator.Instance.Parse(x.Message).Token == autoSyncToken);
                        var vcsCommit = HistoryGenerator.FindCommit(vcsServer, branch, x => VcsCommentsGenerator.Instance.Parse(x.Comment).Token == autoSyncToken);
                        syncHistory.Add(gitCommit.Sha, vcsCommit.ActionDate.Ticks, autoSyncToken);
                        syncHistory.Save();
                        Log.Message("Merge request checkin successfully.");
                        return true;
                    }
                    Log.Error("Merge request checkin failed.");
                    return false;
                }

                Log.Message("Merge request merging failed");
                gitLabWrapper.UpdateMergeRequest(mergeRequest, AutoMergeFailedComment);
                return true;
            }
            return false;
        }
        static string CalcVcsRoot(TrackBranch branch, TreeEntryChanges fileData) {
            var trackItem = branch.TrackItems.First(x => fileData.OldPath.StartsWith(x.ProjectPath, StringComparison.OrdinalIgnoreCase));
            return trackItem.Path.Remove(trackItem.Path.Length - trackItem.ProjectPath.Length, trackItem.ProjectPath.Length);
        }
        static SyncItem ProcessMergeRequestChanges(MergeRequest mergeRequest, MergeRequestFileData fileData, string localGitDir, TrackBranch branch, string token) {
            string vcsRoot = branch.RepoRoot;
            var syncItem = new SyncItem();
            if (fileData.IsNew) {
                syncItem.SyncAction = SyncAction.New;
                syncItem.LocalPath = CalcLocalPath(localGitDir, branch, fileData.OldPath);
                syncItem.VcsPath = CalcVcsPath(vcsRoot, fileData.OldPath);
            }
            else if (fileData.IsDeleted) {
                syncItem.SyncAction = SyncAction.Delete;
                syncItem.LocalPath = CalcLocalPath(localGitDir, branch, fileData.OldPath);
                syncItem.VcsPath = CalcVcsPath(vcsRoot, fileData.OldPath);
            }
            else if (fileData.IsRenamed) {
                syncItem.SyncAction = SyncAction.Move;
                syncItem.LocalPath = CalcLocalPath(localGitDir, branch, fileData.OldPath);
                syncItem.NewLocalPath = CalcLocalPath(localGitDir, branch, fileData.NewPath);
                syncItem.VcsPath = CalcVcsPath(vcsRoot, fileData.OldPath);
                syncItem.NewVcsPath = CalcVcsPath(vcsRoot, fileData.NewPath);
            }
            else {
                syncItem.SyncAction = SyncAction.Modify;
                syncItem.LocalPath = CalcLocalPath(localGitDir, branch, fileData.OldPath);
                syncItem.VcsPath = CalcVcsPath(vcsRoot, fileData.OldPath);
            }
            syncItem.Comment = CalcComment(mergeRequest, branch, token);
            return syncItem;
        }
        static string CalcLocalPath(string localGitDir, TrackBranch branch, string path) {
            return Path.Combine(localGitDir, path);
        }
        static string CalcVcsPath(string vcsRoot, string path) {
            string result = Path.Combine(vcsRoot, path);
            return result.Replace("\\", "/");
        }
        static int ProcessHistory(GitWrapper gitWrapper, string gitRepoPath, string localGitDir, TrackBranch branch, int commitsCount, SyncHistoryWrapper syncHistory) {
            DateTime lastCommit = CalcLastCommitDate(gitWrapper, branch, syncHistory);
            Log.Message($"Last commit has been performed at {lastCommit.ToLocalTime()}.");

            var history = HistoryGenerator.GenerateHistory(vcsServer, branch, lastCommit).OrderBy(x => x.ActionDate).ToList();
            Log.Message($"History generated. {history.Count} history items obtained.");

            var commits = HistoryGenerator.GenerateCommits(history).Where(x => x.TimeStamp > lastCommit && !IsLabel(x)).ToList();
            if (commits.Count > commitsCount) {
                Log.Message($"Commits generated. First {commitsCount} of {commits.Count} commits taken.");
                commits = commits.Take(commitsCount).ToList();
            }
            else {
                Log.Message($"Commits generated. {commits.Count} commits taken.");
            }
            if (commits.Count > 0)
                ProcessHistoryInternal(gitWrapper, localGitDir, branch, commits, syncHistory);
            Log.Message($"Importing history from vcs completed.");
            return 0;
        }
        static DateTime CalcLastCommitDate(GitWrapper gitWrapper, TrackBranch branch, SyncHistoryWrapper syncHistory) {
            var head = syncHistory.GetHead();
            if (head == null)
                return gitWrapper.CalcLastCommitDate(branch.Name, defaultUser);
            return new DateTime(head.VcsCommitTimeStamp);
        }
        static void ProcessHistoryInternal(GitWrapper gitWrapper, string localGitDir, TrackBranch branch, IList<CommitItem> commits, SyncHistoryWrapper syncHistory) {
            ProjectExtractor extractor = new ProjectExtractor(commits, (item) => {
                var localCommits = HistoryGenerator.GetCommits(item.Items).Where(x => !IsLabel(x)).ToList();
                bool hasModifications = false;
                Commit last = null;
                string token = Guid.NewGuid().ToString();
                foreach (var localCommit in localCommits) {
                    string localProjectPath = Path.Combine(localGitDir, localCommit.Track.ProjectPath);
                    DirectoryHelper.DeleteDirectory(localProjectPath);
                    HistoryGenerator.GetProject(vcsServer, localCommit.Track.Path, localProjectPath, item.TimeStamp);

                    gitWrapper.Fetch();
                    bool isLabel = IsLabel(item);
                    bool hasLocalModifications = gitWrapper.CalcHasModification() || isLabel;
                    if (hasLocalModifications) {
                        gitWrapper.Stage("*");
                        try {
                            var comment = CalcComment(localCommit, token);
                            last = gitWrapper.Commit(GitCommentsGenerator.Instance.ConvertToString(comment), localCommit.Author, defaultUser, localCommit.TimeStamp, isLabel);
                            hasModifications = true;
                        }
                        catch (Exception) {
                            Log.Message($"Empty commit detected for {localCommit.Author} {localCommit.TimeStamp}.");
                        }
                    }
                }
                if (hasModifications) {
                    gitWrapper.Push(branch.Name);
                    string tagName = CreateTagName(branch.Name);
                    Comment comment = CalcComment(last, branch, token);
                    gitWrapper.AddTag(tagName, last, defaultUser, item.TimeStamp, GitCommentsGenerator.Instance.ConvertToString(comment));
                    syncHistory.Add(last.Sha, item.TimeStamp.Ticks, token);
                    syncHistory.Save();
                }
                else
                    Log.Message($"Push empty commits rejected for {item.Author} {item.TimeStamp}.");
            });
            int i = 0;
            while (extractor.PerformExtraction())
                Log.Message($"{++i} from {commits.Count} push to branch {branch.Name} completed.");
        }
        static TrackBranch GetBranch(string branchName, string configPath) {
            try {
                var branches = TrackBranch.Deserialize(configPath);
                return branches.First(x => x.Name == branchName);
            }
            catch (Exception ex) {
                Log.Error("Loading items for track failed", ex);
                return null;
            }
        }
        static bool IsLabel(CommitItem item) {
            return item.Items.Any(x => !string.IsNullOrEmpty(x.Label));
        }
        static string CreateTagName(string branchName) {
            return $"dxvcs2gitservice_sync_{branchName}";
        }
        static Comment CalcComment(MergeRequest mergeRequest, TrackBranch branch, string autoSyncToken) {
            Comment comment = new Comment();
            comment.Author = mergeRequest.Author.Name;
            comment.Branch = branch.Name;
            comment.Token = autoSyncToken;
            return comment;
        }
        static Comment CalcComment(Commit commit, TrackBranch branch, string syncToken) {
            Comment comment = new Comment();
            comment.Author = commit.Author.Name;
            comment.Branch = branch.Name;
            comment.Token = syncToken;
            return comment;
        }
        static Comment CalcComment(CommitItem item, string token) {
            Comment comment = new Comment();
            comment.TimeStamp = item.TimeStamp.Ticks.ToString();
            comment.Author = item.Author;
            comment.Branch = item.Track.Branch;
            comment.Token = token;
            return comment;
        }
    }
}