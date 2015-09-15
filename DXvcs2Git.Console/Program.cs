using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using DXVcs2Git.Core;
using DXVcs2Git.DXVcs;
using LibGit2Sharp;
using Polenter.Serialization;

namespace DXVcs2Git.Console {
    internal class Program {
        const string repoPath = "repo";
        const string vcsPath = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService";
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

            GitWrapper gitWrapper = new GitWrapper(localGitDir, gitRepoPath, new UsernamePasswordCredentials() { Username = username, Password = password });
            if (gitWrapper.IsEmpty) {
                gitWrapper.Commit("Initial commit", username, username, new DateTime(2013, 12, 1));
                gitWrapper.Push("master");
            }
            string localPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string configPath = Path.Combine(localPath, clo.Tracker);
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
            var branch = tracker.FindBranch(clo.Branch);
            if (branch == null) {
                Log.Error($"Specified branch {clo.Branch} not found in track file.");
                return 1;
            }

            gitWrapper.EnsureBranch(branch.Name, null);
            gitWrapper.CheckOut(branch.Name);
            Log.Message($"Branch {branch.Name} initialized.");
            DateTime lastCommit = gitWrapper.CalcLastCommitDate(branch.Name, username);
            Log.Message($"Last commit has been performed at {lastCommit.ToLocalTime()}.");

            var history = HistoryGenerator.GenerateHistory(vcsPath, branch, lastCommit);
            Log.Message($"History generated. {history.Count} history items obtained.");

            var commits = HistoryGenerator.GenerateCommits(history).Where(x => x.TimeStamp >= lastCommit).ToList();
            if (commits.Count > clo.CommitsCount) {
                Log.Message($"Commits generated. First {clo.CommitsCount} of {commits.Count} commits taken.");
                commits = commits.Take(clo.CommitsCount).ToList();
            }
            else {
                Log.Message($"Commits generated. {commits.Count} commits taken.");
            }

            ProjectExtractor extractor = new ProjectExtractor(commits, (item) => {
                string local = Path.Combine(localGitDir, item.Track.RelativeLocalPath);
                DirectoryHelper.DeleteDirectory(local);
                HistoryGenerator.GetProject(vcsPath, item.Track.FullPath, local, item.TimeStamp);
                gitWrapper.Fetch();
                if (gitWrapper.CalcHasModification() || IsLabel(item)) {
                    gitWrapper.Stage("*");
                    gitWrapper.Commit(CalcComment(item), item.Author, username, item.TimeStamp);
                    gitWrapper.Push(branch.Name);
                }
                else {
                    Log.Message($"Empty commit rejected for {item.Author} {item.TimeStamp}.");
                }
            });
            int i = 0;
            while (extractor.PerformExtraction()) {
                Log.Message($"{++i} from {commits.Count} push to branch {branch.Name} completed.");
            }

            //string trunk = "master";
            //Commit whereCreateBranch = null;
            //Log.Message("Start");
            //foreach (var branch in tracker.Branches) {
            //    gitWrapper.EnsureBranch(branch.Name, whereCreateBranch);
            //    gitWrapper.CheckOut(branch.Name);
            //    Log.Message($"Branch {branch.Name} initialized");

            //    var history = HistoryGenerator.GenerateHistory(DefaultConfig.Config.AuxPath, branch);
            //    Log.Message($"History generated. {history.Count} history items obtained");
            //    DateTime lastCommit = gitWrapper.CalcLastCommitDate(branch.Name, username);
            //    Log.Message($"Last commit has been performed at {lastCommit}");
            //    var commits = HistoryGenerator.GenerateCommits(history).Where(x => x.TimeStamp >= lastCommit).ToList();
            //    Log.Message($"Commits generated. {commits.Count} commits obtained");
            //    ProjectExtractor extractor = new ProjectExtractor(commits, (item) => {
            //        string local = Path.Combine(localGitDir, item.Track.RelativeLocalPath);
            //        DirectoryHelper.DeleteDirectory(local);
            //        HistoryGenerator.GetProject(DefaultConfig.Config.AuxPath, item.Track.FullPath, local, item.TimeStamp);
            //        gitWrapper.Fetch();
            //        if (gitWrapper.CalcHasModification() || IsLabel(item)) {
            //            gitWrapper.Stage("*");
            //            gitWrapper.Commit(CalcComment(item), item.Author, username, item.TimeStamp);
            //            gitWrapper.Push(branch.Name);
            //        }
            //        else {
            //            Log.Message($"Empty commit rejected for {item.Author} {item.TimeStamp}.");
            //        }
            //    });
            //    int i = 0;
            //    while (extractor.PerformExtraction()) {
            //        Log.Message($"{++i} from {commits.Count} push to branch {branch.Name} completed.");
            //    }
            //    whereCreateBranch = gitWrapper.FindCommit(branch.Name, CalcBranchComment(tracker.Branches, branch));
            //}
            return 0;
        }
        static bool IsLabel(CommitItem item) {
            return item.Items.Any(x => !string.IsNullOrEmpty(x.Label));
        }
        static string CalcBranchComment(IList<TrackBranch> branches, TrackBranch branch) {
            int index = branches.IndexOf(branch);
            if (index == branches.Count - 1)
                return "Label: branch nothing";
            return $"Label: branch {branches[index + 1].Name.Remove(0, 2)}";
        }

        static string CalcComment(CommitItem item) {
            StringBuilder sb = new StringBuilder();
            var labelItem = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Label));
            if (labelItem != null && !string.IsNullOrEmpty(labelItem.Label))
                sb.AppendLine($"Label: {labelItem.Label}");
            var commentItem = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Comment));
            if (commentItem != null && !string.IsNullOrEmpty(commentItem.Comment))
                sb.AppendLine($"{FilterLabel(commentItem.Comment)}");
            return sb.ToString();
        }
        static string FilterLabel(string comment) {
            if (comment.StartsWith("Label: "))
                return "default";
            return comment;
        }
    }

}