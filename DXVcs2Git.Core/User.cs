namespace DXVcs2Git.Core {
    public class User {
        public User(string userName, string email, string displayName) {
            UserName = userName;
            Email = email;
            DisplayName = displayName;
        }
        protected bool Equals(User other) {
            return string.Equals(UserName, other.UserName);
        }
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((User)obj);
        }
        public override int GetHashCode() {
            return UserName?.GetHashCode() ?? 0;
        }
        public override string ToString() {
            return DisplayName;
        }
        public string Email { get;  }
        public string UserName { get; }
        public string DisplayName { get; }
    }
}
