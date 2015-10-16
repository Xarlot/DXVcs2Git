using System;

namespace DXVcs2Git.Core {
    public class CommentsGenerator {
        public Comment Parse(string comment) {
            return new Comment();
        }
        public string ConvertToString(Comment comment) {
            return String.Empty;
        }

    }
}
