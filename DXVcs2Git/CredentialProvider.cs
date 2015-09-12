using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace DXVcs2Git.Git {
    public class CredentialsProvider {
        Credentials credentials;
        public CredentialsProvider(Credentials credentials) {
            this.credentials = credentials;
        }
    }
}
