using System;
using System.Text;
using DXVcs2Git.Core;

namespace DXVcs2Git.DXVcs {
    public class VcsCommentsGenerator : CommentsGenerator {
        public static readonly VcsCommentsGenerator Instance = new VcsCommentsGenerator();
        protected override void ConvertToStringInternal(CommentWrapper comment, StringBuilder sb) {
            WriteAuthor(comment, sb);
            WriteBranch(comment, sb);
            WriteToken(comment, sb);
            WriteTimestamp(comment, sb);
            WriteSha(comment, sb);
        }
    }
}
