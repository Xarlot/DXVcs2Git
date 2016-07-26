using System;
using System.Collections.Generic;
using System.Linq;
using DXVcs2Git.Core;
using User = DXVcs2Git.Core.User;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DXVcs2Git {
    class GitCmdWrapper {
        string gitPath;
        public GitCmdWrapper(string gitPath) {
            this.gitPath = gitPath;
        }

        static int WaitForProcess(string fileName, string workingDir, out string output, out string errors, params string[] args) {
            var proc = new Process();
            proc.StartInfo.WorkingDirectory = workingDir;
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.Arguments = args.Length == 0 ? "" : args.Aggregate((l, r) => l + " " + r);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();
            if (!proc.HasExited) {
                try {
                    proc.PriorityClass = ProcessPriorityClass.High;
                }
                catch { }
            }

            var outputTask = Task.Factory.StartNew(() => proc.StandardOutput.ReadToEnd());
            var errorTask = Task.Factory.StartNew(() => proc.StandardError.ReadToEnd());

            output = string.Empty;
            errors = string.Empty;

            bool result = proc.WaitForExit(12000000);
            if (!result) {
                proc.Kill();
                throw new Exception("process timed out");
            }
            else {
                Task.WaitAll(outputTask, errorTask);
                output = outputTask.Result.ToString();
                errors = errorTask.Result.ToString();
            }
            return proc.ExitCode;
        }

        static void CheckFail(int code, string output, string errors) {
            if (code != 0) {
                Log.Message("Git return output:");
                Log.Message(output);
                Log.Message("Git return error:");
                Log.Message(errors);
                throw new Exception("git invocation failed");
            }
        }

        static string Escape(string str) {
            return '"' + str + '"';
        }

        public void ShallowClone(string localPath, string branch, string remote) {
            var args = new[] {
                "-c", "filter.lfs.smudge=", "-c", "filter.lfs.required=false", "clone", "--depth", "1", "--branch", branch, Escape(remote), Escape(localPath)
            };
            string output, errors;
            var code = WaitForProcess(gitPath, ".", out output, out errors, args);
            CheckFail(code, output, errors);
        }
        public void Add(string repoPath, string relativePath) {
            string output, errors;
            try {
                var code = WaitForProcess(gitPath, repoPath, out output, out errors, "add", relativePath);
                CheckFail(code, output, errors);
            }
            catch {
            }
        }

        public void Commit(string repoPath, string comment, string author, string date) {
            try {
                var args = new[] {
                    "commit",
                    "-m", Escape(EscapeDoubleQuotes(comment)),
                    "--author", Escape(author),
                };
                Environment.SetEnvironmentVariable("GIT_AUTHOR_DATE", date);
                Environment.SetEnvironmentVariable("GIT_COMMITTER_DATE", date);
                string output, errors;
                var code = WaitForProcess(gitPath, repoPath, out output, out errors, args);
                CheckFail(code, output, errors);
            }
            catch {
            }
        }
        string EscapeDoubleQuotes(string comment) {
            if (string.IsNullOrEmpty(comment))
                return comment;
            return comment.Replace("\"", "\\\"");
        }

        public void ResetHard(string repoPath) {
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, "reset", "--hard");
            CheckFail(code, output, errors);
        }
        public void Pull(string repoPath) {
            var args = new[] {
                "-c", "filter.lfs.smudge=", "-c", "filter.lfs.required=false", "pull", "--depth", "2"
            };

            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, args);
            CheckFail(code, output, errors);
        }
        public void LFSPull(string repoPath) {
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, "lfs pull");
            CheckFail(code, output, errors);
        }
        public void Push(string repoPath) {
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, "push");
            CheckFail(code, output, errors);
        }

        public void Checkout(string repoPath, string branch) {
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, "checkout", "-B", branch);
            CheckFail(code, output, errors);
        }

        public void Init(string repoPath) {
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, "init");
            CheckFail(code, output, errors);
        }
        public void Fetch(string remote, string repoPath, bool tags) {
            string output, errors;
            var args = new List<string>();
            args.Add("fetch");
            args.Add("--depth");
            args.Add("1");
            if (!string.IsNullOrEmpty(remote))
                args.Add(remote);
            if (tags) {
                args.Add("--tags");
            }
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, "fetch");
            CheckFail(code, output, errors);
        }
        public void Merge(string repoPath, string remote, string targetBranch, string sourceBranch) {
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, "merge", remote + $@"/{targetBranch}", $"{sourceBranch}");
            CheckFail(code, output, errors);
        }
        string GetLog(string repoPath, int from, string format) {
            var args = new[] {
                "log",
                
                string.Format("HEAD~{0}", from),
                "-1",
                string.Format("--pretty=format:\"{0}\"", format)
            };
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, args);
            CheckFail(code, output, errors);
            return output.Trim();
        }

        public GitCommit FindCommit(string repoPath, Func<GitCommit, bool> pred) {
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors,
                "rev-list", "HEAD", "--count");
            CheckFail(code, output, errors);
            int count = int.Parse(output);

            return Enumerable.Range(0, count).Select(i =>
                new GitCommit {
                    Sha = GetLog(repoPath, i, "%H"),
                    Message = GetLog(repoPath, i, "%B")
                }
            ).FirstOrDefault(pred);
        }
    }

    public class GitCommit {
        public string Sha;
        public string Message;
    }

    public class GitCredentials {
        public string User;
        public string Password;
    }

    public class GitWrapper : IDisposable {
        readonly string localPath;
        readonly string remotePath;
        readonly GitCmdWrapper gitCmd;
        public string GitDirectory { get; }
        public GitCredentials Credentials { get; }

        public GitWrapper(string localPath, string remotePath, string branch, GitCredentials credentials) {
            this.localPath = localPath;
            this.Credentials = credentials;
            this.remotePath = remotePath;
            gitCmd = new GitCmdWrapper(@"C:\Program Files\Git\cmd\git.exe");
            Log.Message("Start initializing git repo");
            this.GitDirectory = localPath;
            if (DirectoryHelper.IsGitDir(localPath)) {
                GitInit(branch);
            }
            else {
                GitClone(branch);
                LFSPull();
            }
            Log.Message("End initializing git repo");
        }
        public void GitInit(string branch) {
            gitCmd.Init(localPath);
            CheckOut(branch);
        }
        void GitClone(string branch) {
            gitCmd.ShallowClone(localPath, branch, remotePath);
            Log.Message($"Git repo {localPath} was cloned with branch {branch}.");
        }
        public void Dispose() {
        }
        public void Fetch(string remote = "", bool updateTags = false) {
            gitCmd.Fetch(remote, localPath, updateTags);
        }
        public void Pull() {
            gitCmd.Pull(localPath);;
        }
        public void LFSPull() {
            gitCmd.LFSPull(localPath);
        }
        public void Stage(string path) {
            gitCmd.Add(localPath, path);
            Log.Message($"Git stage performed.");
        }
        public void Commit(string comment, User user, DateTime timeStamp, bool allowEmpty = true) {
            var userString = $"{user.UserName} <{user.Email}>";
            var time = timeStamp.ToLocalTime().ToString();
            gitCmd.Commit(localPath, comment, userString, time);
        }
        public void PushEverything() {
            gitCmd.Push(localPath);
        }
        string GetOriginName(string name) {
            return $"origin/{name}";
        }
        public void CheckOut(string branch) {
            gitCmd.Checkout(localPath, branch);
            Log.Message($"Git repo {localPath} was checked out for branch {branch}.");
        }
        public void Reset() {
            gitCmd.ResetHard(localPath);
        }
        public GitCommit FindCommit(Func<GitCommit, bool> pred) {
            return gitCmd.FindCommit(localPath, pred);
        }
        public void Merge(string upstream, string targetBranch, string sourceBranch) {
            gitCmd.Merge(localPath, upstream, targetBranch, sourceBranch);
        }
    }
}
