using System.Text;
using DXVcs2Git.Core;

namespace DXVcs2Git.Git {
    public class GitCommentsGenerator : CommentsGenerator {
        public static readonly GitCommentsGenerator Instance = new GitCommentsGenerator();
        protected override void ConvertToStringInternal(CommentWrapper comment, StringBuilder sb) {
            WriteBranch(comment, sb);
            WriteToken(comment, sb);
            WriteTimestamp(comment, sb);
            WriteSha(comment, sb);
        }
    }
}
