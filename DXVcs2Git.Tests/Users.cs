using System.Linq;
using DXVcs2Git.Core;
using DXVcs2Git.Core.AD;
using DXVcs2Git.DXVcs;
using DXVcs2Git.Git;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class Users {
        [Test]
        public void ParseLogin() {
            Assert.AreEqual("test", VcsUserParser.ParseUser(@"corp\test"));
            Assert.AreEqual("test", VcsUserParser.ParseUser(@"Corp\test"));
            Assert.AreEqual(null, VcsUserParser.ParseUser(@"Corp\ test"));
            Assert.AreEqual("barakhov", VcsUserParser.ParseUser(@"Corp\barakhov"));
        }
        [Test]
        public void SearchADUsers() {
            var dict = ADWrapper.GetUsers().ToDictionary(x => x.UserName);
            var user = dict["litvinov"];
            Assert.IsNotNull(user.Email);
        }
        [Test]
        public void SearchGitLabUser() {
            GitLabWrapper wrapper = new GitLabWrapper(TestCredentials.GitServer, TestCredentials.GitLabToken);
            var dict = wrapper.GetUsers().ToDictionary(x => x.Username);
            var user = dict["Litvinov"];
            Assert.IsNotNull(user.Email);
        }
        [Test]
        public void SearchRegisteredUser() {
            GitLabWrapper gitLabWrapper = new GitLabWrapper(TestCredentials.GitServer, TestCredentials.GitLabToken);
            DXVcsWrapper vcsWrapper = new DXVcsWrapper(TestCredentials.VcsServer);
            RegisteredUsers users = new RegisteredUsers(gitLabWrapper, vcsWrapper);

            var user = users.GetUser("litvinov");
            Assert.IsNotNull(user);
            Assert.IsTrue(user.IsRegistered);

            var user2 = users.GetUser("Litvinov");
            Assert.IsNotNull(user2);
            Assert.IsTrue(user2.IsRegistered);
        }
        [Test, Explicit]
        public void RegisterUser() {
            GitLabWrapper gitLabWrapper = new GitLabWrapper(TestCredentials.GitServer, TestCredentials.GitLabToken);
            DXVcsWrapper vcsWrapper = new DXVcsWrapper(TestCredentials.VcsServer);
            RegisteredUsers users = new RegisteredUsers(gitLabWrapper, vcsWrapper);
            var user = users.GetUser("Barakhov");
        }
    }
}
