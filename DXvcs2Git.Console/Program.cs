using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using CommandLine;
using DXVcs2Git.Core;
using DXVcs2Git.Core.GitLab;
using DXVcs2Git.Core.Serialization;
using DXVcs2Git.DXVcs;
using DXVcs2Git.Git;
using DXVcs2Git.UI.Farm;
using NGitLab;
using NGitLab.Models;
using ProjectHookType = DXVcs2Git.Core.GitLab.ProjectHookType;
using User = DXVcs2Git.Core.User;
using Ionic.Zip;

namespace DXVcs2Git.Console {
    internal class Program {
        static IPAddress ipAddress;
        static IPAddress IP { get { return ipAddress ?? (ipAddress = DetectMyIP()); } }
        static IPAddress DetectMyIP() {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        const string repoPath = "repo";
        const string vcsServer = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService";
        const int MaxChangesCount = 1000;
        static void Main(string[] args) {
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            var exitCode = result.MapResult(clo => {
                try {
                    return DoWork(clo);
                }
                catch (Exception ex) {
                    Log.Error("Application crashed with exception", ex);
                    return 1;
                }
            },
            errors => 1);
            Environment.Exit(exitCode);
        }

        static int DoWork(CommandLineOptions clo) {
            WorkMode workMode = clo.WorkMode;

            if (workMode == WorkMode.listener) {
                return DoListenerWork(clo);
            }
            if (workMode == WorkMode.synchronizer) {
                return DoSyncWork(clo);
            }
            if (workMode == WorkMode.patch) {
                return DoPatchWork(clo);
            }
            return 1;
        }
        static int DoPatchWork(CommandLineOptions clo) {
            string localGitDir = clo.LocalFolder != null && Path.IsPathRooted(clo.LocalFolder) ? clo.LocalFolder : Path.Combine(Environment.CurrentDirectory, clo.LocalFolder ?? repoPath);
            EnsureGitDir(localGitDir);

            string gitRepoPath = clo.Repo;
            string username = clo.Login;
            string password = clo.Password;
            string gitlabauthtoken = clo.AuthToken;
            string branchName = clo.Branch;
            string trackerPath = clo.Tracker;
            string gitServer = clo.Server;
            int mergeRequestId = clo.MergeRequestId;

            DXVcsWrapper vcsWrapper = new DXVcsWrapper(vcsServer, username, password);

            TrackBranch branch = FindBranch(branchName, trackerPath, vcsWrapper);
            if (branch == null)
                return 1;

            string historyPath = GetVcsSyncHistory(vcsWrapper, branch.HistoryPath);
            if (historyPath == null)
                return 1;
            SyncHistory history = SyncHistory.Deserialize(historyPath);
            if (history == null)
                return 1;

            SyncHistoryWrapper syncHistory = new SyncHistoryWrapper(history, vcsWrapper, branch.HistoryPath, historyPath);
            var head = syncHistory.GetHistoryHead();
            if (head == null)
                return 1;

            GitLabWrapper gitLabWrapper = new GitLabWrapper(gitServer, gitlabauthtoken);

            Project project = gitLabWrapper.FindProject(gitRepoPath);
            MergeRequest mergeRequest = gitLabWrapper.GetMergeRequests(project, x => x.Id == mergeRequestId).FirstOrDefault();
            if (mergeRequest == null) {
                Log.Error($"Can`t find merge request with id = {mergeRequestId}");
                return 1;
            }

            GitWrapper gitWrapper = CreateGitWrapper(gitRepoPath, localGitDir, branch, username, password);
            if (gitWrapper == null)
                return 1;

            var changes = gitLabWrapper.GetMergeRequestChanges(mergeRequest).Select(x => new PatchItem() {
                SyncAction = CalcSyncAction(x),
                OldPath = x.OldPath,
                NewPath = x.NewPath,
            }).ToList();

            var patch = new PatchInfo() {TimeStamp = DateTime.Now.Ticks, Items = changes};

            using (Package zip = Package.Open(Path.Combine(localGitDir, "patch.zip"), FileMode.CreateNew)) {
                foreach (var path in CalcFilesForPatch(localGitDir, patch)) {
                    AddPart(zip, localGitDir, path);
                }
            }
            return 0;
        }
        static SyncAction CalcSyncAction(MergeRequestFileData fileData) {
            if (fileData.IsDeleted)
                return SyncAction.Delete;
            if (fileData.IsNew)
                return SyncAction.New;
            if (fileData.IsRenamed)
                return SyncAction.Move;
            return SyncAction.Modify;
        }
        static void AddPart(Package zip, string root, string path) {
            Uri uri = PackUriHelper.CreatePartUri(new Uri(path, UriKind.Relative));
            if (zip.PartExists(uri)) {
                zip.DeletePart(uri);
            }
            PackagePart part = zip.CreatePart(uri, "", CompressionOption.Normal);
            using (FileStream fileStream = new FileStream(Path.Combine(root, path), FileMode.Open, FileAccess.Read)) {
                using (Stream dest = part.GetStream()) {
                    CopyStream(fileStream, dest);
                }
            }
        }
        static void CopyStream(System.IO.FileStream inputStream, System.IO.Stream outputStream) {
            long bufferSize = inputStream.Length < Int16.MaxValue ? inputStream.Length : Int16.MaxValue;
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;
            long bytesWritten = 0;
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) != 0) {
                outputStream.Write(buffer, 0, bytesRead);
                bytesWritten += bufferSize;
            }
        }
        static IEnumerable<string> CalcFilesForPatch(string rootPath, PatchInfo patch) {
            var changes = patch.Items;
            yield return SavePatchInfo(rootPath, patch);
            foreach (var change in changes) {
                if (change.SyncAction == SyncAction.Delete)
                    yield break;
                yield return change.SyncAction == SyncAction.Move ? change.NewPath : change.OldPath;
            }
        }
        static string SavePatchInfo(string rootPath, PatchInfo patch) {
            Serializer.Serialize(Path.Combine(rootPath, "patch.info"), patch);
            return "patch.info";
        }

