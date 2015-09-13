using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
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
                gitWrapper.Commit("Initial commit", username, username, new DateTime(2014, 3, 5));
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
                var commits = HistoryGenerator.GenerateCommits(history).Where(x => x.TimeStamp >= lastCommit).Take(30).ToList();
                Log.Message($"Commits generated. {commits.Count} commits obtained");
                ProjectExtractor extractor = new ProjectExtractor(commits, (item) => {
                    string local = Path.Combine(path, item.Track.RelativeLocalPath);
                    DirectoryHelper.DeleteDirectory(local);
                    HistoryGenerator.GetProject(DefaultConfig.Config.AuxPath, item.Track.FullPath, local, item.TimeStamp);
                    gitWrapper.Fetch();
                    if (gitWrapper.CalcHasModification()) {
                        gitWrapper.Stage("*");
                        gitWrapper.Commit(CalcComment(item), item.Author, username, item.TimeStamp);
                        gitWrapper.Push(branch.Name);
                    }
                });
                int i = 0;
                while (extractor.PerformExtraction()) {
                    Log.Message($"{++i} from {commits.Count} push to branch {branch.Name} completed.");
                }
                whereCreateBranch = gitWrapper.FindCommit(trunk, "branch");
            }
        }
        static void CleanUpDir(string path) {
            string gitPath = Path.Combine(path, ".git");
            foreach (var dir in Directory.EnumerateDirectories(path)) {
                if (dir == gitPath)
                    continue;
                Directory.Delete(dir, true);
                foreach (var file in Directory.EnumerateFiles(path)) {
                    File.Delete(file);
                }
            }
        }
        static void PreprocessRepo(string path) {
            string gitPath = Path.Combine(path, ".git");
            foreach (var dir in Directory.EnumerateDirectories(path)) {
                if (dir == gitPath)
                    continue;
                PreprocessRepo(dir);
                if (!Directory.EnumerateFiles(dir).Any())
                    AddEmptyGitIgnore(dir);
            }
        }
        static void AddEmptyGitIgnore(string path) {
            using (var file = File.Create(Path.Combine(path, ".gitignore"))) {
            }
        }
        static string CalcComment(CommitItem item) {
            var labelItem = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Label));
            if (labelItemItem != null && !string.IsNullOrEmpty(labelItem.Label))
                return labelItem.Label;
            var commentItem = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Comment));
            if (commentItem != null && !string.IsNullOrEmpty(commentItem.Comment))
                return commentItem.Comment;
            var messageItem = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Message));
            if (messageItem != null && !string.IsNullOrEmpty(messageItem.Message))
                return messageItem.Message;
            return "default";
        }
    }

}