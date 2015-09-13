using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            Log.Message("Start");
            GitWrapper gitWrapper = new GitWrapper(path, testUrl, new UsernamePasswordCredentials() { Username = username, Password = "q1w2e3r4t5y6" });

            string localPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string configPath = Path.Combine(localPath, DefaultConfig.Config.TrackConfigPath);
            var serializer = new SharpSerializer();
            var trackConfig = (TrackConfig)serializer.Deserialize(configPath);
            var tracker = new Tracker(trackConfig.TrackItems);
            string trunk = "master";
            Commit whereCreateBranch = null;
            foreach (var branch in tracker.Branches) {
                gitWrapper.Fetch();
                gitWrapper.EnsureBranch(branch.Name, whereCreateBranch);

                var history = HistoryGenerator.GenerateHistory(DefaultConfig.Config.AuxPath, branch);
                DateTime lastCommit = gitWrapper.CalcLastCommitDate(branch.Name, username);
                var commits = HistoryGenerator.GenerateCommits(history).Where(x => x.TimeStamp >= lastCommit).ToList();
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
                while (extractor.PerformExtraction()) {
                }
                whereCreateBranch = gitWrapper.FindCommit(trunk, commits.First().TimeStamp);
            }




            //System.Console.WriteLine($"========   Start generating branch timings   ===========");
            //List<DateTime> branchesCreatedTime = branches.Select(x => {
            //    var history = repo.GetProjectHistory(x, true);
            //    return history.First(IsBranchCreatedTimeStamp).ActionDate;
            //}).Concat(new[] {DateTime.Now}).ToList();
            //System.Console.WriteLine($"========   Completed generating branch timings   ===========");
            //System.Console.WriteLine($"========   Start generating project history   ===========");
            //var resultHistory = Enumerable.Empty<HistoryItem>();
            //DateTime previousStamp = branchesCreatedTime[0];
            //for (int i = 0; i < branches.Count; i++) {
            //    DateTime currentStamp = branchesCreatedTime[i + 1];
            //    string branch = branches[i];
            //    System.Console.WriteLine($"========   Start generating project history for brahch {branch}  ===========");
            //    var history = repo.GetProjectHistory(branch, true, previousStamp, currentStamp);
            //    System.Console.WriteLine($"========   Completed generating project history for brahch {branch}  ===========");

            //    var projectHistory = CalcProjectHistory(history).Where(x => x.ActionDate >= previousStamp && x.ActionDate <= currentStamp).ToList();
            //    foreach (var historyItem in projectHistory) {
            //        historyItem.Branch = branch;
            //    }
            //    resultHistory = resultHistory.Concat(projectHistory);
            //    previousStamp = currentStamp;
            //}
            //resultHistory = resultHistory.ToList();
            //System.Console.WriteLine($"========   Completed generating project history   ===========");
            //InitUserCredentials();
            //GitWrapper gitRepo = new GitWrapper(path, testUrl, credentials);
            //System.Console.WriteLine($"========   Start updating git repo    ===========");
            //gitRepo.Fetch();
            //System.Console.WriteLine($"========   Startup git fetch completed   ===========");
            //foreach (var item in resultHistory) {
            //    System.Console.WriteLine($"========   Start updating project {item.Branch} {item.TimeStamp} ===========");
            //    CleanUpDir(path);
            //    repo.GetProject(item.Branch, path, item.ActionDate);
            //    System.Console.WriteLine($"========   Completed updating project   ===========");
            //    if (IsDirEmpty(path)) {
            //        System.Console.WriteLine($"========   No history for {item.Branch}  {item.TimeStamp}  ===========");
            //        continue;
            //    }
            //    PreprocessRepo(path);

            //    gitRepo.Fetch();
            //    gitRepo.Stage("*");

            //    string user = item.Items.First().User;
            //    gitRepo.Commit(CalcComment(item), user, item.TimeStamp);

            //    gitRepo.Push("master");
            //}
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
        static bool IsDirEmpty(string path) {
            return !Directory.EnumerateDirectories(path).Any(x => {
                string dirName = Path.GetFileName(x);
                return dirName != ".git";
            });
        }
        static string CalcComment(CommitItem item) {
            var messageItem = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Message));
            if (!string.IsNullOrEmpty(messageItem.Message))
                return messageItem.Message;
            var commentItem = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Comment));
            if (!string.IsNullOrEmpty(commentItem.Comment))
                return commentItem.Comment;
            return string.Empty;
        }
        static bool IsBranchCreatedTimeStamp(ProjectHistoryInfo x) {
            return x.Message != null && x.Message.ToLowerInvariant() == "create";
        }
        //static IEnumerable<HistoryItem> CalcProjectHistory(IEnumerable<ProjectHistoryInfo> history) {
        //    return history.Reverse().GroupBy(x => x.ActionDate).OrderBy(x => x.First().ActionDate).Select(x => new HistoryItem()x.First().ActionDate, x.ToList()));
        //}
    }

}