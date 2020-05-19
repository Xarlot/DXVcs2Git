using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace DXVcs2Git.Core.AD {
    public static class ADWrapper {
        public static IEnumerable<User> GetUsers() {
            List<User> users = new List<User>();
            try {
                using var context = new PrincipalContext(ContextType.Domain, "corp.devexpress.com");
                using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                foreach (var result in searcher.FindAll()) {
                    DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;
                    string email = $"{(string)de.Properties["mail"].Value}";
                    string displayName = (string)de.Properties["displayname"].Value;
                    string userName = (string)de.Properties["samaccountname"].Value;
                    displayName = string.IsNullOrEmpty(displayName) ? displayName : displayName.Replace(" (DevExpress)", string.Empty);
                    users.Add(new User(userName, email, displayName));
                }
            }
            catch (Exception ex) {
                return users.Where(x => !string.IsNullOrEmpty(x.Email)).ToList();
            }

            return users;
        }
    }
}