using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using DXVcs2Git.DXVcs;
using DXVCS;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class DXVcsServiceTests {
        DXVcsConfig defaultConfig = new DXVcsConfig() { AuxPath = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService" };

        [Test]
        public void SimpleStart() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
            Assert.IsNotNull(repo);
        }
        [Test]
        public void GetProjectHistoryFromTestHistory() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
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
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
            var history = repo.GetProjectHistory(@"$/Sandbox/litvinov/DXVcsTest/testhistory", true, new DateTime(2015, 9, 9), new DateTime(2015, 9, 10));
            var grouped = history.GroupBy(x => x.ActionDate).ToList();
            Assert.AreEqual(3, grouped.Count);
        }
        [Test]
        public void GroupHistoryByTimeStampInFolderIfAddingTwoFiles() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
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
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
        }
        [Test, Explicit]
        public void GetProjectHistoryForXpfCore152() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
            var vcsPath81 = @"$/NET.OLD/2008.1/WPF/DevExpress.Wpf.Core";
            //var vcsPath = @"$/2015.2/XPF/DevExpress.Xpf.Core/DevExpress.Xpf.Core";
            var history = repo.GetProjectHistory(vcsPath81, true);
            var grouped = history.Reverse().GroupBy(x => x.ActionDate).OrderBy(x => x.First().ActionDate).Select(x => new HistoryItem(x.First().ActionDate, x.ToList()));
        }

        [Test]
        void UpdateProjectForGroupedHistory() {
            
        }
    }
    class HistoryItem {
        public DateTime TimeStamp { get; private set; }
        public IList<ProjectHistoryInfo> History { get; private set; }

        public HistoryItem(DateTime timeStamp, IList<ProjectHistoryInfo> info) {
            TimeStamp = timeStamp;
            History = info;
        }
    }
}
