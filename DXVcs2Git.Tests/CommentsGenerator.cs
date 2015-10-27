using System;
using System.Text;
using DXVcs2Git.Core;
using DXVcs2Git.DXVcs;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class CommentsGenerator {
        [Test]
        public void ParseAuthor() {
            string commentString = "[a:litvinov t:34] test";
            var comment = CommentWrapper.Parse(commentString);
            Assert.AreEqual("litvinov", comment.Author);
            commentString = "a:litvinov t:34] test";
            comment = CommentWrapper.Parse(commentString);
            Assert.AreEqual(null, comment.Author);
        }
        [Test]
        public void ParseToken() {
            string commentString = "[a:litvinov t:34] test";
            var comment = CommentWrapper.Parse(commentString);
            Assert.AreEqual("34", comment.Token);
            commentString = "a:litvinov t:34] test";
            comment = CommentWrapper.Parse(commentString);
            Assert.AreEqual(null, comment.Token);
        }
        //[Test]
        //public void ParseComment() {
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine("token: test");
        //    sb.AppendLine("dxvcs2gitservice token: test");
        //    var comment = VcsCommentsGenerator.Instance.Parse(sb.ToString());
        //    Assert.AreEqual("test", comment.Token);
        //    Assert.AreEqual("token: test", comment.Comment);
        //}
    }
}
