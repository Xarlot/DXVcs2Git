using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using DXVcs2Git.Core;
using DXVcs2Git.DXVcs;
using DXVcs2Git.Git;
using LibGit2Sharp;
using NGitLab.Models;
using Polenter.Serialization;
using Commit = LibGit2Sharp.Commit;

namespace DXVcs2Git.Console {
    internal class Program {
        const string token = "X6XV2G_ycz_U4pi4m93K";
        static readonly string AutoSyncBranchRegexFormat = @"\[.*:autosync for {0}\]";
        const string AutoSyncBranchFormat = @"[{0}:autosync for {1}]";
        const string AutoSyncTimeStampFormat = "{0:M/d/yyyy HH:mm:ss.ffffff}";
        const string repoPath = "repo";
        const string gitServer = @"http://litvinov-lnx";
        const string vcsServer = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService";
        const string defaultUser = "dxvcs2gitservice";
        const string tagName = "dxvcs2gitservice_sync{0}";
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
            GitWrapper gitWrapper = CreateGitWrapper(gitRepoPath, localGitDir, branch, username, password);
            if (gitWrapper == null)
                return 1;

            if (workMode.HasFlag(WorkMode.Initialize)) {
                int result = ProcessInitializeRepo(gitWrapper, gitRepoPath, localGitDir, branch, from);
                if (result != 0)
                    return result;
            }
            if (workMode.HasFlag(WorkMode.DirectChanges)) {
                int result = ProcessDirectChanges(gitWrapper, gitRepoPath, localGitDir, branch);
                if (result != 0)
                    return result;
            }
            if (workMode.HasFlag(WorkMode.History)) {
                int result = ProcessHistory(gitWrapper, gitRepoPath, localGitDir, branch, clo.CommitsCount);
                if (result != 0)
                    return result;
            }
            if (workMode.HasFlag(WorkMode.MergeRequests)) {
                int result = ProcessMergeRequests(gitRepoPath, localGitDir, clo.Branch, clo.Tracker, username, password);
                if (result != 0)
                    return result;
            }
            return 0;
        }
        static int ProcessInitializeRepo(GitWrapper gitWrapper, string gitRepoPath, string localGitDir, TrackBranch branch, DateTime from) {
            var commit = gitWrapper.FindCommit(branch.Name, x => IsAutoSyncComment(branch.Name, x.Message));
            if (commit != null) {
                Log.Message($"Branch {branch.Name} initialized already.");
                return 0;
            }
            var history = HistoryGenerator.GenerateHistory(vcsServer, branch, from);
            var commits = HistoryGenerator.GenerateCommits(history).Where(x => x.TimeStamp > from);
            CommitItem startCommit = commits.FirstOrDefault();
            if (object.Equals(startCommit, null)) {
                Log.Error($"Repo has no commits since {from}. Initializing repo failed.");
                return 1;
            }
            ProcessHistoryInternal(gitWrapper, localGitDir, branch, new [] {startCommit});
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
            gitWrapper.Fetch(true);
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
        static int ProcessDirectChanges(GitWrapper gitWrapper, string gitRepoPath, string localGitDir, TrackBranch branch) {
            bool hasChanges = HasDirectChanges(gitWrapper, branch, localGitDir);
            if (!hasChanges) {
                Log.Message($"Branch {branch.Name} checked. There is no direct changes.");
                return 0;
            }
            Log.Message("$Branch {branch.Name} has local changes.");
            return 1;
        }
        static bool HasDirectChanges(GitWrapper gitWrapper, TrackBranch branch, string localGitDir) {
            var commit = gitWrapper.FindCommit(branch.Name);
            var tag = gitWrapper.GetTag(string.Format(tagName, branch.Name));
            if (tag != null && tag.Target.Sha == commit.Sha)
                return false;
            if (tag == null) {
                if (commit.Committer.Name == defaultUser && IsAutoSyncComment(branch.Name, commit.Message)) {
                    Log.Message($"Branch {branch.Name} has no associated sync tag. Sync based on commit message.");
                    return false;
                }
            }
            bool changesDetected = CheckLocalChanges(gitWrapper, branch, localGitDir);
            Commit lastSyncCommit = gitWrapper.FindCommit(branch.Name, x => IsAutoSyncComment(branch.Name, x.Message));
            if (lastSyncCommit == null) {
                Log.Error("Last sync commit not found, repo is not initialized or damaged.");
                throw new ArgumentException("Repo in inconsistent state");
            }
            return changesDetected;
        }
        static bool CheckLocalChanges(GitWrapper gitWrapper, TrackBranch branch, string localGitDir) {
            foreach (var trackItem in branch.TrackItems) {
                string localProjectPath = Path.Combine(localGitDir, trackItem.ProjectPath);
                DirectoryHelper.DeleteDirectory(localProjectPath);
                HistoryGenerator.GetProject(vcsServer, trackItem.Path, localProjectPath, DateTime.Now);
            }
            gitWrapper.Fetch();
            return gitWrapper.CalcHasModification();
        }
        static bool IsAutoSyncComment(string branchName, string message) {
            if (string.IsNullOrEmpty(message))
                return false;
            var chunks = message.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            Regex regex = new Regex(string.Format(AutoSyncBranchRegexFormat, branchName));
            return chunks.Any(x => regex.IsMatch(x));
        }
        static int ProcessMergeRequests(string gitRepoPath, string localGitDir, string branchName, string tracker, string username, string password) {
            DXVcsWrapper vcsWrapper = new DXVcsWrapper(vcsServer, branchName, localGitDir);
            GitLabWrapper gitWrapper = new GitLabWrapper(gitServer, branchName, token);
            var project = gitWrapper.FindProject(gitRepoPath);
            TrackBranch branch = GetBranch(branchName, tracker);
            if (branch == null) {
                Log.Error($"Specified branch {branchName} not found in track file.");
                return 1;
            }
            var mergeRequests = gitWrapper.GetMergeRequests(project).ToList();
            if (!mergeRequests.Any()) {
                Log.Message("Zero registered merge requests.");
                return 0;
            }
            foreach (var mergeRequest in mergeRequests) {
                ProcessMergeRequest(gitWrapper, vcsWrapper, branch, mergeRequest);
            }
            return 0;
        }
        static void ProcessMergeRequest(GitLabWrapper gitWrapper, DXVcsWrapper vcsWrapper, TrackBranch branch, MergeRequest mergeRequest) {
            switch (mergeRequest.State) {
                case "merged":
                    gitWrapper.RemoveMergeRequest(mergeRequest);
                    break;
                case "reopened":
                case "opened":
                    ProcessOpenedMergeRequest(gitWrapper, vcsWrapper, branch, mergeRequest);
                    break;
            }
        }
        static void ProcessOpenedMergeRequest(GitLabWrapper wrapper, DXVcsWrapper vcsWrapper, TrackBranch branch, MergeRequest mergeRequest) {
            var changes = wrapper.GetMergeRequestChanges(mergeRequest).Where(x => branch.TrackItems.FirstOrDefault(track => x.OldPath.StartsWith(track.ProjectPath)) != null);
            var genericChange = changes.Select(x => ProcessMergeRequestChanges(x, CalcVcsRoot(branch, x))).ToList();
            vcsWrapper.ProcessCheckout(genericChange);
        }
        static string CalcVcsRoot(TrackBranch branch, MergeRequestFileData fileData) {
            var trackItem = branch.TrackItems.First(x => fileData.OldPath.StartsWith(x.ProjectPath, StringComparison.OrdinalIgnoreCase));
            return trackItem.Path;
        }
        static SyncItem ProcessMergeRequestChanges(MergeRequestFileData fileData, string vcsRoot) {
            var syncItem = new SyncItem();
            if (fileData.IsNew) {
                syncItem.SyncAction = SyncAction.Modify;
                syncItem.LocalPath = fileData.OldPath;
                syncItem.VcsPath = CalcVcsPath(vcsRoot, fileData.OldPath);
            }
            else if (fileData.IsDeleted) {
                syncItem.SyncAction = SyncAction.Delete;
                syncItem.LocalPath = fileData.OldPath;
                syncItem.VcsPath = CalcVcsPath(vcsRoot, fileData.OldPath);
            }
            else if (fileData.IsRenamed) {
                syncItem.SyncAction = SyncAction.Move;
                syncItem.LocalPath = fileData.OldPath;
                syncItem.NewLocalPath = fileData.NewPath;
                syncItem.VcsPath = CalcVcsPath(vcsRoot, fileData.OldPath);
                syncItem.NewVcsPath = CalcVcsPath(vcsRoot, fileData.NewPath);
            }
            else {
                syncItem.SyncAction = SyncAction.Modify;
                syncItem.LocalPath = fileData.OldPath;
                syncItem.VcsPath = CalcVcsPath(vcsRoot ,fileData.OldPath);
            }
            return syncItem;
        }
        static string CalcVcsPath(string vcsRoot, string path) {
            return Path.Combine(vcsRoot, path);
        }
        static int ProcessHistory(GitWrapper gitWrapper, string gitRepoPath, string localGitDir, TrackBranch branch, int commitsCount) {
            DateTime lastCommit = gitWrapper.CalcLastCommitDate(branch.Name, defaultUser);
            Log.Message($"Last commit has been performed at {lastCommit.ToLocalTime()}.");

            var history = HistoryGenerator.GenerateHistory(vcsServer, branch, lastCommit).OrderBy(x => x.ActionDate).ToList();
            Log.Message($"History generated. {history.Count} history items obtained.");

            var commits = HistoryGenerator.GenerateCommits(history).Where(x => x.TimeStamp > lastCommit).ToList();
            if (commits.Count > commitsCount) {
                Log.Message($"Commits generated. First {commitsCount} of {commits.Count} commits taken.");
                commits = commits.Take(commitsCount).ToList();
            }
            else {
                Log.Message($"Commits generated. {commits.Count} commits taken.");
            }

            ProcessHistoryInternal(gitWrapper, localGitDir, branch, commits);
            return 0;
        }
        static void ProcessHistoryInternal(GitWrapper gitWrapper, string localGitDir, TrackBranch branch, IList<CommitItem> commits) {
            ProjectExtractor extractor = new ProjectExtractor(commits, (item) => {
                var localCommits = HistoryGenerator.GetCommits(item.Items).ToList();
                bool hasModifications = false;
                Commit last = null;
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
                            last = gitWrapper.Commit(CalcComment(localCommit), localCommit.Author, defaultUser, localCommit.TimeStamp, isLabel);
                            hasModifications = true;
                        }
                        catch (Exception) {
                            Log.Message($"Empty commit detected for {localCommit.Author} {localCommit.TimeStamp}.");
                        }
                    }
                }
                if (hasModifications) {
                    gitWrapper.Push(branch.Name);
                    if (last != null) {
                        string tagName = CreateTagName(branch.Name);
                        gitWrapper.AddTag(tagName, last, defaultUser, item.TimeStamp, string.Format(AutoSyncTimeStampFormat, item.TimeStamp));
                    }
                }
                else
                    Log.Message($"Push empty commits rejected for {item.Author} {item.TimeStamp}.");
            });
            int i = 0;
            while (extractor.PerformExtraction())
                Log.Message($"{++i} from {commits.Count} push to branch {branch.Name} completed.");
        }
        static TrackBranch GetBranch(string branchName, string configPath) {
            var serializer = new SharpSerializer();
            TrackConfig trackConfig;

            try {
                trackConfig = (TrackConfig)serializer.Deserialize(configPath);
            }
            catch (Exception ex) {
                Log.Error("Loading items for track failed", ex);
                return null;
            }

            var tracker = new Tracker(trackConfig.TrackItems);
            return tracker.FindBranch(branchName);
        }
        static bool IsLabel(CommitItem item) {
            return item.Items.Any(x => !string.IsNullOrEmpty(x.Label));
        }
        static string CreateTagName(string branchName) {
            return $"dxvcs2gitservice_sync_{branchName}";
        }
        static string CalcComment(CommitItem item) {
            StringBuilder sb = new StringBuilder();
            var labelItem = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Label));
            if (labelItem != null && !string.IsNullOrEmpty(labelItem.Label))
                sb.AppendLine($"Label: {labelItem.Label}");
            var commentItem = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Comment));
            if (commentItem != null && !string.IsNullOrEmpty(commentItem.Comment))
                sb.AppendLine($"{FilterLabel(commentItem.Comment)}");
            sb.AppendLine(string.Format(AutoSyncBranchFormat, item.Author, item.Track.Branch));
            sb.AppendLine(string.Format(AutoSyncTimeStampFormat, item.TimeStamp));
            return sb.ToString();
        }
        static string FilterLabel(string comment) {
            if (comment.StartsWith("Label: "))
                return "default";
            return comment;
        }
    }
}