using System;
using DXVcs2Git.Core;
using LibGit2Sharp;

namespace DXVcs2Git {
    public class GitWrapper : IDisposable {
        string path;
        Credentials credentials;
        string repoPath;
        string gitPath;
        Repository repo;
        public GitWrapper(string path, string gitPath, Credentials credentials) {
            this.path = path;
            this.credentials = credentials;
            this.gitPath = gitPath;
            this.repoPath = DirectoryHelper.IsGitDir(path) ? GitClone() : GitInit();
            repo = new Repository(repoPath);
        }
        public string GitInit() {
            return Repository.Init(path);
        }
        string GitClone() {
            CloneOptions options = new CloneOptions();
            options.CredentialsProvider += (url, fromUrl, types) => credentials;
            string clonedRepoPath = Repository.Clone(gitPath, path, options);
            System.Console.WriteLine($"========   Git repo Initialized   ===========");
            return clonedRepoPath;
        }
        public void Dispose() {
            throw new NotImplementedException();
        }
    }
}
