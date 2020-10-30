﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using DXVcs2Git.Core;
using User = DXVcs2Git.Core.User;
using System.Diagnostics;
using System.IO;
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
               "clone", "--filter=blob:none", "--no-checkout", "--branch", branch, Escape(remote), Escape(localPath)
            };
            string output, errors;
            var code = WaitForProcess(gitPath, ".", out output, out errors, args);
            CheckFail(code, output, errors);
        }
        public void Add(string repoPath, string relativePath, bool force) {
            string output, errors;
            try {
                var opt = force ? new[] {"add", "-f", relativePath} : new[] {"add", relativePath};
                var code = WaitForProcess(gitPath, repoPath, out output, out errors, opt);
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
        public void AddRemote(string repoPath, string remote, string path) {
            var code = WaitForProcess(gitPath, repoPath, out string output, out string errors, "remote add", remote, path);
            CheckFail(code, output, errors);
        }
        public void ResetHard(string repoPath) {
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, "reset", "--hard");
            CheckFail(code, output, errors);
        }
        public void Pull(string repoPath) {
            var args = new[] {
                "pull",
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

        public void Checkout(string repoPath, string branch, bool track = true) {
            string output, errors;
            string args = track ? "checkout -B " : "checkout";
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, args, branch, $"origin/{branch}");
            CheckFail(code, output, errors);
        }

        public void Init(string repoPath) {
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, "init");
            CheckFail(code, output, errors);
        }
        public void FetchRemoteBranch(string repoPath, string remote, string branch) {
            var code = WaitForProcess(gitPath, repoPath, out string output, out string errors, "fetch", remote, $@"{branch}:refs/remotes/{remote}/{branch}");
            CheckFail(code, output, errors);
        }
        public void DiffWithRemoteBranch(string repoPath, string remote, string branch) {
            var code = WaitForProcess(gitPath, repoPath, out string output, out string errors, "diff --name-status", branch, $@"{remote}/{branch}");
            CheckFail(code, output, errors);
        }
        public GitDiff[] Diff(string repoPath, string from, string to) { 
            var args = new[] {
                "diff",
                "--name-status",
                from,
                to
            };
            string output, errors;
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, args);
            CheckFail(code, output, errors);
            var strings = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var status = new List<GitDiff>(strings.Length);
            foreach (var s in strings) {
                string path = s.Remove(0, s.IndexOf('\t') + 1);
                if (s.StartsWith("R")) {
                    var paths = path.Split('\t').ToArray();
                    status.Add(new GitDiff() {Status = GitDiffStatus.Moved, OldPath = paths[0], NewPath = paths[1]});
                }
                else {
                    var gitDiff = new GitDiff() {OldPath = path, NewPath = path};
                    gitDiff.Status = CalcGitDiffStatus(s);
                    status.Add(gitDiff);
                }
            }
            return status.ToArray();
        }
        GitDiffStatus CalcGitDiffStatus(string line) {
            if (line.StartsWith("M", StringComparison.InvariantCultureIgnoreCase))
                return GitDiffStatus.Modified;
            if (line.StartsWith("A", StringComparison.InvariantCultureIgnoreCase))
                return GitDiffStatus.Added;
            if (line.StartsWith("D", StringComparison.InvariantCultureIgnoreCase))
                return GitDiffStatus.Removed;
            throw new ArgumentException("GitDiffStatus");
        }
        public void Fetch(string remote, string repoPath, bool tags) {
            string output, errors;
            var args = new List<string>();
            args.Add("fetch");
            if (!string.IsNullOrEmpty(remote)) {
                args.Add("origin");
                args.Add(remote);
            }
            else {
                args.Add("--all");
                args.Add("--depth");
                args.Add("1");
            }
            if (tags) {
                args.Add("--tags");
            }
            var code = WaitForProcess(gitPath, repoPath, out output, out errors, args.ToArray());
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
        public void Config(string repoPath, string property, string value) {
            var code = WaitForProcess(gitPath, repoPath, out string output, out string errors, "config", property, value);
            CheckFail(code, output, errors);
        }
        public void SparseCheckout(string repoPath, string branch, string sparseCheckoutFile) {
            string sparseCheckoutPath = Path.Combine(repoPath, ".git", "info", "sparse-checkout");
            File.WriteAllText(sparseCheckoutPath, sparseCheckoutFile);
            Checkout(repoPath, branch);
        }
        public void SparseCheckoutSet(string repoPath, string dirs) {
            var code = WaitForProcess(gitPath, repoPath, out var output, out var errors, "sparse-checkout", "set", dirs);
            CheckFail(code, output, errors);
        }
        public void SparseCheckoutInit(string repoPath) {
            var code = WaitForProcess(gitPath, repoPath, out var output, out var errors, "sparse-checkout", "init", "--cone");
            CheckFail(code, output, errors);
        }
        public void ReadTree(string repoPath, string sparseCheckoutFile) {
            string sparseCheckoutPath = Path.Combine(repoPath, ".git", "info", "sparse-checkout");
            File.WriteAllText(sparseCheckoutPath, sparseCheckoutFile);
            var code = WaitForProcess(gitPath, repoPath, out string output, out string errors, "read-tree -mu HEAD");
            CheckFail(code, output, errors);
        }
        
    }

    public enum GitDiffStatus {
        Added,
        Modified,
        Moved,
        Removed,
    }
    public class GitDiff {
        public GitDiffStatus Status { get; set; }
        public string OldPath { get; set; }
        public string NewPath { get; set; }
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
            GitDirectory = localPath;
            if (!DirectoryHelper.IsGitDir(localPath))
                GitClone(branch);
            GitInit(branch);
            Pull();

            Log.Message("End initializing git repo");
        }
        public void GitInit(string branch) {
            Fetch(branch);
            SparseCheckoutInit();
            SparseCheckout(branch, string.Empty);
        }
        void GitClone(string branch) {
            gitCmd.ShallowClone(localPath, branch, remotePath);
            Log.Message($"Git repo {localPath} was cloned with branch {branch}.");
        }
        public void Dispose() {
        }
        public void AddRemote(string remote, string path) {
            gitCmd.AddRemote(localPath, remote, path);
        }
        public void Fetch(string remote = "", bool updateTags = false) {
            gitCmd.Fetch(remote, localPath, updateTags);
        }
        public void FetchRemoteBranch(string remote, string branch) {
            gitCmd.FetchRemoteBranch(localPath, remote, branch);
        }
        public void DiffWithRemoteBranch(string remote, string branch) {
            gitCmd.DiffWithRemoteBranch(localPath, remote, branch);
        }
        public void Pull() {
            gitCmd.Pull(localPath);
            ;
        }
        public void LFSPull() {
            gitCmd.LFSPull(localPath);
        }
        public void Stage(string path, bool force) {
            gitCmd.Add(localPath, path, force);
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
        public void CheckOut(string branch, bool track = true) {
            gitCmd.Checkout(localPath, branch, track);
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
        public void Config(string property, string value) {
            gitCmd.Config(localPath, property, value);
        }
        public void SparseCheckout(string branch, string sparseCheckoutConfig) {
            SparseCheckoutSet(sparseCheckoutConfig);
            CheckOut(branch);
        }
        public void SparseCheckoutSet(string content) {
            gitCmd.SparseCheckoutSet(localPath, content);
        }
        //git sparse-checkout init --cone
        public void SparseCheckoutInit() {
            gitCmd.SparseCheckoutInit(localPath);
        }
        public void ReadTree(string sparseCheckoutFile) {
            gitCmd.ReadTree(localPath, sparseCheckoutFile);
        }
        public GitDiff[] Diff(string from, string to) {
            return gitCmd.Diff(localPath, from, to);
        }
    }
}