        static int DoListenerWork(CommandLineOptions clo) {
            string gitServer = clo.Server;
            string gitlabauthtoken = clo.AuthToken;
            Stopwatch sw = Stopwatch.StartNew();

            GitLabWrapper gitLabWrapper = new GitLabWrapper(gitServer, gitlabauthtoken);
            FarmIntegrator.Start(Dispatcher.CurrentDispatcher, null);

            var projects = gitLabWrapper.GetProjects();
            foreach (Project project in projects) {
                var hooks = gitLabWrapper.GetHooks(project);
                foreach (ProjectHook hook in hooks) {
                    if (WebHookHelper.IsSameHost(hook.Url, IP) || !WebHookHelper.IsSharedHook(hook.Url))
                        continue;
                    Uri url = WebHookHelper.Replace(hook.Url, IP);
                    gitLabWrapper.UpdateProjectHook(project, hook, url);
                    Log.Message($"WebHook registered for project {project.Name} at {url}");
                }
            }

            WebServer server = new WebServer(WebHookHelper.GetSharedHookUrl(IP));
            server.Start();
            while (sw.Elapsed.Minutes < clo.Timeout) {
                Thread.Sleep(10);
                var request = server.GetRequest();
                if (request == null)
                    continue;
                ProcessWebHook(gitLabWrapper, request);
            }

            return 0;
        }
        static void ProcessWebHook(GitLabWrapper gitLabWrapper, WebHookRequest request) {
            var hookType = ProjectHookClient.ParseHookType(request);
            if (hookType == null)
                return;
            Log.Message($"Web hook received.");
            Log.Message($"Web hook type: {hookType.HookType}.");
            var hook = ProjectHookClient.ParseHook(hookType);
            if (hook.HookType == ProjectHookType.push)
                ProcessPushHook((PushHookClient)hook);
            else if (hook.HookType == ProjectHookType.merge_request)
                ProcessMergeRequestHook(gitLabWrapper, (MergeRequestHookClient)hook);
        }
        static void ProcessMergeRequestHook(GitLabWrapper gitLabWrapper, MergeRequestHookClient hook) {
            Log.Message($"Merge hook title: {hook.Attributes.Description}");
            Log.Message($"Merge hook state: {hook.Attributes.State}");

            if (!IsOpenedState(hook))
                return;

            Log.Message($"Merge hook action: {hook.Attributes.Action}");
            Log.Message($"Merge hook merge status: {hook.Attributes.MergeStatus}");
            Log.Message($"Merge hook author: {gitLabWrapper.GetUser(hook.Attributes.AuthorId).Name}.");
            Log.Message($"Merge hook target branch: {hook.Attributes.TargetBranch}.");
            Log.Message($"Merge hook sourceBranch branch: {hook.Attributes.SourceBranch}.");

            if (!ShouldForceSyncTask(gitLabWrapper, hook))
                return;
            var project = gitLabWrapper.GetProject(hook.Attributes.TargetProjectId);
            var mergeRequest = gitLabWrapper.GetMergeRequest(project, hook.Attributes.Id);
            var xmlComments = gitLabWrapper.GetComments(mergeRequest).Where(x => IsXml(x.Note));
            var options = xmlComments.Select(x => MergeRequestOptions.ConvertFromString(x.Note)).FirstOrDefault();
            if (options != null)
                ForceBuild(options.SyncTask);
            else
                Log.Message("Merge request can`t be merged because merge request notes has no farm config.");
            Log.Message("");
        }
        static bool ShouldForceSyncTask(GitLabWrapper gitLabWrapper, MergeRequestHookClient hook) {
            var project = gitLabWrapper.GetProject(hook.Attributes.TargetProjectId);
            var mergeRequest = gitLabWrapper.GetMergeRequest(project, hook.Attributes.Id);
            var assignee = mergeRequest.Assignee;
            if (assignee == null || !assignee.Name.StartsWith("dxvcs2git")) {
                Log.Message("Force sync rejected because assignee is not set or not admin.");
                return false;
            }

            if (hook.Attributes.WorkInProcess) {
                Log.Message("Force sync rejected because merge request has work in process flag.");
                return false;
            }

            if (hook.Attributes.MergeStatus == "unchecked" || hook.Attributes.MergeStatus == "can_be_merged")
                return true;
            Log.Message("Force sync rejected because merge request can`t be merged automatically.");
            return false;
        }
        static void ForceBuild(string syncTask) {
            Log.Message($"Build forced: {syncTask}");
            FarmIntegrator.ForceBuild(syncTask);
        }
        static bool IsXml(string xml) {
            return !string.IsNullOrEmpty(xml) && xml.StartsWith("<");
        }
        static bool IsOpenedState(MergeRequestHookClient hook) {
            return hook.Attributes.State == "opened" || hook.Attributes.State == "reopened";
        }
        static void ProcessPushHook(PushHookClient hook) {
        }
        static int DoSyncWork(CommandLineOptions clo) {
            string localGitDir = clo.LocalFolder != null && Path.IsPathRooted(clo.LocalFolder) ? clo.LocalFolder : Path.Combine(Environment.CurrentDirectory, clo.LocalFolder ?? repoPath);
            EnsureGitDir(localGitDir);

            string gitRepoPath = clo.Repo;
            string username = clo.Login;
            string password = clo.Password;
            string gitlabauthtoken = clo.AuthToken;
            string branchName = clo.Branch;
            string trackerPath = clo.Tracker;
            string gitServer = clo.Server;

            DXVcsWrapper vcsWrapper = new DXVcsWrapper(vcsServer, username, password);

            TrackBranch branch = FindBranch(branchName, trackerPath, vcsWrapper);
            if (branch == null)
                return 1;

            string historyPath = GetVcsSyncHistory(vcsWrapper, branch.HistoryPath);
            if (historyPath == null)
                return 1;
            SyncHistory history = SyncHistory.Deserialize(historyPath);
            if (history == null)
                return 1;

            SyncHistoryWrapper syncHistory = new SyncHistoryWrapper(history, vcsWrapper, branch.HistoryPath, historyPath);
            var head = syncHistory.GetHistoryHead();
            if (head == null)
                return 1;

            GitLabWrapper gitLabWrapper = new GitLabWrapper(gitServer, gitlabauthtoken);
            RegisteredUsers registeredUsers = new RegisteredUsers(gitLabWrapper, vcsWrapper);
            User defaultUser = registeredUsers.GetUser(username);
            if (!defaultUser.IsRegistered) {
                Log.Error($"default user {username} is not registered in the active directory.");
                return 1;
            }
            var checkMergeChangesResult = CheckChangesForMerging(gitLabWrapper, gitRepoPath, branchName, head, vcsWrapper, branch, syncHistory, defaultUser);
            if (checkMergeChangesResult == CheckMergeChangesResult.NoChanges)
                return 0;
            if (checkMergeChangesResult == CheckMergeChangesResult.Error)
                return 1;

            GitWrapper gitWrapper = CreateGitWrapper(gitRepoPath, localGitDir, branch, username, password);
            if (gitWrapper == null)
                return 1;

            ProcessHistoryResult processHistoryResult = ProcessHistory(vcsWrapper, gitWrapper, registeredUsers, defaultUser, gitRepoPath, localGitDir, branch, clo.CommitsCount, syncHistory, true);
            if (processHistoryResult == ProcessHistoryResult.NotEnough)
                return 0;
            if (processHistoryResult == ProcessHistoryResult.Failed)
                return 1;

            int result = ProcessMergeRequests(vcsWrapper, gitWrapper, gitLabWrapper, registeredUsers, defaultUser, gitRepoPath, localGitDir, clo.Branch, clo.Tracker, syncHistory, username);
            if (result != 0)
                return result;
            return 0;
        }
        static CheckMergeChangesResult CheckChangesForMerging(GitLabWrapper gitLabWrapper, string gitRepoPath, string branchName, SyncHistoryItem head, DXVcsWrapper vcsWrapper, TrackBranch branch, SyncHistoryWrapper syncHistory, User defaultUser) {
            var project = gitLabWrapper.FindProject(gitRepoPath);
            if (project == null) {
                Log.Error($"Can`t find git project {gitRepoPath}");
                return CheckMergeChangesResult.Error;
            }

            var gitlabBranch = gitLabWrapper.GetBranches(project).Single(x => x.Name == branchName);
            if (gitlabBranch.Commit.Id.Equals(new Sha1(head.GitCommitSha))) {
                var commits = GenerateCommits(vcsWrapper, branch, syncHistory, false);
                if (commits.Count == 0) {
                    var mergeRequests = GetMergeRequests(gitLabWrapper, branchName, defaultUser.UserName, project);
                    if (!mergeRequests.Any()) {
                        Log.Message("Zero registered merge requests.");
                        return CheckMergeChangesResult.NoChanges;
                    }
                }
            }
            return CheckMergeChangesResult.HasChanges;
        }
        static string GetVcsSyncHistory(DXVcsWrapper vcsWrapper, string historyPath) {
            string local = Path.GetTempFileName();
            return vcsWrapper.GetFile(historyPath, local);
        }
        static void EnsureGitDir(string localGitDir) {
            if (Directory.Exists(localGitDir)) {
                KillProcess("tgitcache");
                DirectoryHelper.DeleteDirectory(localGitDir);
            }
        }
        static void KillProcess(string process) {
            Process[] procs = Process.GetProcessesByName(process);

            foreach (Process proc in procs) {
                proc.Kill();
                proc.WaitForExit();
            }
        }
        static GitWrapper CreateGitWrapper(string gitRepoPath, string localGitDir, TrackBranch branch, string username, string password) {
            try {
                var gitWrapper = new GitWrapper(localGitDir, gitRepoPath, branch.Name, new GitCredentials { User = username, Password = password });
                Log.Message($"Branch {branch.Name} initialized.");

                return gitWrapper;
            }
            catch (Exception e) {
                Log.Error("Git Wrapper was not created: " + e.Message, e);
                return null;
            }
        }
        static TrackBranch FindBranch(string branchName, string trackerPath, DXVcsWrapper vcsWrapper) {
            string localPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string configPath = Path.Combine(localPath, trackerPath);
            var branch = GetBranch(branchName, configPath, vcsWrapper);
            if (branch == null) {
                Log.Error($"Specified branch {branchName} not found in track file.");
                return null;
            }
            return branch;
        }
        static int ProcessMergeRequests(DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, GitLabWrapper gitLabWrapper, RegisteredUsers users, User defaultUser, string gitRepoPath, string localGitDir, string branchName, string tracker, SyncHistoryWrapper syncHistory, string userName) {
            var project = gitLabWrapper.FindProject(gitRepoPath);
            TrackBranch branch = GetBranch(branchName, tracker, vcsWrapper);
            if (branch == null) {
                Log.Error($"Specified branch {branchName} not found in track file.");
                return 1;
            }
            var mergeRequests = GetMergeRequests(gitLabWrapper, branchName, userName, project);
            if (!mergeRequests.Any()) {
                Log.Message("Zero registered merge requests.");
                return 0;
            }
            int result = 0;
            foreach (var mergeRequest in mergeRequests) {
                var mergeRequestResult = ProcessMergeRequest(vcsWrapper, gitWrapper, gitLabWrapper, users, defaultUser, localGitDir, branch, mergeRequest, syncHistory);
                if (mergeRequestResult == MergeRequestResult.Failed)
                    return 1;
                if (mergeRequestResult == MergeRequestResult.CheckoutFailed || mergeRequestResult == MergeRequestResult.Conflicts || mergeRequestResult == MergeRequestResult.InvalidState)
                    result = 1;
            }
            return result;
        }
        static List<MergeRequest> GetMergeRequests(GitLabWrapper gitLabWrapper, string branchName, string userName, Project project) {
            return gitLabWrapper.GetMergeRequests(project, x => x.TargetBranch == branchName).Where(x => x.Assignee?.Name == userName).ToList();
        }
        static void AssignBackConflictedMergeRequest(GitLabWrapper gitLabWrapper, RegisteredUsers users, MergeRequest mergeRequest, string comment) {
            User author = users.GetUser(mergeRequest.Author.Username);
            var mr = gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, author.UserName);
            gitLabWrapper.AddCommentToMergeRequest(mr, comment);
        }
        static bool ValidateMergeRequest(DXVcsWrapper vcsWrapper, TrackBranch branch, SyncHistoryItem previous, User defaultUser) {
            var history = vcsWrapper.GenerateHistory(branch, new DateTime(previous.VcsCommitTimeStamp)).Where(x => x.ActionDate.Ticks > previous.VcsCommitTimeStamp);
            if (history.Any(x => x.User != defaultUser.UserName))
                return false;
            return true;
        }
        static MergeRequestResult ProcessMergeRequest(DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, GitLabWrapper gitLabWrapper, RegisteredUsers users, User defaultUser, string localGitDir, TrackBranch branch, MergeRequest mergeRequest, SyncHistoryWrapper syncHistory) {
            switch (mergeRequest.State) {
                case "reopened":
                case "opened":
                    return ProcessOpenedMergeRequest(vcsWrapper, gitWrapper, gitLabWrapper, users, defaultUser, localGitDir, branch, mergeRequest, syncHistory);
            }
            return MergeRequestResult.InvalidState;
        }
        static MergeRequestResult ProcessOpenedMergeRequest(DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, GitLabWrapper gitLabWrapper, RegisteredUsers users, User defaultUser, string localGitDir, TrackBranch branch, MergeRequest mergeRequest, SyncHistoryWrapper syncHistory) {
            string autoSyncToken = syncHistory.CreateNewToken();
            var lastHistoryItem = syncHistory.GetHead();

            Log.Message($"Start merging mergerequest {mergeRequest.Title}");

            Log.ResetErrorsAccumulator();
            var changes = gitLabWrapper.GetMergeRequestChanges(mergeRequest).ToList();
            if (changes.Count >= MaxChangesCount) {
                Log.Error($"Merge request contains more than {MaxChangesCount} changes and cannot be processed. Split it into smaller merge requests");
                AssignBackConflictedMergeRequest(gitLabWrapper, users, mergeRequest, CalcCommentForFailedCheckoutMergeRequest(null));
                return MergeRequestResult.Failed;
            }
            var genericChange = changes
                .Where(x => branch.TrackItems.FirstOrDefault(track => {
                    var root = x.OldPath.Split(new[] { @"\", @"/" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    return root == track.ProjectPath;
                }) != null)
                .Select(x => ProcessMergeRequestChanges(mergeRequest, x, localGitDir, branch, autoSyncToken)).ToList();
            bool ignoreValidation = gitLabWrapper.ShouldIgnoreSharedFiles(mergeRequest);

            if (!ValidateMergeRequestChanges(gitLabWrapper, mergeRequest, ignoreValidation) || !vcsWrapper.ProcessCheckout(genericChange, ignoreValidation, branch)) {
                Log.Error("Merging merge request failed because failed validation.");
                AssignBackConflictedMergeRequest(gitLabWrapper, users, mergeRequest, CalcCommentForFailedCheckoutMergeRequest(genericChange));
                vcsWrapper.ProcessUndoCheckout(genericChange);
                return MergeRequestResult.CheckoutFailed;
            }
            CommentWrapper comment = CalcComment(mergeRequest, branch, autoSyncToken);
            mergeRequest = gitLabWrapper.ProcessMergeRequest(mergeRequest, comment.ToString());
            if (mergeRequest.State == "merged") {
                Log.Message("Merge request merged successfully.");

                gitWrapper.Pull();
                gitWrapper.LFSPull();

                var gitCommit = gitWrapper.FindCommit(x => CommentWrapper.Parse(x.Message).Token == autoSyncToken);
                long timeStamp = lastHistoryItem.VcsCommitTimeStamp;

                if (gitCommit != null && vcsWrapper.ProcessCheckIn(genericChange, comment.ToString())) {
                    var checkinHistory = vcsWrapper.GenerateHistory(branch, new DateTime(timeStamp)).Where(x => x.ActionDate.Ticks > timeStamp);
                    var lastCommit = checkinHistory.OrderBy(x => x.ActionDate).LastOrDefault();
                    long newTimeStamp = lastCommit?.ActionDate.Ticks ?? timeStamp;
                    var mergeRequestResult = MergeRequestResult.Success;
                    if (!ValidateMergeRequest(vcsWrapper, branch, lastHistoryItem, defaultUser))
                        mergeRequestResult = MergeRequestResult.Mixed;
                    if (!ValidateChangeSet(genericChange))
                        mergeRequestResult = MergeRequestResult.Mixed;
                    syncHistory.Add(gitCommit.Sha, newTimeStamp, autoSyncToken, mergeRequestResult == MergeRequestResult.Success ? SyncHistoryStatus.Success : SyncHistoryStatus.Mixed);
                    syncHistory.Save();
                    Log.Message("Merge request checkin successfully.");
                    return mergeRequestResult;
                }
                Log.Error("Merge request checkin failed.");
                if (gitCommit == null)
                    Log.Error($"Can`t find git commit with token {autoSyncToken}");
                var failedHistory = vcsWrapper.GenerateHistory(branch, new DateTime(timeStamp));
                var lastFailedCommit = failedHistory.OrderBy(x => x.ActionDate).LastOrDefault();
                syncHistory.Add(gitCommit.Sha, lastFailedCommit?.ActionDate.Ticks ?? timeStamp, autoSyncToken, SyncHistoryStatus.Failed);
                syncHistory.Save();
                return MergeRequestResult.Failed;
            }
            Log.Message($"Merge request merging failed due conflicts. Resolve conflicts manually.");
            vcsWrapper.ProcessUndoCheckout(genericChange);
            AssignBackConflictedMergeRequest(gitLabWrapper, users, mergeRequest, CalcCommentForFailedCheckoutMergeRequest(genericChange));

            return MergeRequestResult.Conflicts;
        }
        static bool ValidateChangeSet(List<SyncItem> genericChangeSet) {
            return !genericChangeSet.Any(x => x.SharedFile);
        }
        static bool ValidateMergeRequestChanges(GitLabWrapper gitLabWrapper, MergeRequest mergeRequest, bool ignoreValidation) {
            if (ignoreValidation)
                return true;

            bool result = true;
            var fileChanges = gitLabWrapper.GetFileChanges(mergeRequest);
            foreach (var fileChange in fileChanges) {
                if (!ValidateFileChange(fileChange)) {
                    Log.Error($"File {fileChange.OldPath} has nonwindows line endings. Only windows line endings allowed.");
                    result = false;
                }
            }
            return result;
        }
        static Regex NewlinePattern { get; } = new Regex(@"^[+][^+?]", RegexOptions.Compiled);
        static HashSet<string> CheckFilesList { get; } = new HashSet<string> { ".cs", ".xaml", ".csproj", ".sln", ".tt", ".resx" };
        static bool ValidateFileChange(MergeRequestFileData diff) {
            if (!CheckFilesList.Contains(Path.GetExtension(diff.OldPath)))
                return true;
            var fixeol = diff.Diff.Replace("\n\\ No newline at end of file\n", Environment.NewLine);
            var chunks = fixeol.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return chunks.Where(x => NewlinePattern.IsMatch(x)).Select(chunk => chunk.ToCharArray()).All(charArray => charArray.LastOrDefault() == '\r');
        }
        static string CalcCommentForFailedCheckoutMergeRequest(List<SyncItem> genericChange) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Merge request has been assigned back because of changed validation.");
            sb.AppendLine(Log.GetErrorsAccumulatorContent());
            return sb.ToString();
        }
        static SyncItem ProcessMergeRequestChanges(MergeRequest mergeRequest, MergeRequestFileData fileData, string localGitDir, TrackBranch branch, string token) {
            string vcsRoot = branch.RepoRoot;
            var syncItem = new SyncItem();
            if (fileData.IsNew) {
                syncItem.SyncAction = SyncAction.New;
                syncItem.LocalPath = CalcLocalPath(localGitDir, branch, fileData.OldPath);
                syncItem.VcsPath = CalcVcsPath(vcsRoot, branch, fileData.OldPath);
            }
            else if (fileData.IsDeleted) {
                syncItem.SyncAction = SyncAction.Delete;
                syncItem.LocalPath = CalcLocalPath(localGitDir, branch, fileData.OldPath);
                syncItem.VcsPath = CalcVcsPath(vcsRoot, branch, fileData.OldPath);
            }
            else if (fileData.IsRenamed) {
                syncItem.SyncAction = SyncAction.Move;
                syncItem.LocalPath = CalcLocalPath(localGitDir, branch, fileData.OldPath);
                syncItem.NewLocalPath = CalcLocalPath(localGitDir, branch, fileData.NewPath);
                syncItem.VcsPath = CalcVcsPath(vcsRoot, branch, fileData.OldPath);
                syncItem.NewVcsPath = CalcVcsPath(vcsRoot, branch, fileData.NewPath);
            }
            else {
                syncItem.SyncAction = SyncAction.Modify;
                syncItem.LocalPath = CalcLocalPath(localGitDir, branch, fileData.OldPath);
                syncItem.VcsPath = CalcVcsPath(vcsRoot, branch, fileData.OldPath);
            }
            syncItem.Comment = CalcComment(mergeRequest, branch, token);
            return syncItem;
        }
        static string CalcLocalPath(string localGitDir, TrackBranch branch, string path) {
            return Path.Combine(localGitDir, path);
        }
        static string CalcVcsPath(string vcsRoot, TrackBranch branch, string path) {
            var root = path.Split(new[] { @"\", @"/" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            var trackItem = branch.TrackItems.First(x => root == x.ProjectPath);
            var resultPath = path.Remove(0, trackItem.ProjectPath.Length).TrimStart(@"\/".ToCharArray());
            string trackPath = branch.GetTrackRoot(trackItem);
            return Path.Combine(trackPath, resultPath).Replace("\\", "/");
        }
        static ProcessHistoryResult ProcessHistory(DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, RegisteredUsers users, User defaultUser, string gitRepoPath, string localGitDir, TrackBranch branch, int commitsCount, SyncHistoryWrapper syncHistory, bool mergeCommits) {
            IList<CommitItem> commits = GenerateCommits(vcsWrapper, branch, syncHistory, mergeCommits);

            if (commits.Count > commitsCount) {
                Log.Message($"Commits generated. First {commitsCount} of {commits.Count} commits taken.");
                commits = commits.Take(commitsCount).ToList();
            }
            else {
                Log.Message($"Commits generated. {commits.Count} commits taken.");
            }
            if (commits.Count > 0)
                ProcessHistoryInternal(vcsWrapper, gitWrapper, users, defaultUser, localGitDir, branch, commits, syncHistory);
            Log.Message($"Importing history from vcs completed.");

            return commits.Count > commitsCount ? ProcessHistoryResult.NotEnough : ProcessHistoryResult.Success;
        }
        static IList<CommitItem> GenerateCommits(DXVcsWrapper vcsWrapper, TrackBranch branch, SyncHistoryWrapper syncHistory, bool mergeCommits) {
            DateTime lastCommit = CalcLastCommitDate(syncHistory);
            Log.Message($"Last commit has been performed at {lastCommit.ToLocalTime()}.");

            var history = vcsWrapper.GenerateHistory(branch, lastCommit).OrderBy(x => x.ActionDate).ToList();
            Log.Message($"History generated. {history.Count} history items obtained.");

            IList<CommitItem> commits = vcsWrapper.GenerateCommits(history).Where(x => x.TimeStamp > lastCommit && !IsLabel(x)).ToList();
            if (mergeCommits)
                commits = vcsWrapper.MergeCommits(commits);
            return commits;
        }
        static DateTime CalcLastCommitDate(SyncHistoryWrapper syncHistory) {
            var head = syncHistory.GetHistoryHead();
            return new DateTime(head.VcsCommitTimeStamp);
        }
        static void ProcessHistoryInternal(DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, RegisteredUsers users, User defaultUser, string localGitDir, TrackBranch branch, IList<CommitItem> commits, SyncHistoryWrapper syncHistory) {
            ProjectExtractor extractor = new ProjectExtractor(commits, (item) => {
                var localCommits = vcsWrapper.GetCommits(item.TimeStamp, item.Items).Where(x => !IsLabel(x)).ToList();
                bool hasModifications = false;
                GitCommit last = null;
                string token = syncHistory.CreateNewToken();
                foreach (var localCommit in localCommits) {
                    string localProjectPath = Path.Combine(localGitDir, localCommit.Track.ProjectPath);
                    DirectoryHelper.DeleteDirectory(localProjectPath);
                    string trackPath = branch.GetTrackRoot(localCommit.Track);
                    vcsWrapper.GetProject(vcsServer, trackPath, localProjectPath, item.TimeStamp);

                    Log.Message($"git stage {localCommit.Track.ProjectPath}");
                    gitWrapper.Stage(localCommit.Track.ProjectPath);
                    string author = CalcAuthor(localCommit, defaultUser);
                    var comment = CalcComment(localCommit, author, token);
                    User user = users.GetUser(author);
                    try {
                        gitWrapper.Commit(comment.ToString(), user, localCommit.TimeStamp, false);
                        last = gitWrapper.FindCommit(x => true);
                        hasModifications = true;
                    }
                    catch (Exception) {
                        Log.Message($"Empty commit detected for {localCommit.Author} {localCommit.TimeStamp}.");
                    }
                }
                if (hasModifications) {
                    gitWrapper.PushEverything();
                    syncHistory.Add(last.Sha, item.TimeStamp.Ticks, token);
                }
                else {
                    var head = syncHistory.GetHead();
                    syncHistory.Add(head.GitCommitSha, item.TimeStamp.Ticks, token);
                    string author = CalcAuthor(item, defaultUser);
                    Log.Message($"Push empty commits rejected for {author} {item.TimeStamp}.");
                }
                syncHistory.Save();
            });
            int i = 0;
            while (extractor.PerformExtraction())
                Log.Message($"{++i} from {commits.Count} push to branch {branch.Name} completed.");
        }
        static string CalcAuthor(CommitItem localCommit, User defaultUser) {
            string author = localCommit.Author;
            if (string.IsNullOrEmpty(author))
                author = localCommit.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.User))?.User;
            if (!IsServiceUser(author, defaultUser.UserName))
                return author;
            var comment = localCommit.Items.FirstOrDefault(x => CommentWrapper.IsAutoSyncComment(x.Comment));
            if (comment == null)
                return author;
            var commentWrapper = CommentWrapper.Parse(comment.Comment);
            return commentWrapper.Author;
        }
        static bool IsServiceUser(string author, string defaultUser) {
            if (author == defaultUser)
                return true;
            return author?.StartsWith("dxvcs2git") ?? true;
        }

        static TrackBranch GetBranch(string branchName, string configPath, DXVcsWrapper vcsWrapper) {
            try {
                var branches = TrackBranch.Deserialize(configPath, vcsWrapper);
                return branches.First(x => x.Name == branchName);
            }
            catch (Exception ex) {
                Log.Error("Loading items for track failed", ex);
                return null;
            }
        }
        static bool IsLabel(CommitItem item) {
            return item.Items.Any(x => !string.IsNullOrEmpty(x.Label));
        }
        static CommentWrapper CalcComment(MergeRequest mergeRequest, TrackBranch branch, string autoSyncToken) {
            CommentWrapper comment = new CommentWrapper();
            comment.Author = mergeRequest.Author.Username;
            comment.Branch = branch.Name;
            comment.Token = autoSyncToken;
            comment.Comment = CalcCommentForMergeRequest(mergeRequest);
            return comment;
        }
        static string CalcCommentForMergeRequest(MergeRequest mergeRequest) {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(mergeRequest.Title))
                sb.AppendLine(mergeRequest.Title);
            if (!string.IsNullOrEmpty(mergeRequest.Description)) {
                sb.Append(mergeRequest.Description);
                sb.AppendLine();
            }
            return sb.ToString();
        }
        static CommentWrapper CalcComment(CommitItem item, string author, string token) {
            CommentWrapper comment = new CommentWrapper();
            comment.TimeStamp = item.TimeStamp.Ticks.ToString();
            comment.Author = author;
            comment.Branch = item.Track.Branch;
            comment.Token = token;
            if (item.Items.Any(x => !string.IsNullOrEmpty(x.Comment) && CommentWrapper.IsAutoSyncComment(x.Comment)))
                comment.Comment = item.Items.Select(x => CommentWrapper.Parse(x.Comment ?? x.Message).Comment).FirstOrDefault(x => !string.IsNullOrEmpty(x));
            else
                comment.Comment = item.Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.Comment))?.Comment;
            return comment;
        }
    }
    public enum MergeRequestResult {
        Success,
        Failed,
        Conflicts,
        CheckoutFailed,
        Mixed,
        InvalidState,
    }
    public enum ProcessHistoryResult {
        Success,
        Failed,
        NotEnough,
    }

    public enum CheckMergeChangesResult {
        NoChanges,
        Error,
        HasChanges,
    }

    public class PatchItem {
        public SyncAction SyncAction { get; set; }
        public string OldPath { get; set; }
        public string NewPath { get; set; }
    }

    public class PatchInfo {
        public long TimeStamp { get; set; }
        public List<PatchItem> Items { get; set; }
    }
}