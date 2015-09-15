namespace DXVcs2Git.Core {
    public class User {
        public string Mail { get; set; }
        public string Name { get; set; }
        public User() {
        }
        public User(string name, string mail) {
            Name = name;
            Mail = mail;
        }
    }
}
