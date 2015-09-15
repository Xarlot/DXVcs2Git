using System.Collections.Generic;
using System.Linq;

namespace DXVcs2Git.Core {
    public class RegisteredUsers {
        const string DefaultMail = "noreply@mail.com";
        public IDictionary<string, User> Users { get; private set; }
        public RegisteredUsers(IEnumerable<User> users) {
            Users = users.ToDictionary(x => x.Name, x => x);
        }

        public User GetUser(string name) {
            User user;
            if (Users.TryGetValue(name, out user))
                return user;
            return new User(name, DefaultMail);
        }
    }
}
