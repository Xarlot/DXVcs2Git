using System.Collections.Generic;
using System.Linq;
using DXVcs2Git.Core.AD;
using DXVcs2Git.DXVcs;
using DXVcs2Git.Git;
using DXVCS;

namespace DXVcs2Git.Core {
    public class RegisteredUsers {
        const string DefaultMail = "noreply@mail.com";
        public IDictionary<string, User> Users { get; private set; }
        IDictionary<string, User> ADUsers { get; set; }
        IList<UserInfo> VcsUsers { get; set; }
        readonly GitLabWrapper gitLabWrapper;
        readonly DXVcsWrapper vcsWrapper;
        public RegisteredUsers(GitLabWrapper gitLabWrapper, DXVcsWrapper vcsWrapper) {
            this.gitLabWrapper = gitLabWrapper;
            this.vcsWrapper = vcsWrapper;
            ADUsers = ADWrapper.GetUsers().ToDictionary(x => x.UserName.ToLowerInvariant());
            Users = gitLabWrapper.GetUsers().Select(x => new User(x.Username, x.Email, x.Name)).ToDictionary(x => x.UserName);
            this.VcsUsers = vcsWrapper.GetUsers().ToList();
        }

        public User GetUser(string name) {
            User user;
            if (Users.TryGetValue(name, out user))
                return user;
            user = FindAndRegisterUser(name);
            return user ?? new User(name, DefaultMail, name);
        }
        private User FindAndRegisterUser(string name) {
            var userInfo = VcsUsers.FirstOrDefault(x => x.Name == name);
            if (string.IsNullOrEmpty(userInfo.Name) || userInfo.Logins == null || userInfo.Logins.Length == 0)
                return null;
            string loginCandidate = userInfo.Logins.FirstOrDefault(x => {
                var check = CalcLogin(x);
                User checkUser;
                return ADUsers.TryGetValue(check, out checkUser);
            });
            if (string.IsNullOrEmpty(loginCandidate))
                return null;
            string login = CalcLogin(loginCandidate);
            User adUser = ADUsers[login];
            User gitLabUser = new User(userInfo.Name, adUser.Email, adUser.DisplayName);
            this.gitLabWrapper.RegisterUser(gitLabUser.UserName, gitLabUser.DisplayName, gitLabUser.Email);
            Users.Add(gitLabUser.UserName, gitLabUser);
            return gitLabUser;
        }
        private static string CalcLogin(string x) {
            return x.Replace(@"corp\", "");
        }
    }
}
