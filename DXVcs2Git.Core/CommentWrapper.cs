using System.Text.RegularExpressions;

namespace DXVcs2Git.Core {
    public class CommentWrapper {
        //new Regex(@"(?<=sha:\s*)[0-9a-f]+", RegexOptions.Compiled);
        static readonly Regex parseAuthorRegex = new Regex(@"(?<=a:)\S+", RegexOptions.Compiled);
        static readonly Regex parseTokenRegex = new Regex(@"(?<=t:)\S+(?=\])", RegexOptions.Compiled);
        static readonly Regex parseStructureRegex = new Regex(@"\[a:\S+ t:\S+\]", RegexOptions.Compiled);
        static readonly Regex parseCommentRegex = new Regex(@"(?<=\[.+\] ).+", RegexOptions.Compiled);

        public string TimeStamp { get; set; }
        public string Sha { get; set; }
        public string Token { get; set; }
        public string Author { get; set; }
        public string Branch { get; set; }
        public string Comment { get; set; }

        public static CommentWrapper Parse(string comment) {
            if (!CheckStructure(comment))
                return new CommentWrapper() {Comment = comment};
            return new CommentWrapper() {
                Author = ParseAuthor(comment),
                Token = ParseToken(comment),
                Comment = ParseComment(comment),
            };
        }
        public static bool IsAutoSyncComment(string comment) {
            return CheckStructure(comment);
        }
        static string ParseComment(string comment) {
            return parseCommentRegex.Match(comment).Value;
        }
        static bool CheckStructure(string comment) {
            return parseStructureRegex.IsMatch(comment);
        }
        static string ParseAuthor(string comment) {
            return parseAuthorRegex.Match(comment).Value;
        }
        static string ParseToken(string comment) {
            return parseTokenRegex.Match(comment).Value;
        }
        public override string ToString() {
            return $"[a:{Author} t:{Token}] " + Comment;
        }
    }
}
