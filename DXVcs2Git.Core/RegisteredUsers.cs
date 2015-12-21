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
        public RegisteredUsers(GitLabWrapper gitLabWrapper, DXVcsWrapper vcsWrapper) {
            this.gitLabWrapper = gitLabWrapper;
            ADUsers = ADWrapper.GetUsers().ToDictionary(x => x.UserName.ToLowerInvariant());
            Users = gitLabWrapper.GetUsers().Select(x => new User(x.Username, GetEmail(x.Username), x.Name, true)).ToDictionary(x => x.UserName);
            this.VcsUsers = vcsWrapper.GetUsers().ToList();
        }

        public User GetUser(string name) {
            User user;
            if (Users.TryGetValue(name, out user))
                return user;
            user = FindAndRegisterUser(name);
            return user ?? new User(name, DefaultMail, name);
        }
        string GetEmail(string userName) {
            User user;
            if (ADUsers.TryGetValue(userName, out user))
                return user.Email;
            return DefaultMail;
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
            User gitLabUser = new User(userInfo.Name, adUser.Email, adUser.DisplayName, true);
            this.gitLabWrapper.RegisterUser(gitLabUser.UserName, gitLabUser.DisplayName, gitLabUser.Email);
            Users.Add(gitLabUser.UserName, gitLabUser);
            return gitLabUser;
        }
        private static string CalcLogin(string x) {
            string lowerX = x.ToLower();
            string removeCorp = lowerX.Replace(@"corp\", "");
            return removeCorp;
        }
    }
}
