using System.IO;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class PushToTestProject {
        string testProject = @"git@litvinov-lnx:tester/testproject.git";
        [Test]
        public void GitInit() {
            string path = Config.DefaultFolder;
            ClearDirectory(path);
            string test = GitWrapper.GitInit(path);
            Assert.IsNotNull(test);
        }
        [Test]
        public void GitClone() {
            string path = Config.DefaultFolder;
            ClearDirectory(path);
            //GitWrapper.GitInit(path);
            GitWrapper.GitClone(testProject, path);
        }
        void ClearDirectory(string path) {
            if (Directory.Exists(path))
                Directory.Delete(path);
        }
    }
}
