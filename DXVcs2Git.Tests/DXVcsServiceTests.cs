using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using DXVcs2Git.Core;
using DXVcs2Git.DXVcs;
using DXVCS;
using NUnit.Framework;


namespace DXVcs2Git.Tests {
    [TestFixture]
    public class DXVcsServiceTests {
        [Test]
        public void SimpleStart() {
            var repo = DXVcsConectionHelper.Connect(DefaultConfig.Config.AuxPath);
            Assert.IsNotNull(repo);
        }
        [Test]
        public void GetProjectHistoryFromTestHistory() {
            var repo = DXVcsConectionHelper.Connect(DefaultConfig.Config.AuxPath);
            var history = repo.GetProjectHistory(@"$/Sandbox/litvinov/DXVcsTest/testhistory", true, new DateTime(2015, 9, 9), new DateTime(2015, 9, 10));
            Assert.AreEqual(3, history.Count());
            Assert.AreEqual(@"9/9/2015 7:30:57 PM,,,Create,,Project,Litvinov,1", FormatProjectHistoryItem(history[0]));
            Assert.AreEqual(@"9/9/2015 7:31:09 PM,,,Shared from $/Sandbox/litvinov/DXVcsTest/test.txt,test.txt,File,Litvinov,2", FormatProjectHistoryItem(history[1]));
            Assert.AreEqual(@"9/9/2015 7:05:37 PM,,,Checked in (2),test.txt,File,Litvinov,2", FormatProjectHistoryItem(history[2]));
        }
        string FormatProjectHistoryItem(ProjectHistoryInfo historyItem) {
            return $"{historyItem.ActionDate},{historyItem.Comment},{historyItem.Label},{historyItem.Message},{historyItem.Name},{historyItem.Type},{historyItem.User},{historyItem.Version}";
        }
        [Test]
        public void GroupHistoryByTimeStampInFolderIfAddingOneFile() {
            var repo = DXVcsConectionHelper.Connect(DefaultConfig.Config.AuxPath);
            var history = repo.GetProjectHistory(@"$/Sandbox/litvinov/DXVcsTest/testhistory", true, new DateTime(2015, 9, 9), new DateTime(2015, 9, 10));
            var grouped = history.GroupBy(x => x.ActionDate).ToList();
            Assert.AreEqual(3, grouped.Count);
        }
        [Test]
        public void GroupHistoryByTimeStampInFolderIfAddingTwoFiles() {
            var repo = DXVcsConectionHelper.Connect(DefaultConfig.Config.AuxPath);
            var history = repo.GetProjectHistory(@"$/Sandbox/litvinov/DXVcsTest/testhistorybyaddingtwofiles", true, new DateTime(2015, 9, 9), new DateTime(2015, 9, 10));
            var grouped = history.GroupBy(x => x.ActionDate).ToList();
            Assert.AreEqual(4, grouped.Count);
            Assert.AreEqual(@"9/9/2015 7:45:26 PM,,,Create,,Project,Litvinov,1", FormatProjectHistoryItem(grouped[0].ToList()[0]));
            Assert.AreEqual(@"9/9/2015 7:46:32 PM,1,,Created,1.txt,File,Litvinov,2", FormatProjectHistoryItem(grouped[1].ToList()[0]));
            Assert.AreEqual(@"9/9/2015 7:46:32 PM,1,,Created,2.txt,File,Litvinov,3", FormatProjectHistoryItem(grouped[2].ToList()[0]));
            Assert.AreEqual(@"9/9/2015 7:47:22 PM,1,,Checked in (2),1.txt,File,Litvinov,2", FormatProjectHistoryItem(grouped[3].ToList()[0]));
            Assert.AreEqual(@"9/9/2015 7:47:22 PM,1,,Checked in (2),2.txt,File,Litvinov,2", FormatProjectHistoryItem(grouped[3].ToList()[1]));
        }
        [Test]
        public void GetProjectForTimeStamp() {
            var repo = DXVcsConectionHelper.Connect(DefaultConfig.Config.AuxPath);
        }
        [Test, Explicit]
        public void GetProjectHistoryForXpfCore152() {
            //string path = @"c:\test\";
            //var repo = DXVcsConectionHelper.Connect(DefaultConfig.Config.AuxPath);
            //List<string> branches = new List<string>() {
            //    @"$/NET.OLD/2010.1/XPF/DevExpress.Xpf.Core",
            //    @"$/NET.OLD/2010.2/XPF/DevExpress.Xpf.Core",
            //    @"$/NET.OLD/2011.1/XPF/DevExpress.Xpf.Core",
            //};
            //List<DateTime> branchesCreatedTime = branches.Select(x => {
            //    var history = repo.GetProjectHistory(x, true);
            //    return history.First(IsBranchCreatedTimeStamp).ActionDate;
            //}).Concat(new[] { DateTime.Now }).ToList();
            //DateTime previous = branchesCreatedTime[0];
            //var resultHistory = Enumerable.Empty<HistoryItem>();
            //for (int i = 0; i < branches.Count; i++) {
            //    DateTime currentStamp = branchesCreatedTime[i + 1];
            //    string branch = branches[i];
            //    var history = repo.GetProjectHistory(branch, true, previous, currentStamp);
            //    var projectHistory = CalcProjectHistory(history).Where(x => x.ActionDate >= previous && x.ActionDate < currentStamp).OrderBy(x => x.ActionDate).ToList();
            //    foreach (var historyItem in projectHistory) {
            //        historyItem.Path = branch;
            //    }
            //    resultHistory = resultHistory.Concat(projectHistory);
            //    previous = currentStamp;
            //}
            //var result = resultHistory.ToList();
            //foreach (var item in result) {
            //    repo.GetProject(item.Path, path, item.ActionDate);
            //    if (IsDirEmpty(path))
            //        continue;

            //}
        }
        bool IsDirEmpty(string path) {
            return !Directory.EnumerateDirectories(path).Any(x => {
                string dirName = Path.GetFileName(x);
                return dirName != ".git";
            });
        }
        [Test]
        public void TestFindCreateBranchTimeStamp() {
            var repo = DXVcsConectionHelper.Connect(DefaultConfig.Config.AuxPath);
            var vcsPath = @"$/2014.1/XPF/DevExpress.Xpf.Core/DevExpress.Xpf.Core";
            var history = repo.GetProjectHistory(vcsPath, true);
            var create = history.Where(IsBranchCreatedTimeStamp).ToList();
            Assert.AreEqual(1, create.Count);
            Assert.AreEqual(635187859620700000, create[0].ActionDate.Ticks);
            //var projectHistory = history.Reverse().GroupBy(x => x.ActionDate).OrderBy(x => x.First().ActionDate).Select(x => new HistoryItem(x.First().ActionDate, x.ToList()));
            //var project = projectHistory.Where(x => x.History.Any(h => h.Message != null && h.Message.ToLowerInvariant().Contains("branch"))).ToList();
        }
        IEnumerable<HistoryItem> CalcProjectHistory(IEnumerable<ProjectHistoryInfo> history) {
            return history.Reverse().GroupBy(x => x.ActionDate).Select(x => new HistoryItem() { ActionDate = x.First().ActionDate });
        }

