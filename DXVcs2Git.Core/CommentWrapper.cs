using System.Text.RegularExpressions;

namespace DXVcs2Git.Core {
    public class CommentWrapper {
        //new Regex(@"(?<=sha:\s*)[0-9a-f]+", RegexOptions.Compiled);
        static readonly Regex parseAuthorRegex = new Regex(@"(?<=a:)\w+", RegexOptions.Compiled);
        static readonly Regex parseTokenRegex = new Regex(@"(?<=t:)\w+", RegexOptions.Compiled);
        static readonly Regex parseCommentRegex = new Regex(@"\[a:\w+ t:\w+\]", RegexOptions.Compiled);

        public string TimeStamp { get; set; }
        public string Sha { get; set; }
        public string Token { get; set; }
        public string Author { get; set; }
        public string Branch { get; set; }
        public string Comment { get; set; }

        public static CommentWrapper Parse(string comment) {
            if (!CheckStructure(comment))
                return new CommentWrapper();
            return new CommentWrapper() {
                Author = ParseAuthor(comment),
                Token = ParseToken(comment),
            };
        }
        static bool CheckStructure(string comment) {
            return parseCommentRegex.IsMatch(comment);
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
