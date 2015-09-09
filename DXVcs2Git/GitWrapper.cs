using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace DXVcs2Git {
    public static class GitWrapper {
        public static string GitInit(string path) {
            return Repository.Init(path);
        }
        public static string GitClone(string sourceUrl, string repoPath) {
            return null;
        }
    }
}
