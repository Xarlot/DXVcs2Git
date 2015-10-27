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
        [Test]
        public void ParseComment() {
            string commentString = @"[a:litvinov t:34] test \r\n test2";
            var comment = CommentWrapper.Parse(commentString);
            Assert.AreEqual(@"test \r\n test2", comment.Comment);
            Assert.AreEqual("litvinov", comment.Author);
            Assert.AreEqual("34", comment.Token);
            commentString = "[a:litvinov t:34] test";
            comment = CommentWrapper.Parse(commentString);
            Assert.AreEqual("test", comment.Comment);

            commentString = "[a:litvinov t:34] ";
            comment = CommentWrapper.Parse(commentString);
            Assert.AreEqual("", comment.Comment);
        }
    }
}
