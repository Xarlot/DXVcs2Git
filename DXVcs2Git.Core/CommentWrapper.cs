namespace DXVcs2Git.Core {
    public class CommentWrapper {
        public string TimeStamp { get; set; }
        public string Sha { get; set; }
        public string Token { get; set; }
        public string Author { get; set; }
        public string Branch { get; set; }
        public string Comment { get; set; }

        public static CommentWrapper Parse(string comment) {
            return new CommentWrapper();
        }
        public override string ToString() {
            return $"[a:{Author} t:{Token}] " + Comment;
        }
    }
}
