using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        const string autoSyncFormat = "{0:M/d/yyyy HH:mm:ss.ffffff}";
        const string repoPath = "repo";
        const string gitServer = @"http://litvinov-lnx";
        const string vcsServer = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService";
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
            if (workMode.HasFlag(WorkMode.History)) {
                int result = ProcessHistory(gitRepoPath, localGitDir, clo.Branch, clo.Tracker, clo.CommitsCount, username, password);
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
        static int ProcessMergeRequests(string gitRepoPath, string localGitDir, string branch, string tracker, string username, string password) {
            GitLabWrapper wrapper = new GitLabWrapper(gitServer, token);
            var project = wrapper.FindProject(gitRepoPath);
            var mergeRequests = wrapper.GetMergeRequests(project, branch).ToList();
            if (!mergeRequests.Any()) {
                Log.Message("Zero registered merge requests.");
                return 0;
            }
            foreach (var mergeRequest in mergeRequests) {
                ProcessMergeRequest(wrapper, mergeRequest);
            }
            return 0;
        }
        static void ProcessMergeRequest(GitLabWrapper wrapper, MergeRequest mergeRequest) {
            switch (mergeRequest.State) {
                case "merged":
                    wrapper.RemoveMergeRequest(mergeRequest);
                    break;
                case "reopened":
                case "opened":
                    ProcessOpenedMergeRequest(wrapper, mergeRequest);
                    break;
            }
        }
        static void ProcessOpenedMergeRequest(GitLabWrapper wrapper, MergeRequest mergeRequest) {
            var changes = wrapper.GetMergeRequestChanges(mergeRequest).ToList();
        }
        static int ProcessHistory(string gitRepoPath, string localGitDir, string trackerPath, string branchName, int commitsCount, string username, string password) {
            GitWrapper gitWrapper = new GitWrapper(localGitDir, gitRepoPath, new UsernamePasswordCredentials() { Username = username, Password = password });
            if (gitWrapper.IsEmpty) {
                gitWrapper.Commit("Initial commit", username, username, new DateTime(2013, 12, 1));
                gitWrapper.Push("master");
            }
            string localPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string configPath = Path.Combine(localPath, trackerPath);
            var serializer = new SharpSerializer();
            TrackConfig trackConfig;

            try {
                trackConfig = (TrackConfig)serializer.Deserialize(configPath);
            }
            catch (Exception ex) {
                Log.Error("Loading items for track failed", ex);
                return 1;
            }

            var tracker = new Tracker(trackConfig.TrackItems);
            var branch = tracker.FindBranch(branchName);
            if (branch == null) {
                Log.Error($"Specified branch {branchName} not found in track file.");
                return 1;
            }

            gitWrapper.EnsureBranch(branch.Name, null);
            gitWrapper.CheckOut(branch.Name);
            gitWrapper.Fetch(true);
            Log.Message($"Branch {branch.Name} initialized.");
            DateTime lastCommit = gitWrapper.CalcLastCommitDate(branch.Name, username);
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

            ProjectExtractor extractor = new ProjectExtractor(commits, (item) => {
                var localCommits = HistoryGenerator.GetCommits(item.Items).ToList();
                bool hasModifications = false;
                Commit last = null;
                foreach (var localCommit in localCommits) {
                    string localProjectPath = Path.Combine(localGitDir, localCommit.Track.RelativeLocalPath);
                    DirectoryHelper.DeleteDirectory(localProjectPath);
                    HistoryGenerator.GetProject(vcsServer, localCommit.Track.FullPath, localProjectPath, item.TimeStamp);

                    gitWrapper.Fetch();
                    bool isLabel = IsLabel(item);
                    bool hasLocalModifications = gitWrapper.CalcHasModification() || isLabel;
                    if (hasLocalModifications) {
                        gitWrapper.Stage("*");
                        try {
                            last = gitWrapper.Commit(CalcComment(localCommit), localCommit.Author, username, localCommit.TimeStamp, isLabel);
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
                        gitWrapper.AddTag(tagName, last, username, item.TimeStamp, string.Format(autoSyncFormat, item.TimeStamp));
                    }
                }
                else {
                    Log.Message($"Push empty commits rejected for {item.Author} {item.TimeStamp}.");
                }
            });
            int i = 0;
            while (extractor.PerformExtraction()) {
                Log.Message($"{++i} from {commits.Count} push to branch {branch.Name} completed.");
            }
            return 0;
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
            sb.AppendLine(string.Format(autoSyncFormat, item.TimeStamp));
            return sb.ToString();
        }
        static string FilterLabel(string comment) {
            if (comment.StartsWith("Label: "))
                return "default";
            return comment;
        }
    }
}