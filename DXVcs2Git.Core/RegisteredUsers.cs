using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            ADUsers = ADWrapper.GetUsers().ToDictionary(x => x.UserName);
            Users = gitLabWrapper.GetUsers().Select(x => new User(x.Username, GetEmail(x.Username), x.Name, true)).ToDictionary(x => x.UserName, new UserNameEqualityComparer());
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
        static string CalcLogin(string x) {
            return VcsUserParser.ParseUser(x);
        }
    }
    class UserNameEqualityComparer : IEqualityComparer<string> {
        public bool Equals(string x, string y) {
            return x.ToUpperInvariant() == y.ToUpperInvariant();
        }
        public int GetHashCode(string obj) {
            return obj.ToUpperInvariant().GetHashCode();
        }
    }
    public static class VcsUserParser {
        static readonly Regex CheckStructure = new Regex(@"^[a-zA-Z]{4}\\[a-zA-Z]{1,}$", RegexOptions.Compiled);
        static readonly Regex ParseUserRegex = new Regex(@"(?<=^[a-zA-Z]{4}\\)\S+", RegexOptions.Compiled);

        public static string ParseUser(string x) {
            if (string.IsNullOrEmpty(x) || !CheckStructure.IsMatch(x))
                return null;
            return ParseUserRegex.Match(x).Value;
        }
    }
}
