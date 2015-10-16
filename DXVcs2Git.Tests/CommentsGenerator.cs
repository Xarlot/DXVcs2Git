using DXVcs2Git.DXVcs;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class CommentsGenerator {
        [Test]
        public void ParseSha() {
            string commentString = "dxvcs2gitservice sha: 123";
            var comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("123", comment.Sha);

            commentString = "123";
            comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual(null, comment.Sha);

            commentString = "dxvcs2gitservice sha:    1234        ";
            comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("1234", comment.Sha);


            commentString = "dxvcs2gitservice sha:    1234     5   ";
            comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("1234", comment.Sha);
        }
        [Test]
        public void ParseBranch() {
            string commentString = "dxvcs2gitservice branch: 123";
            var comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("123", comment.Branch);

            commentString = "dxvcs2gitservice branch: test";
            comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("test", comment.Branch);

            commentString = "dxvcs2gitservice branch: test 1";
            comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("test", comment.Branch);
        }
        [Test]
        public void ParseTimeStamp() {
            string commentString = "dxvcs2gitservice timestamp: 444444";
            var comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("444444", comment.TimeStamp);

            commentString = "dxvcs2gitservice timestamp: test";
            comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual(string.Empty, comment.TimeStamp);

            commentString = "dxvcs2gitservice timestamp: 1 test";
            comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("1", comment.TimeStamp);
        }
        [Test]
        public void ParseAuthor() {
            string commentString = "dxvcs2gitservice author: test";
            var comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("test", comment.Author);

            commentString = "dxvcs2gitservice author: 123";
            comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("123", comment.Author);

            commentString = "dxvcs2gitservice author: 1 test";
            comment = VcsCommentsGenerator.Instance.Parse(commentString);
            Assert.AreEqual("1", comment.Author);
        }
    }
}