        static bool IsBranchCreatedTimeStamp(ProjectHistoryInfo x) {
            return x.Message != null && x.Message.ToLowerInvariant() == "create";
        }
        //[Test]
        //public void HistoryGeneratorTest() {
        //    string configString = "$/2015.1/DB.Standard";
        //    TrackConfig config = new TrackConfig(configString);
        //    var tracker = new Tracker(config.TrackItems);
        //    var history = HistoryGenerator.GenerateHistory(DefaultConfig.Config.AuxPath, tracker.Branches[0]);
        //    var commits = HistoryGenerator.GenerateCommits(history);
        //    Assert.Greater(commits.Count, 0);
        //}
        //string testFolder = @"z:\test\";
        //[Test]
        //public void ProjectExtractorTest() {
        //    string configString = "$/2015.1/DB.Standard";
        //    TrackConfig config = new TrackConfig(configString);
        //    var tracker = new Tracker(config.TrackItems);
        //    var history = HistoryGenerator.GenerateHistory(DefaultConfig.Config.AuxPath, tracker.Branches[0]);
        //    var commits = HistoryGenerator.GenerateCommits(history);

        //    ProjectExtractor extractor = new ProjectExtractor(commits, Extract);
        //    extractor.PerformExtraction();
        //}
        //void Extract(CommitItem item) {
        //    string vcsPath = item.Track.FullPath;
        //    string localPath = vcsPath.Replace($"$/{item.Track.Branch}", testFolder);

        //    var repo = DXVcsConectionHelper.Connect(DefaultConfig.Config.AuxPath);
        //    repo.GetProject(vcsPath, localPath, item.TimeStamp);
        //}
    }
}
