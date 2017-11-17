using System;
using System.IO;
using System.IO.Packaging;
using DXVcs2Git.Git;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    public static class TestCredentials {
        public const string GitLabToken = "X6XV2G_ycz_U4pi4m93K";
        public const string GitServer = "http://litvinov-lnx";
        public const string VcsServer = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService";
    }
    [TestFixture]
    public class GitlabWrapperTests {
        [Test]
        public void GetProject() {
            GitLabWrapper wrapper = new GitLabWrapper(TestCredentials.GitServer, TestCredentials.GitLabToken);
            var project = wrapper.FindProject("tester/testxpfall");
            Assert.IsNotNull(project);
        }
        [Test]
        public void GetMergeRequests() {
            GitLabWrapper wrapper = new GitLabWrapper(TestCredentials.GitServer, TestCredentials.GitLabToken);
            var project = wrapper.FindProject("tester/testxpfall");
            var requests = wrapper.GetMergeRequests(project, x => x.TargetBranch == "test2");
        }
        [Test]
        public void UnescapeDataString() {
            Assert.AreEqual("/Test 20/Test.bat", Uri.UnescapeDataString("/Test 20/Test.bat"));
            Assert.AreEqual("/Test 20/Test.bat", Uri.UnescapeDataString("/Test%2020/Test.bat"));
            Assert.AreEqual("/Test 20/ Test.bat", Uri.UnescapeDataString("/Test%2020/%20Test.bat"));
        }
    }
}
