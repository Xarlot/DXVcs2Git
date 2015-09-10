using System;
using System.Collections.Generic;
using System.Linq;
using DXVcs2Git.DXVcs;
using DXVcs2Git.Tests;
using DXVcs2Git.Tests.TestHelpers;
using DXVCS;
using LibGit2Sharp;

namespace DXvcs2Git.Console {
    class Program {
        static Credentials credentials;
        static string path = @"c:\test\";
        static string testUrl = "http://litvinov-lnx/tester/testxpfall.git";
        static void Main(string[] args) {

            var repo = DXVcsConectionHelper.Connect(DefaultConfig.Config.AuxPath);
            List<string> branches = new List<string>() {
                @"$/NET.OLD/2009.1/WPF/DevExpress.Wpf.Core",
                @"$/NET.OLD/2009.2/WPF/DevExpress.Wpf.Core",
                //@"$/NET.OLD/2009.3/WPF/DevExpress.Wpf.Core",
                //@"$/NET.OLD/2010.1/XPF/DevExpress.Xpf.Core",
                //@"$/NET.OLD/2010.2/XPF/DevExpress.Xpf.Core",
                //@"$/NET.OLD/2011.1/XPF/DevExpress.Xpf.Core",
                //@"$/NET.OLD/2011.2/XPF/DevExpress.Xpf.Core",
                //@"$/NET.OLD/2012.1/XPF/DevExpress.Xpf.Core",
                //@"$/NET.OLD/2012.2/XPF/DevExpress.Xpf.Core",
                //@"$/NET.OLD/2013.1/XPF/DevExpress.Xpf.Core",
                //@"$/NET.OLD/2013.2/XPF/DevExpress.Xpf.Core",
                //@"$/2014.1/XPF/DevExpress.Xpf.Core",
                //@"$/2014.2/XPF/DevExpress.Xpf.Core",
                //@"$/2015.1/XPF/DevExpress.Xpf.Core",
                //@"$/2015.2/XPF/DevExpress.Xpf.Core",
            };

            System.Console.WriteLine($"===================   Start generating branch timings   ======================");
            List<DateTime> branchesCreatedTime = branches.Select(x => {
                var history = repo.GetProjectHistory(x, true);
                return history.First(IsBranchCreatedTimeStamp).ActionDate;
            }).ToList();
            System.Console.WriteLine($"===================   Completed generating branch timings   ======================");
            System.Console.WriteLine($"===================   Start generating project history   ======================");
            var resultHistory = Enumerable.Empty<HistoryItem>();
            DateTime previousStamp = DateTime.MinValue;
            for (int i = 0; i < branchesCreatedTime.Count; i++) {
                DateTime currentStamp = branchesCreatedTime[i];
                string branch = branches[i];
                System.Console.WriteLine($"===================   Start generating project history for brahch {branch}  ======================");
                var history = repo.GetProjectHistory(branch, true, previousStamp, currentStamp);
                System.Console.WriteLine($"===================   Completed generating project history for brahch {branch}  ======================");

                var projectHistory = CalcProjectHistory(history);
                foreach (var historyItem in projectHistory) {
                    historyItem.Branch = branch;
                }
                resultHistory = resultHistory.Concat(projectHistory);
                previousStamp = currentStamp;
            }
            System.Console.WriteLine($"===================   Completed generating project history   ======================");

            InitUserCredentials();
            string gitPath = InitGit(path);
            using (var gitRepo = new Repository(gitPath)) {
                System.Console.WriteLine($"===================   Start updating git repo    ======================");
                var network = gitRepo.Network.Remotes.First();
                FetchOptions fetchOptions = new FetchOptions();
                fetchOptions.CredentialsProvider += (url, fromUrl, types) => credentials;
                gitRepo.Fetch(network.Name, fetchOptions);
                System.Console.WriteLine($"===================   Startup git fetch completed   ======================");
                foreach (var item in resultHistory) {
                    System.Console.WriteLine($"===================   Startup git fetch completed   ======================");
                    System.Console.WriteLine($"===================   Start updating project {item.Branch} {item.TimeStamp} ======================");
                    repo.GetProject(item.Branch, path, item.TimeStamp);
                    System.Console.WriteLine($"===================   Completed updating project   ======================");
                    gitRepo.Fetch(network.Name, fetchOptions);

                    string user = item.History.First().User;
                    System.Console.WriteLine($"===================   Start git commit {user} {item.TimeStamp}  ======================");
                    Commit commit = gitRepo.Commit(CalcComment(item), new Signature(user, "test@mail.com", new DateTimeOffset(item.TimeStamp)));
                    System.Console.WriteLine($"===================   Completed git commit   ======================");

                    System.Console.WriteLine($"===================   Start git push for branch master  ======================");
                    PushOptions pushOptions = new PushOptions();
                    pushOptions.CredentialsProvider += (url, fromUrl, types) => credentials;
                    gitRepo.Network.Push(gitRepo.Branches["master"], pushOptions);
                    System.Console.WriteLine($"===================   Completed git push  ======================");
                }
            }
        }
        static string CalcComment(HistoryItem item) {
            var messageItem = item.History.FirstOrDefault(x => !string.IsNullOrEmpty(x.Message));
            if (!string.IsNullOrEmpty(messageItem.Message))
                return messageItem.Message;
            var commentItem = item.History.FirstOrDefault(x => !string.IsNullOrEmpty(x.Comment));
            if (!string.IsNullOrEmpty(commentItem.Comment))
                return commentItem.Comment;
            return string.Empty;
        }
        static void InitUserCredentials() {
            var user = new UsernamePasswordCredentials();
            user.Username = Constants.Identity.Name;
            user.Password = "q1w2e3r4t5y6";
            credentials = user;
            System.Console.WriteLine($"===================   User Initialized   ======================");
        }
        static string InitGit(string path) {
            CloneOptions options = new CloneOptions();
            options.CredentialsProvider += (url, fromUrl, types) => credentials;
            string clonedRepoPath = Repository.Clone(testUrl, path, options);
            System.Console.WriteLine($"===================   Git repo Initialized   ======================");
            return clonedRepoPath;
        }
        static bool IsBranchCreatedTimeStamp(ProjectHistoryInfo x) {
            return x.Message != null && x.Message.ToLowerInvariant() == "create";
        }
        static IEnumerable<HistoryItem> CalcProjectHistory(IEnumerable<ProjectHistoryInfo> history) {
            return history.Reverse().GroupBy(x => x.ActionDate).OrderBy(x => x.First().ActionDate).Select(x => new HistoryItem(x.First().ActionDate, x.ToList()));
        }
    }
}
