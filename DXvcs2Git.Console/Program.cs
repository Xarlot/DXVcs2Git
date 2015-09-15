using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using DXVcs2Git;
using DXVcs2Git.Core;
using DXVcs2Git.DXVcs;
using DXVcs2Git.Tests;
using DXVcs2Git.Tests.TestHelpers;
using DXVCS;
using LibGit2Sharp;
using Polenter.Serialization;

namespace DXVcs2Git.Console {
    internal class Program {
        static string path = @"z:\test\";
        static string testUrl = "http://litvinov-lnx/XPF/Common.git";
        static string username = "dxvcs2gitservice";
        static void Main(string[] args) {
            GitWrapper gitWrapper = new GitWrapper(path, testUrl, new UsernamePasswordCredentials() { Username = username, Password = "q1w2e3r4t5y6" });
            if (gitWrapper.IsEmpty) {
                gitWrapper.Commit("Initial commit", username, username, new DateTime(2013, 12, 1));
                gitWrapper.Push("master");
            }
            string localPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string configPath = Path.Combine(localPath, DefaultConfig.Config.TrackConfigPath);
            var serializer = new SharpSerializer();
            var trackConfig = (TrackConfig)serializer.Deserialize(configPath);
            var tracker = new Tracker(trackConfig.TrackItems);
            string trunk = "master";
            Commit whereCreateBranch = null;
            Log.Message("Start");
            foreach (var branch in tracker.Branches) {
                gitWrapper.EnsureBranch(branch.Name, whereCreateBranch);
                gitWrapper.CheckOut(branch.Name);
                Log.Message($"Branch {branch.Name} initialized");

                var history = HistoryGenerator.GenerateHistory(DefaultConfig.Config.AuxPath, branch);
                Log.Message($"History generated. {history.Count} history items obtained");
                DateTime lastCommit = gitWrapper.CalcLastCommitDate(branch.Name, username);
                Log.Message($"Last commit has been performed at {lastCommit}");
                var commits = HistoryGenerator.GenerateCommits(history).Where(x => x.TimeStamp >= lastCommit).ToList();
                Log.Message($"Commits generated. {commits.Count} commits obtained");
                ProjectExtractor extractor = new ProjectExtractor(commits, (item) => {
                    string local = Path.Combine(path, item.Track.RelativeLocalPath);
                    DirectoryHelper.DeleteDirectory(local);
                    HistoryGenerator.GetProject(DefaultConfig.Config.AuxPath, item.Track.FullPath, local, item.TimeStamp);
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
                whereCreateBranch = gitWrapper.FindCommit(branch.Name, CalcBranchComment(tracker.Branches, branch));
            }
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