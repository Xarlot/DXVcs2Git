using System.Threading.Tasks;
using DXVcs2Git.Git;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class GitlabWrapperTests {
        const string token = "X6XV2G_ycz_U4pi4m93K";

        [Test]
        public void GetProject() {
            GitLabWrapper wrapper = new GitLabWrapper("http://litvinov-lnx", token);
            var project = wrapper.FindProject("tester/testxpfall");
            Assert.IsNotNull(project);
        }
        [Test]
        public void GetMergeRequests() {
            GitLabWrapper wrapper = new GitLabWrapper("http://litvinov-lnx", token);
            var project = wrapper.FindProject("tester/testxpfall");
            var requests = wrapper.GetMergeRequests(project);
        }
    }
}
