using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommandLine;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Farm;
using DXVcs2Git.Core.GitLab;
using DXVcs2Git.Core.Serialization;
using DXVcs2Git.DXVcs;
using DXVcs2Git.Git;
using DXVcs2Git.UI.Farm;
using Newtonsoft.Json;
using NGitLab;
using NGitLab.Models;
using Nito.AsyncEx;
using RestSharp;
using ProjectHookType = DXVcs2Git.Core.GitLab.ProjectHookType;
using User = DXVcs2Git.Core.User;

namespace DXVcs2Git.Console {
    internal class Program {
        static IPAddress ipAddress;
        static IPAddress IP => ipAddress ?? (ipAddress = DetectMyIP());
        static IPAddress DetectMyIP() {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList.LastOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        const string repoPath = "repo";
        const string patchZip = "patch.zip";
        const string patchInfoName = "patch.info";
        const string vcsServer = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService";
        const string farmServer = @"tcp://ccnet.devexpress.devx:21234/CruiseManager.rem";
        public const string DXUpdateConnectionString = @"data source=xpf-anallytics;user id=DXUpdate;password=xVT2WkWi;MultipleActiveResultSets=True";
        const int MaxChangesCount = 1000;
        static async Task<int> Main(string[] args) {
            var result = Parser.Default.ParseArguments<SyncOptions, PatchOptions, ApplyPatchOptions, ListenerOptions, ProcessTestsOptions, DXUpdateOptions>(args);
            try {
                var exitCode = await result.MapResult(
                    async (SyncOptions syncOptions) => await DoSyncWork(syncOptions),
                    async (PatchOptions patchOptions) => await DoPatchWork(patchOptions),
                    async (ApplyPatchOptions applypatch) => await DoApplyPatchWork(applypatch),
                    async (ListenerOptions listenerOptions) => await DoListenerWork(listenerOptions),
                    async (ProcessTestsOptions testOptions) => await DoProcessTestResultsWork(testOptions),
                    err => Task.FromResult(DoErrors(args)));
                Environment.Exit(exitCode);
            }
            catch (Exception ex) {
                Log.Error("Application crashed with exception", ex);
                Environment.Exit(1);
            }

            return 0;
        }
        static int DoErrors(string[] args) {
            return 1;
        }
        static Task<int> DoApplyPatchWork(ApplyPatchOptions applypatch) {
            string localDir = applypatch.LocalFolder != null && Path.IsPathRooted(applypatch.LocalFolder) ? applypatch.LocalFolder : Path.Combine(Environment.CurrentDirectory, applypatch.LocalFolder ?? repoPath);
            if (!Directory.Exists(localDir)) {
                Log.Error($"Sources directory {localDir} was not found.");
                return Task.FromResult(1);
            }

            string patchPath = applypatch.Patch != null && Path.IsPathRooted(applypatch.Patch) ? applypatch.Patch : Path.Combine(Environment.CurrentDirectory, applypatch.Patch ?? patchZip);
            if (!File.Exists(patchPath)) {
                Log.Error($"{patchZip} was not found at {patchPath} location.");
                return Task.FromResult(1);

            }

            using (Package zip = Package.Open(patchPath, FileMode.Open)) {
                var patchInfoUri = PackUriHelper.CreatePartUri(new Uri(patchInfoName, UriKind.Relative));
                PatchInfo patchInfo = null;
                try {
                    var patchInfoPart = zip.GetPart(patchInfoUri);
                    patchInfo = Serializer.Deserialize<PatchInfo>(patchInfoPart.GetStream());
                }
                catch (Exception ex) {
                    Log.Error("Patch.info doesn`t found.", ex);
                    return Task.FromResult(1);
                }
                foreach (var patchItem in patchInfo.Items) {
                    if (patchItem.SyncAction == SyncAction.Delete) {
                        DeleteFile(localDir, patchItem);
                        continue;
                    }
                    if (patchItem.SyncAction == SyncAction.Modify) {
                        RewriteFile(patchItem, localDir, zip);
                        continue;
                    }
                    if (patchItem.SyncAction == SyncAction.Move) {
                        DeleteFile(localDir, patchItem);
                        RewriteFile(patchItem, localDir, zip);
                    }
                    if (patchItem.SyncAction == SyncAction.New) {
                        RewriteFile(patchItem, localDir, zip);
                        continue;
                    }
                    throw new Exception("syncAction");
                }

            }
            return Task.FromResult(0);
        }
        static void RewriteFile(PatchItem patchItem, string localDir, Package zip) {
            var uri = PackUriHelper.CreatePartUri(new Uri(patchItem.OldPath, UriKind.Relative));
            string path = Path.Combine(localDir, patchItem.OldPath);
            var patchItemPart = zip.GetPart(uri);
            using (var fileStream = File.OpenWrite(path)) {
                using (StreamWriter sw = new StreamWriter(fileStream)) {
                    sw.Write(patchItemPart.GetStream());
                }
            }
        }
        static void DeleteFile(string localDir, PatchItem patchItem) {
            string path = Path.Combine(localDir, patchItem.OldPath);
            File.Delete(path);
        }
        static Task<int> DoProcessTestResultsWork(ProcessTestsOptions clo) {
            Log.Message("process test results.");
            string targetRepoPath = GetSimpleGitHttpPath(clo.Repo);

            if (string.IsNullOrEmpty(targetRepoPath)) {
                Log.Error($"Can`t parse repo path {clo.Repo}");
                return Task.FromResult(1);
            }

            string sourceRepoPath = GetSimpleGitHttpPath(clo.SourceRepo);
            if (string.IsNullOrEmpty(sourceRepoPath)) {
                Log.Error($"Can`t parse source repo path {clo.SourceRepo}");
                return Task.FromResult(1);
            }

            string username = clo.Login;
            string gitlabauthtoken = clo.AuthToken;
            string targetBranchName = clo.Branch;
            string gitServer = clo.Server;
            string sourceBranchName = clo.SourceBranch;
            bool testIntegration = !clo.Individual;
            string jobId = clo.JobId;
            string commitSha = clo.Commit;

            GitLabWrapper gitLabWrapper = new GitLabWrapper(gitServer, gitlabauthtoken);

            Project targetProject = gitLabWrapper.FindProject(targetRepoPath);
            if (targetProject == null) {
                Log.Error($"Can`t find target project {targetRepoPath}.");
                return Task.FromResult(1);

            }
            Branch targetBranch = gitLabWrapper.GetBranch(targetProject, targetBranchName);
            if (targetBranch == null) {
                Log.Error($"Can`t find targetBranch branch {targetBranchName}");
                return Task.FromResult(1);
            }

            var sourceProject = gitLabWrapper.FindProjectFromAll(sourceRepoPath);
            if (sourceProject == null) {
                Log.Error($"Can`t find source project {sourceRepoPath}");
                return Task.FromResult(1);
            }

            var sourceBranch = gitLabWrapper.GetBranch(sourceProject, sourceBranchName);
            if (sourceBranch == null) {
                Log.Error($"Source branch {sourceBranchName} was not found.");
                return Task.FromResult(1);
            }

            MergeRequest mergeRequest = gitLabWrapper.GetMergeRequests(targetProject, x => x.SourceBranch == sourceBranchName && x.TargetBranch == targetBranchName && x.SourceProjectId == sourceProject.Id).FirstOrDefault();
            if (mergeRequest == null) {
                if (!testIntegration) {
                    if (clo.Result == 0)
                        Log.Message($@"Pipeline passed. http://asp-git:8181?source_id={sourceProject.Id}&path={Uri.EscapeDataString(targetProject.PathWithNamespace)}&build={jobId}");
                    else
                        Log.Message($@"Pipeline failed. http://asp-git:8181?source_id={sourceProject.Id}&path={Uri.EscapeDataString(targetProject.PathWithNamespace)}&build={jobId}");
                    return Task.FromResult(clo.Result);
                }
                Log.Error($"Can`t find merge request.");
                return Task.FromResult(1);
            }

            Log.Message($"Merge request id: {mergeRequest.Iid}.");
            Log.Message($"Merge request title: {mergeRequest.Title}.");
            Log.Message($"Merge request assignee: {mergeRequest.Assignee?.Name ?? "None"}.");

            string pipeline = $@"Pipeline {(clo.Result == 0 ? "passed." : "failed")} http://asp-git:8181?source_id={sourceProject.Id}&path={Uri.EscapeDataString(targetProject.PathWithNamespace)}&mergerequest_id={mergeRequest.Iid}&build={jobId}";
            Log.Message(pipeline);

            var commit = gitLabWrapper.GetMergeRequestCommits(mergeRequest).FirstOrDefault();
            if (commit == null) {
                Log.Message("Merge request has no commits.");
                return Task.FromResult(0);
            }

            gitLabWrapper.AddCommentToMergeRequest(mergeRequest, pipeline);
            if (clo.Result == 0) {
                if (mergeRequest.WorkInProgress ?? false) {
                    Log.Message("Work in progress. Assign on test service skipped.");
                    return Task.FromResult(0);
                }

                var xmlComments = gitLabWrapper.GetComments(mergeRequest).Where(x => IsXml(x.Note));
                var options = xmlComments.Select(x => MergeRequestOptions.ConvertFromString(x.Note)).FirstOrDefault();
                if (options != null && options.ActionType == MergeRequestActionType.sync) {
                    var action = (MergeRequestSyncAction)options.Action;
                    if (action.AssignToSyncService && !string.IsNullOrEmpty(action.SyncService) && !string.IsNullOrEmpty(commitSha)) {
                        if (commit.Id.Equals(new Sha1(commitSha))) {
                            gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, action.SyncService);
                            Log.Message("Auto sync performed by gittools config.");
                        }
                        else {
                            Log.Message("Auto sync by gittools config rejected because testing commit is not head.");
                        }
                    }
                }

                var autoAssigneeUser = GetAutoAssigneeUser(targetProject) ?? GetAutoAssigneeUser(sourceProject);
                if (autoAssigneeUser != null) {
                    gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, autoAssigneeUser);
                    Log.Message("Auto sync performed by repo tag");
                }
            }
            return Task.FromResult(0);
        }

        const string AutoAssigneeTagPrefix = "autoAssigneeUser=";
        static string GetAutoAssigneeUser(Project project) {
            return project.Tags?
                .FirstOrDefault(x => x.StartsWith(AutoAssigneeTagPrefix))?
                .Substring(AutoAssigneeTagPrefix.Length);
        }

        static async Task<int> DoPatchWork(PatchOptions clo) {
            string localGitDir = clo.LocalFolder != null && Path.IsPathRooted(clo.LocalFolder) ? clo.LocalFolder : Path.Combine(Environment.CurrentDirectory, clo.LocalFolder ?? repoPath);

            string targetRepoPath = GetSimpleGitHttpPath(clo.Repo);

            if (string.IsNullOrEmpty(targetRepoPath)) {
                Log.Error($"Can`t parse repo path {clo.Repo}");
                return 1;
            }
            string sourceRepoPath = GetSimpleGitHttpPath(clo.SourceRepo);
            if (string.IsNullOrEmpty(sourceRepoPath)) {
                Log.Error($"Can`t parse source repo path {clo.SourceRepo}");
                return 1;
            }

            string username = clo.Login;
            string password = clo.Password;
            string gitlabauthtoken = clo.AuthToken;
            string targetBranchName = clo.Branch;
            string trackerPath = clo.Tracker;
            string gitServer = clo.Server;
            string sourceBranchName = clo.SourceBranch;
            string patchdir = clo.PatchDir ?? localGitDir;
            bool testIntegration = !clo.Individual;
            string commitSha = clo.Commit;
            string sourceRepo = clo.SourceRepo;
            bool usePatchService = clo.UsePatchService;
            string patchServiceUrl = clo.PatchServiceUrl;

            DXVcsWrapper vcsWrapper = new DXVcsWrapper(vcsServer, username, password);

            TrackBranch trackBranch = FindBranch(targetBranchName, trackerPath, vcsWrapper);
            if (trackBranch == null) {
                return 1;
            }

            string historyPath = GetVcsSyncHistory(vcsWrapper, trackBranch.HistoryPath);
            if (historyPath == null)
                return 1;
            SyncHistory history = SyncHistory.Deserialize(historyPath);
            if (history == null)
                return 1;

            GitLabWrapper gitLabWrapper = new GitLabWrapper(gitServer, gitlabauthtoken);

            Project targetProject = gitLabWrapper.FindProject(targetRepoPath);
            if (targetProject == null) {
                Log.Error($"Can`t find target project {targetRepoPath}.");
                return 1;
            }
            Log.Message($"Target project url: {targetProject.HttpUrl}");

            Branch targetBranch = gitLabWrapper.GetBranch(targetProject, targetBranchName);
            if (targetBranch == null) {
                Log.Error($"Can`t find targetBranch branch {targetBranchName}");
                return 1;
            }
            Log.Message($"Target branch name: {targetBranch.Name}");

            var sourceProject = gitLabWrapper.FindProjectFromAll(sourceRepoPath);
            if (sourceProject == null) {
                Log.Error($"Can`t find source project {sourceRepoPath}");
                return 1;
            }
            Log.Message($"Source project url: {sourceProject.HttpUrl}");

            var sourceBranch = gitLabWrapper.GetBranch(sourceProject, sourceBranchName);
            if (sourceBranch == null) {
                Log.Error($"Source branch {sourceBranchName} was not found.");
                return 1;
            }
            Log.Message($"Source branch name: {sourceBranch.Name}");

            MergeRequest mergeRequest = gitLabWrapper.GetMergeRequests(targetProject,
                x => x.SourceBranch == sourceBranchName && x.TargetBranch == targetBranchName && x.SourceProjectId == sourceProject.Id).FirstOrDefault();

            //bool shouldTestIntegration = testIntegration || mergeRequest != null && mergeRequest.Assignee?.Name == username;

//            if (shouldTestIntegration) {
                if (mergeRequest == null) {
                    Log.Error($"Can`t find merge request.");
                    return 1;
                }
                Log.Message($"Merge request id: {mergeRequest.Iid}.");
                Log.Message($"Merge request title: {mergeRequest.Title}.");

                if (mergeRequest.Assignee?.Name != username) {
                    Log.Error($"Merge request is not assigned to service user {username} or doesn`t require testing.");
                    return 1;
                }
                Log.Message($"Merge request assignee: {mergeRequest.Assignee?.Name ?? "None"}.");

                var changes = GetMergeRequestChanges(gitLabWrapper, mergeRequest, trackBranch);

                var patch = new PatchInfo() { TimeStamp = DateTime.Now.Ticks, Items = changes };
                var result = await GeneratePatch(patchdir, patch, usePatchService, patchServiceUrl, localGitDir, sourceProject.SshUrl, sourceBranch.Name, commitSha);
                return result ? 0 : 1;
//            }
//            else {
//                if (mergeRequest != null) {
//                    Log.Message($"Merge request id: {mergeRequest.Iid}.");
//                    Log.Message($"Merge request title: {mergeRequest.Title}.");
//                }
//                else
//                    Log.Message($"Merge request not found. Testing by commits.");
//
//                Sha1 searchSha = mergeRequest == null ? new Sha1(commitSha) : gitLabWrapper.GetMergeRequestCommits(mergeRequest).LastOrDefault()?.Id ?? new Sha1(commitSha);
//                var commit = FindSyncCommit(gitLabWrapper, sourceProject, searchSha);
//                if (commit == null) {
//                    Log.Error($"Can`t find sync commit. Try to merge with latest version.");
//                    return 1;
//                }
//                long? vcsCommitTimeStamp = FindVcsCommitTimeStamp(commit, vcsWrapper, history, historyPath, trackBranch.HistoryPath);
//                if (vcsCommitTimeStamp == null) {
//                    Log.Error($"Can`t find vcs sync commit. Try to merge with latest version.");
//                    return 1;
//                }
//
//
//                Log.Message($"Patch based on commit {commit.Id}");
//                Log.Message($"Vcs commit timestamp {new DateTime(vcsCommitTimeStamp.Value).ToLocalTime()}");
//                var changes = GetCommitChanges(sourceProject, commit.Id.ToString(), commitSha, trackBranch);
//                var patch = new PatchInfo() { TimeStamp = vcsCommitTimeStamp.Value, Items = changes };
//                GeneratePatch(patchdir, patch, usePatchService, patchServiceUrl, localGitDir, sourceProject.Path, sourceBranch.Name, searchSha.ToString());
//                return 0;
//            }
        }
        static async Task<bool> GeneratePatch(string patchdir, PatchInfo patch, bool usePatchService, string patchServiceUrl, string localGitDir, string repo, string branch, string sha1) {
            if (usePatchService) {
                return await GeneratePatchRemote(patchdir, patch, patchServiceUrl, repo, branch, sha1);
            }

            return GeneratePatchLocal(patchdir, patch, localGitDir);

        }
        static async Task<bool> GeneratePatchRemote(string patchdir, PatchInfo patch, string patchServiceUrl, string repo, string branch, string sha1) {
            Log.Message($"Start generating patch by remote service.");

            string info = SavePatchInfo(patchdir, patch);
            string content = File.ReadAllText(Path.Combine(patchdir, info));
            
            var client = new RestClient(patchServiceUrl);
            var request = new RestRequest($@"/api/v1/createpatch/{sha1}", Method.POST);
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("repo", repo);
            values.Add("branch", branch);
            values.Add("content", content);
            
            string json = JsonConvert.SerializeObject(values);
            
            request.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            var response = await client.ExecuteTaskAsync(request);
            var waitPatchTask = WaitPatch(response, client, sha1);
            TimeSpan timeout = TimeSpan.FromSeconds(600);

            try {
                using (var cts = new CancellationTokenSource(timeout)) {
                    var link = await waitPatchTask.WaitAsync(cts.Token);
                    if (string.IsNullOrEmpty(link)) {
                        Log.Message($"Patch.info generation failed");
                        return false;
                    }

                    var patchPath = Path.Combine(patchdir, patchZip);

                    File.Copy(link, patchPath, true);
                    Log.Message($"Patch.info generated at {patchPath}");
                    return true;
                }
            }
            catch (TaskCanceledException) {
                Log.Error("Get patch timeout.");
                return false;
            }
        }
        enum ChunkStatus {
            NotRunning,
            Running,
            Success,
            Failed,
        }
        static async Task<string> WaitPatch(IRestResponse response, RestClient client, string sha1) {
            if (response.ResponseStatus == ResponseStatus.Completed) {
                while (true) {
                    var request = new RestRequest($"/api/v1/getpatch/{sha1}");
                    
                    var getPatchResponse = await client.ExecuteGetTaskAsync(request);
                    if (getPatchResponse.ResponseStatus == ResponseStatus.Completed) {
                        var chunkStatus = getPatchResponse.Headers.FirstOrDefault(x => x.Name == "chunk_status");
                        if (chunkStatus != null && Enum.TryParse(chunkStatus.Value.ToString(), true, out ChunkStatus status)) {
                            if (status == ChunkStatus.Running || status == ChunkStatus.NotRunning) {
                                await Task.Delay(10000);
                                continue;
                            }

                            if (status == ChunkStatus.Failed) {
                                Log.Message($"Get patch returns error.");
                                var error = getPatchResponse.Headers.FirstOrDefault(x => x.Name == "error")?.Value?.ToString();
                                if (!string.IsNullOrEmpty(error))
                                    Log.Error(Base64Decode(error));
                                return null;
                            }
                            if (status == ChunkStatus.Success)
                                return getPatchResponse.Headers.FirstOrDefault(x => x.Name == "link")?.Value?.ToString();
                            throw new ArgumentException("status");
                        }
                    }
                    else {
                        Log.Message($"Get patch failed with {getPatchResponse.ResponseStatus}");
                        return null;
                    }
                }
            }

            Log.Message($"Create patch failed with {response.ResponseStatus}");

            return null;
        }
        static string Base64Decode(string base64EncodedData) {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        static bool GeneratePatchLocal(string patchdir, PatchInfo patch, string localGitDir) {
            Log.Message($"Start generating patch by local sources.");
            var patchPath = Path.Combine(patchdir, patchZip);
            using (var fileStream = new FileStream(patchPath, FileMode.CreateNew)) {
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true)) {
                    SavePatchInfo(patchdir, patch);
                    AddPart(archive, patchdir, "patch.info");
                    foreach (var path in CalcFilesForPatch(patch)) {
                        AddPart(archive, localGitDir, path);
                    }
                }
            }
            Log.Message($"Patch.info generated at {patchPath}");
            return true;
        }
        static List<PatchItem> GetCommitChanges(GitWrapper gitsWrapper, Project project, string from, string to, TrackBranch branch) {
            return gitsWrapper
                .Diff(from, to)
                .Select(x => {
                    var trackItem = CalcTrackItem(branch, x.OldPath);
                    var newTrackItem = trackItem;
                    if (x.Status == GitDiffStatus.Moved) {
                        newTrackItem = CalcTrackItem(branch, x.NewPath);
                    }
                    return new { trackItem = trackItem, newTrackItem = newTrackItem, item = x };
                })
                .Where(x => x.trackItem != null || x.newTrackItem != null)
                .Select(x => new PatchItem() {
                    SyncAction = CalcSyncAction(x.item),
                    OldPath = x.item.OldPath,
                    NewPath = x.item.NewPath,
                    OldVcsPath = CalcVcsPath(x.trackItem, x.item.OldPath),
                    NewVcsPath = CalcVcsPath(x.trackItem, x.item.NewPath),
                }).ToList();
        }
        static long? FindVcsCommitTimeStamp(Commit commit, DXVcsWrapper vcsWrapper, SyncHistory history, string localHistoryPath, string historyPath) {
            string token = CommentWrapper.Parse(commit.Title).Token;
            var item = history.Items.FirstOrDefault(x => new Sha1(x.GitCommitSha).Equals(commit.Id));
            if (item != null)
                return item.VcsCommitTimeStamp;

            DateTime commitTime = commit.CreatedAt;
            foreach (var fileHistoryInfo in vcsWrapper.GetFileHistory(historyPath, commitTime.ToLocalTime())) {
                vcsWrapper.GetFile(historyPath, localHistoryPath, fileHistoryInfo.ActionDate);
                SyncHistory currentHistory = SyncHistory.Deserialize(localHistoryPath);
                if (currentHistory == null) {
                    Log.Error($"Can`t parse current history file.");
                    break;
                }
                var historyItem = currentHistory.Items.FirstOrDefault(x => new Sha1(x.GitCommitSha).Equals(commit.Id));
                if (historyItem != null)
                    return historyItem.VcsCommitTimeStamp;
            }
            return null;
        }
        static Commit FindSyncCommit(GitLabWrapper gitLabWrapper, Project project, Sha1 commitSha) {
            return gitLabWrapper.FindParentCommit(project, commitSha, x => CommentWrapper.IsAutoSyncComment(x.Title));
        }
        static List<PatchItem> GetMergeRequestChanges(GitLabWrapper gitLabWrapper, MergeRequest mergeRequest, TrackBranch branch) {
            return gitLabWrapper
                .GetMergeRequestChanges(mergeRequest)
                .Select(x => {
                    var trackItem = CalcTrackItem(branch, x.OldPath);
                    var newTrackItem = trackItem;
                    if (x.IsRenamed) {
                        newTrackItem = CalcTrackItem(branch, x.NewPath);
                    }
                    return new { trackItem = trackItem, newTrackItem = newTrackItem, item = x };
                })
                .Where(x => x.trackItem != null || x.newTrackItem != null)
                .Select(x => new PatchItem() {
                    SyncAction = CalcSyncAction(x.item),
                    OldPath = x.item.OldPath,
                    NewPath = x.item.NewPath,
                    OldVcsPath = CalcVcsPath(x.trackItem, x.item.OldPath),
                    NewVcsPath = CalcVcsPath(x.newTrackItem, x.item.NewPath),
                }).ToList();
        }
        static MergeRequestSyncAction CalcMergeRequestSyncOptions(IEnumerable<Comment> comments) {
            if (comments == null)
                return null;
            var xmlBased = comments.Where(x => IsXml(x.Note)).Where(x => {
                var mr = MergeRequestOptions.ConvertFromString(x.Note);
                return mr?.ActionType == MergeRequestActionType.sync;
            }).Select(x => (MergeRequestSyncAction)MergeRequestOptions.ConvertFromString(x.Note).Action).LastOrDefault();

            if (xmlBased != null)
                return xmlBased;
            return null;
        }
        static readonly Regex SimpleHttpGitRegex = new Regex(@"http://[\w\._-]+/[\w\._-]+/[\w\._-]+.git", RegexOptions.Compiled);
        static readonly Regex GitlabciCheckRegex = new Regex(@"http://gitlab-ci-token:[\w\._-]+@(?<server>[\w\._-]+)/(?<nspace>[\w\._-]+)/(?<name>[\w\._-]+).git", RegexOptions.Compiled);
        static string GetSimpleGitHttpPath(string gitRepoPath) {
            if (string.IsNullOrEmpty(gitRepoPath)) {
                Log.Error("Git repo path is null or empty");
                return null;
            }
            if (SimpleHttpGitRegex.IsMatch(gitRepoPath))
                return gitRepoPath;
            var match = GitlabciCheckRegex.Match(gitRepoPath);
            if (!match.Success)
                return null;
            string server = match.Groups["server"].Value;
            string nspace = match.Groups["nspace"].Value;
            string name = match.Groups["name"].Value;
            return $"http://{server}/{nspace}/{name}.git";
        }
        static SyncAction CalcSyncAction(GitDiff fileData) {
            switch (fileData.Status) {
                case GitDiffStatus.Added:
                    return SyncAction.New;
                case GitDiffStatus.Modified:
                    return SyncAction.Modify;
                case GitDiffStatus.Moved:
                    return SyncAction.Move;
                case GitDiffStatus.Removed:
                    return SyncAction.Delete;
                default:
                    throw new Exception("GitDiffStatus");
            }
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
        static void AddPart(ZipArchive archive, string root, string path) {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (var zipStream = entry.Open()) {
                using (FileStream fileStream = new FileStream(Path.Combine(root, path), FileMode.Open, FileAccess.Read)) {
                    CopyStream(fileStream, zipStream);
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
        static IEnumerable<string> CalcFilesForPatch(PatchInfo patch) {
            var changes = patch.Items;
            foreach (var change in changes) {
                if (change.SyncAction == SyncAction.Delete)
                    continue;
                yield return change.SyncAction == SyncAction.Move ? change.NewPath : change.OldPath;
            }
        }
        static string SavePatchInfo(string rootPath, PatchInfo patch) {
            Serializer.Serialize(Path.Combine(rootPath, "patch.info"), patch);
            return "patch.info";
        }

        static Task<int> DoListenerWork(ListenerOptions clo) {
            string gitServer = clo.Server;
            string gitlabauthtoken = clo.AuthToken;
            string sharedWebHookPath = clo.WebHook;
            string taskName = clo.FarmTaskName;
            string serviceUser = clo.Login;
            bool supportsSendingMessages = !string.IsNullOrEmpty(taskName);
            Stopwatch sw = Stopwatch.StartNew();

            GitLabWrapper gitLabWrapper = new GitLabWrapper(gitServer, gitlabauthtoken);

            WebServer server = new WebServer(WebHookHelper.GetSharedHookUrl(IP, sharedWebHookPath));
            server.Start();
            while (sw.Elapsed.Minutes < clo.Timeout) {
                Thread.Sleep(10);
                var request = server.GetRequest();
                if (request == null)
                    continue;
                ProcessWebHook(gitLabWrapper, serviceUser, request, supportsSendingMessages, taskName);
            }

            return Task.FromResult(0);
        }
        static void ProcessWebHook(GitLabWrapper gitLabWrapper, string serviceUser, WebHookRequest request, bool supportSendingMessages, string farmTaskName) {
            var hookType = ProjectHookClient.ParseHookType(request);
            if (hookType == null)
                return;
            Log.Message($"Web hook received.");
            Log.Message($"Web hook type: {hookType.HookType}.");
            var hook = ProjectHookClient.ParseHook(hookType);
            if (hook.HookType == ProjectHookType.push)
                ProcessPushHook((PushHookClient)hook);
            else if (hook.HookType == ProjectHookType.merge_request)
                ProcessMergeRequestHook(gitLabWrapper, serviceUser, (MergeRequestHookClient)hook, supportSendingMessages, farmTaskName);
            else if (hook.HookType == ProjectHookType.build)
                ProcessBuildHook(gitLabWrapper, serviceUser, (BuildHookClient)hook, supportSendingMessages, farmTaskName);
        }
        static void ProcessBuildHook(GitLabWrapper gitLabWrapper, string serviceUser, BuildHookClient hook, bool supportSendingMessages, string farmTaskName) {
            Log.Message($"Build hook title: {hook.BuildName}");
            Log.Message($"Build hook status: {hook.Status}");

            if (supportSendingMessages)
                SendMessage(serviceUser, hook.Json, farmTaskName);

            if (hook.Status == PipelineStatus.success) {
                Project project = gitLabWrapper.GetProject(hook.ProjectId);
                if (project == null) {
                    Log.Message($"Can`t find project {hook.ProjectName}.");
                    return;
                }
                Log.Message($"Project: {project.PathWithNamespace}");
                var mergeRequest = CalcMergeRequest(gitLabWrapper, hook, project);
                if (mergeRequest == null) {
                    Log.Message("Can`t find merge request.");
                    return;
                }
                Log.Message($"Merge request: id = {mergeRequest.Iid} title = {mergeRequest.Title}");
                Log.Message($"Merge request state = {mergeRequest.State}");
                if (mergeRequest.State == MergeRequestState.opened || mergeRequest.State == MergeRequestState.reopened) {
                    var latestCommit = gitLabWrapper.GetMergeRequestCommits(mergeRequest).FirstOrDefault();
                    if (latestCommit == null) {
                        Log.Message("Wrong merge request found.");
                        return;
                    }
                    Log.Message($"Merge request latest commit sha = {latestCommit.Id}");
                    if (!latestCommit.Id.Equals(new Sha1(hook.Commit.Id))) {
                        Log.Message($"Additional commits has been added {hook.Commit.Id}");
                        return;
                    }

                    var xmlComments = gitLabWrapper.GetComments(mergeRequest).Where(x => IsXml(x.Note));
                    var options = xmlComments.Select(x => MergeRequestOptions.ConvertFromString(x.Note)).LastOrDefault();
                    if (options?.ActionType == MergeRequestActionType.sync) {
                        Log.Message("Sync options found.");
                        var syncOptions = (MergeRequestSyncAction)options.Action;
                        Log.Message($"Sync options perform testing is {syncOptions.TestIntegration}");
                        Log.Message($"Sync options assign to service is {syncOptions.AssignToSyncService}");
                        Log.Message($"Sync options sync task is {syncOptions.SyncTask}");
                        Log.Message($"Sync options sync service is {syncOptions.SyncService}");
                        if (syncOptions.TestIntegration && syncOptions.AssignToSyncService) {
                            gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, syncOptions.SyncService);
                            ForceBuild(syncOptions.SyncTask);
                        }
                        return;
                    }
                    var syncService = project.Tags?.FirstOrDefault(x => IsServiceUser(x, null));
                    if (!string.IsNullOrEmpty(syncService)) {
                        Log.Message($"Found sync service from project tag: {syncService}");
                        gitLabWrapper.UpdateMergeRequestAssignee(mergeRequest, syncService);
                        return;
                    }
                    Log.Message("Sync options not found.");
                }
            }
        }
        static MergeRequest CalcMergeRequest(GitLabWrapper gitLabWrapper, BuildHookClient hook, Project project) {
            foreach (var checkProject in gitLabWrapper.GetProjects()) {
                var mergeRequests = gitLabWrapper.GetMergeRequests(checkProject, x => x.SourceProjectId == project.Id);
                var mergeRequest = mergeRequests.FirstOrDefault(x => x.SourceBranch == hook.Branch);
                if (mergeRequest != null)
                    return mergeRequest;
            }
            return null;
        }
        static void ProcessMergeRequestHook(GitLabWrapper gitLabWrapper, string serviceUser, MergeRequestHookClient hook, bool supportSendingMessages, string farmTaskName) {
            Log.Message($"Merge hook title: {hook.Attributes.Description}");
            Log.Message($"Merge hook state: {hook.Attributes.State}");

            var targetProject = gitLabWrapper.GetProject(hook.Attributes.TargetProjectId);
            var mergeRequest = gitLabWrapper.GetMergeRequest(targetProject, hook.Attributes.IID);
            if (supportSendingMessages)
                SendMessage(serviceUser, hook.Json, farmTaskName);

            if (!IsOpenedState(hook))
                return;

            Log.Message($"Merge hook action: {hook.Attributes.Action}");
            Log.Message($"Merge hook merge status: {hook.Attributes.MergeStatus}");
            Log.Message($"Merge hook author: {gitLabWrapper.GetUser(hook.Attributes.AuthorId).Name}.");
            Log.Message($"Merge hook target branch: {hook.Attributes.TargetBranch}.");
            Log.Message($"Merge hook sourceBranch branch: {hook.Attributes.SourceBranch}.");

            if (ShouldForceSyncTask(mergeRequest, hook)) {
                ForceSyncBuild(gitLabWrapper, mergeRequest, targetProject, hook);
                return;
            }
        }
        static void SendMessage(string serviceUser, string json, string farmTaskName) {
            var bytes = Encoding.UTF8.GetBytes(json);
            string message = Convert.ToBase64String(bytes);

            FarmIntegrator.SendNotification(farmServer, farmTaskName, serviceUser, message);
        }
        static void ForceSyncBuild(GitLabWrapper gitLabWrapper, MergeRequest mergeRequest, Project targetProject, MergeRequestHookClient hook) {
            var xmlComments = gitLabWrapper.GetComments(mergeRequest).Where(x => IsXml(x.Note));
            var options = xmlComments.Select(x => MergeRequestOptions.ConvertFromString(x.Note)).FirstOrDefault();
            if (options != null && options.ActionType == MergeRequestActionType.sync) {
                var action = (MergeRequestSyncAction)options.Action;
                if (action.TestIntegration) {
                    Log.Message("Check build status before force build.");
                    var commit = gitLabWrapper.GetMergeRequestCommits(mergeRequest).FirstOrDefault();
                    var build = commit != null ? gitLabWrapper.GetBuilds(mergeRequest, commit.Id).FirstOrDefault() : null;
                    var buildStatus = build?.Pipeline.Status;
                    Log.Message($"Build status = {buildStatus}.");
                    if (buildStatus == PipelineStatus.success)
                        ForceBuild(action.SyncTask);
                    return;
                }
                Log.Message("Build forces without checking tests status.");
                ForceBuild(action.SyncTask);
                return;
            }
            string task = FarmIntegrator.FindTask($"{mergeRequest.TargetBranch}@{targetProject.PathWithNamespace}");
            if (!string.IsNullOrEmpty(task)) {
                Log.Message($"Sync task {task} found by heuristic.");
                ForceBuild(task);
                return;
            }

            Log.Message("Merge request can`t be merged because merge request notes has no farm config.");
            Log.Message("");
        }
        static bool ShouldForceSyncTask(MergeRequest mergeRequest, MergeRequestHookClient hook) {
            var assignee = mergeRequest.Assignee;
            if (assignee == null || !assignee.Name.StartsWith("dxvcs2git")) {
                Log.Message("Force sync rejected because assignee is not set or not sync task.");
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
            return hook.Attributes.State == MergerRequestState.opened || hook.Attributes.State == MergerRequestState.opened;
        }
        static void ProcessPushHook(PushHookClient hook) {
        }
        static Task<int> DoSyncWork(SyncOptions clo) {
            string localGitDir = clo.LocalFolder != null && Path.IsPathRooted(clo.LocalFolder) ? clo.LocalFolder : Path.Combine(Environment.CurrentDirectory, clo.LocalFolder ?? repoPath);
            //EnsureGitDir(localGitDir);

            string gitRepoPath = clo.Repo;
            string username = clo.Login;
            string password = clo.Password;
            string gitlabauthtoken = clo.AuthToken;
            string branchName = clo.Branch;
            string trackerPath = clo.Tracker;
            string gitServer = clo.Server;
            string syncTask = clo.SyncTask;
            int commitsCount = clo.CommitsCount;

            bool sendNotifications = !string.IsNullOrEmpty(clo.SyncTask) && !string.IsNullOrWhiteSpace(clo.SyncTask);

            if (sendNotifications)
                FarmIntegrator.Start(null);

            DXVcsWrapper vcsWrapper = new DXVcsWrapper(vcsServer, username, password);

            TrackBranch branch = FindBranch(branchName, trackerPath, vcsWrapper);
            if (branch == null)
                return Task.FromResult(1);

            string historyPath = GetVcsSyncHistory(vcsWrapper, branch.HistoryPath);
            if (historyPath == null)
                return Task.FromResult(1);
            SyncHistory history = SyncHistory.Deserialize(historyPath);
            if (history == null)
                return Task.FromResult(1);

            SyncHistoryWrapper syncHistory = new SyncHistoryWrapper(history, vcsWrapper, branch.HistoryPath, historyPath);
            var head = syncHistory.GetHistoryHead();
            if (head == null)
                return Task.FromResult(1);

            GitLabWrapper gitLabWrapper = new GitLabWrapper(gitServer, gitlabauthtoken);
            RegisteredUsers registeredUsers = new RegisteredUsers(gitLabWrapper, vcsWrapper);
            User defaultUser = registeredUsers.GetUser(username);
            if (!defaultUser.IsRegistered) {
                Log.Error($"default user {username} is not registered in the active directory.");
                return Task.FromResult(1);
            }

            var checkMergeChangesResult = CheckChangesForMerging(gitLabWrapper, gitRepoPath, branchName, head, vcsWrapper, branch, syncHistory, defaultUser);
            if (checkMergeChangesResult == CheckMergeChangesResult.NoChanges)
                return Task.FromResult(0);
            if (checkMergeChangesResult == CheckMergeChangesResult.Error)
                return Task.FromResult(1);

            SendMessageToGitTools(sendNotifications, syncTask, username, "Initializing git repo.");
            GitWrapper gitWrapper = CreateGitWrapper(gitRepoPath, localGitDir, "master", username, password);
            if (gitWrapper == null)
                return Task.FromResult(1);

            SendMessageToGitTools(sendNotifications, syncTask, username, "Adding vcs commits to git.");

            var historyResult = ProcessHistory(vcsWrapper, gitWrapper, registeredUsers, defaultUser, gitRepoPath, localGitDir, branch, commitsCount, syncHistory);

            if (historyResult == ProcessHistoryResult.NotEnough)
                return Task.FromResult(0);
            if (historyResult == ProcessHistoryResult.Failed)
                return Task.FromResult(1);

            SendMessageToGitTools(sendNotifications, syncTask, username, "Process merge requests.");

            int result = ProcessMergeRequests(vcsWrapper, gitWrapper, gitLabWrapper, registeredUsers, defaultUser, gitRepoPath, localGitDir, clo.Branch, clo.Tracker, syncHistory, username, sendNotifications, syncTask);

            SendMessageToGitTools(sendNotifications, syncTask, username, "Shutting down.");
            if (result != 0)
                return Task.FromResult(result);
            return Task.FromResult(0);
        }
        static ProcessHistoryResult ProcessHistory(DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, RegisteredUsers registeredUsers, User defaultUser, string gitRepoPath, string localGitDir, TrackBranch branch, int commitsCount, SyncHistoryWrapper syncHistory) {
            var (commits, historyResult) = GenerateHistory(vcsWrapper, gitWrapper, registeredUsers, defaultUser, gitRepoPath, localGitDir, branch, commitsCount, syncHistory, true);
            var requiredTrackItems = commits.SelectMany(x => x.Items).Select(x => x.Track).Distinct().ToList();
            gitWrapper.Config("core.sparsecheckout", "true");
            Log.Message("Sparse checkout enabled");
            gitWrapper.SparseCheckout(branch.Name, CalcSparseCheckoutFile(requiredTrackItems));
            Log.Message($"Sparse checkout branch {branch.Name}");
            ProcessHistoryInternal(vcsWrapper, gitWrapper, registeredUsers, defaultUser, localGitDir, branch, commits, syncHistory);
            Log.Message($"Importing history from vcs completed.");
            return historyResult;
        }
        static string CalcSparseCheckoutFile(List<TrackItem> items) {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(".gitignore");
            builder.AppendLine(".gitattributes");
            foreach (var trackItem in items) {
                string trackRoot = trackItem.Branch.GetRepoRoot(trackItem);
                if (trackItem.IsFile)
                    builder.AppendLine(trackRoot);
                else {
                    builder.AppendLine($@"/{trackRoot}/");
                }
            }
            return builder.ToString();
        }
        static void SendMessageToGitTools(bool sendMessage, string task, string user, string message) {
            if (!sendMessage)
                return;
            var notification = new FarmSyncTaskNotification(FarmNotificationType.synctask, task, message);
            FarmIntegrator.SendNotification(farmServer, task, user, JsonConvert.SerializeObject(notification));
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
            return vcsWrapper.GetLatestFile(historyPath);
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
        static GitWrapper CreateGitWrapper(string gitRepoPath, string localGitDir, string branchName, string username = null, string password = null) {
            try {
                GitCredentials credentials = username == null && password == null ? null : new GitCredentials {User = username, Password = password};
                var gitWrapper = new GitWrapper(localGitDir, gitRepoPath, branchName, credentials);
                Log.Message($"Branch {branchName} initialized.");

                return gitWrapper;
            }
            catch (Exception e) {
                Log.Error("Git Wrapper was not created: " + e.Message, e);
                return null;
            }
        }
        static TrackBranch FindBranch(string branchName, string trackerPath, DXVcsWrapper vcsWrapper) {
            string configPath;
            if (Path.IsPathRooted(trackerPath))
                configPath = trackerPath;
            else {
                string localPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                configPath = Path.Combine(localPath, trackerPath);
            }

            var branch = GetBranch(branchName, configPath, vcsWrapper);
            if (branch == null) {
                Log.Error($"Specified branch {branchName} not found.");
                return null;
            }
            return branch;
        }
        static int ProcessMergeRequests(DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, GitLabWrapper gitLabWrapper, RegisteredUsers users, User defaultUser, string gitRepoPath,
            string localGitDir, string branchName, string tracker, SyncHistoryWrapper syncHistory, string userName, bool sendNotification, string taskName) {
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
                var mergeRequestResult = ProcessMergeRequest(vcsWrapper, gitWrapper, gitLabWrapper, users, defaultUser, localGitDir, branch, mergeRequest, syncHistory, sendNotification, taskName);
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
        static MergeRequestResult ProcessMergeRequest(DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, GitLabWrapper gitLabWrapper, RegisteredUsers users, User defaultUser, string localGitDir, TrackBranch branch, MergeRequest mergeRequest, SyncHistoryWrapper syncHistory, bool sendNotifications, string syncTask) {
            switch (mergeRequest.State) {
                case MergeRequestState.opened:
                case MergeRequestState.reopened:
                    return ProcessOpenedMergeRequest(vcsWrapper, gitWrapper, gitLabWrapper, users, defaultUser, localGitDir, branch, mergeRequest, syncHistory, sendNotifications, syncTask);
            }
            return MergeRequestResult.InvalidState;
        }
        static MergeRequestResult ProcessOpenedMergeRequest(DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, GitLabWrapper gitLabWrapper, RegisteredUsers users, User defaultUser, string localGitDir, TrackBranch branch, MergeRequest mergeRequest, SyncHistoryWrapper syncHistory, bool sendNotifications, string syncTask) {
            string autoSyncToken = syncHistory.CreateNewToken();
            var lastHistoryItem = syncHistory.GetHead();

            SendMessageToGitTools(sendNotifications, syncTask, defaultUser.UserName, $"Processing merge {mergeRequest.Title}");

            Log.Message($"Start merging mergerequest {mergeRequest.Title}");

            Log.ResetErrorsAccumulator();
            var changes = gitLabWrapper.GetMergeRequestChanges(mergeRequest).ToList();
            if (changes.Count >= MaxChangesCount) {
                Log.Error($"Merge request contains more than {MaxChangesCount} changes and cannot be processed. Split it into smaller merge requests");
                AssignBackConflictedMergeRequest(gitLabWrapper, users, mergeRequest, CalcCommentForFailedCheckoutMergeRequest(null));
                return MergeRequestResult.Failed;
            }
            var genericChange = changes
                .Select(x => {
                    var trackItem = CalcTrackItem(branch, x.OldPath);
                    var newTrackItem = trackItem;
                    if (x.IsRenamed) {
                        newTrackItem = CalcTrackItem(branch, x.NewPath);
                    }
                    return new { trackItem = trackItem, newTrackItem = newTrackItem, item = x };
                })
                .Where(x => x.trackItem != null || x.newTrackItem != null)
                .Select(x => ProcessMergeRequestChanges(mergeRequest, x.item, localGitDir, x.trackItem, x.newTrackItem, autoSyncToken)).ToList();

            var requiredTrackItems = genericChange
                .Where(x => x.Track != null).Select(x => x.Track)
                .Concat(genericChange
                    .Where(x => x.NewTrack != null).Select(x => x.NewTrack)
                ).Distinct().ToList();
            gitWrapper.ReadTree(CalcSparseCheckoutFile(requiredTrackItems));

            bool ignoreValidation = gitLabWrapper.ShouldIgnoreSharedFiles(mergeRequest);

            if (!ValidateMergeRequestChanges(gitLabWrapper, mergeRequest, ignoreValidation) || !vcsWrapper.ProcessCheckout(genericChange, ignoreValidation, branch)) {
                Log.Error("Merging merge request failed because failed validation.");
                AssignBackConflictedMergeRequest(gitLabWrapper, users, mergeRequest, CalcCommentForFailedCheckoutMergeRequest(genericChange));
                vcsWrapper.ProcessUndoCheckout(genericChange);
                return MergeRequestResult.CheckoutFailed;
            }
            CommentWrapper comment = CalcComment(mergeRequest, branch, autoSyncToken);
            mergeRequest = gitLabWrapper.ProcessMergeRequest(mergeRequest, comment.ToString());
            if (mergeRequest.State == MergeRequestState.merged) {
                Log.Message("Merge request merged successfully.");

                gitWrapper.Pull();

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
        static TrackItem CalcTrackItem(TrackBranch branch, string path) {
            var trackItem = branch.TrackItems.Where(b => !b.IsFile).FirstOrDefault(track => CheckItemForChangeSet(path, track));
            if (trackItem == null)
                trackItem = branch.TrackItems.Where(b => b.IsFile).FirstOrDefault(track => CheckFileItemForChangeSet(path, track));
            return trackItem;
        }
        static bool CheckItemForChangeSet(string path, TrackItem track) {
            if (string.IsNullOrEmpty(path))
                return false;
            string projectPath = track.ProjectPath.EndsWith(@"/") ? track.ProjectPath : $@"{track.ProjectPath}/";
            return path.StartsWith(projectPath, StringComparison.InvariantCultureIgnoreCase);
        }
        static bool CheckFileItemForChangeSet(string path, TrackItem track) {
            if (string.IsNullOrEmpty(path))
                return false;
            string projectPath = track.ProjectPath;
            return string.Compare(path, projectPath, StringComparison.InvariantCultureIgnoreCase) == 0;
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
        static SyncItem ProcessMergeRequestChanges(MergeRequest mergeRequest, MergeRequestFileData fileData, string localGitDir, TrackItem trackItem, TrackItem newTrackItem, string token) {
            if (trackItem == null && newTrackItem == null)
                throw new ArgumentNullException("trackItem and newTrackItems both null.");
            var syncItem = new SyncItem();
            if (fileData.IsNew) {
                syncItem.SyncAction = SyncAction.New;
                syncItem.LocalPath = CalcLocalPath(localGitDir, fileData.OldPath);
                syncItem.VcsPath = CalcVcsPath(trackItem, fileData.OldPath);
            }
            else if (fileData.IsDeleted) {
                syncItem.SyncAction = SyncAction.Delete;
                syncItem.LocalPath = CalcLocalPath(localGitDir, fileData.OldPath);
                syncItem.VcsPath = CalcVcsPath(trackItem, fileData.OldPath);
            }
            else if (fileData.IsRenamed) {
                if (trackItem == null) {
                    syncItem.SyncAction = SyncAction.New;
                    syncItem.LocalPath = CalcLocalPath(localGitDir, fileData.NewPath);
                    syncItem.VcsPath = CalcVcsPath(newTrackItem, fileData.NewPath);
                }
                else if (newTrackItem == null) {
                    syncItem.SyncAction = SyncAction.Delete;
                    syncItem.LocalPath = CalcLocalPath(localGitDir, fileData.OldPath);
                    syncItem.VcsPath = CalcVcsPath(trackItem, fileData.OldPath);
                }
                else {
                    syncItem.SyncAction = SyncAction.Move;
                    syncItem.LocalPath = CalcLocalPath(localGitDir, fileData.OldPath);
                    syncItem.NewLocalPath = CalcLocalPath(localGitDir, fileData.NewPath);
                    syncItem.VcsPath = CalcVcsPath(trackItem, fileData.OldPath);
                    syncItem.NewVcsPath = CalcVcsPath(newTrackItem, fileData.NewPath);
                }
            }
            else {
                syncItem.SyncAction = SyncAction.Modify;
                syncItem.LocalPath = CalcLocalPath(localGitDir, fileData.OldPath);
                syncItem.VcsPath = CalcVcsPath(trackItem, fileData.OldPath);
            }
            syncItem.SingleSyncFile = trackItem?.IsFile ?? newTrackItem.IsFile;
            syncItem.Track = trackItem;
            syncItem.NewTrack = newTrackItem;
            syncItem.Comment = CalcComment(mergeRequest, trackItem?.Branch ?? newTrackItem.Branch, token);
            return syncItem;
        }
        static string CalcLocalPath(string localGitDir, string path) {
            return Path.Combine(localGitDir, path);
        }
        static string CalcVcsPath(TrackItem trackItem, string path) {
            var resultPath = path.Remove(0, trackItem.ProjectPath.Length).TrimStart(@"\/".ToCharArray());
            var branch = trackItem.Branch;
            return branch.GetVcsPath(trackItem, resultPath);
        }
        static (IList<CommitItem> commits, ProcessHistoryResult result) GenerateHistory(
            DXVcsWrapper vcsWrapper, GitWrapper gitWrapper, RegisteredUsers users, User defaultUser, string gitRepoPath, string localGitDir, TrackBranch branch, int commitsCount,
            SyncHistoryWrapper syncHistory, bool mergeCommits) {

            IList<CommitItem> commits = GenerateCommits(vcsWrapper, branch, syncHistory, mergeCommits);

            if (commits.Count > commitsCount) {
                Log.Message($"Commits generated. First {commitsCount} of {commits.Count} commits taken.");
                commits = commits.Take(commitsCount).ToList();
            }
            else {
                Log.Message($"Commits generated. {commits.Count} commits taken.");
            }
            var historyResult = commits.Count > commitsCount ? ProcessHistoryResult.NotEnough : ProcessHistoryResult.Success;
            return (commits, historyResult);
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
                    string localPath = branch.GetLocalRoot(localCommit.Track, localGitDir);
                    if (localCommit.Track.IsFile) {
                        if (File.Exists(localPath))
                            File.Delete(localPath);
                        string trackPath = branch.GetTrackRoot(localCommit.Track);
                        vcsWrapper.GetFile(trackPath, localPath, item.TimeStamp);
                    }
                    else {
                        DirectoryHelper.DeleteDirectory(localPath);
                        string trackPath = branch.GetTrackRoot(localCommit.Track);
                        vcsWrapper.GetProject(vcsServer, trackPath, localPath, item.TimeStamp);
                    }
                    Log.Message($"git stage {localCommit.Track.ProjectPath}");
                    gitWrapper.Stage(localCommit.Track.ProjectPath);
                }
                var syncCommitItem = localCommits.Last();
                string syncCommitAuthor = CalcAuthor(syncCommitItem, defaultUser);
                var comment = CalcComment(syncCommitItem, syncCommitAuthor, token);
                User user = users.GetUser(syncCommitAuthor);
                try {
                    gitWrapper.Commit(comment.ToString(), user, syncCommitItem.TimeStamp, false);
                    last = gitWrapper.FindCommit(x => true);
                    hasModifications = true;
                }
                catch (Exception) {
                    Log.Message($"Empty commit detected for {syncCommitItem.Author} {syncCommitItem.TimeStamp}.");
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
                return branches.FirstOrDefault(x => x.Name == branchName);
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
            comment.Branch = item.Track.Branch.Name;
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
        public string OldVcsPath { get; set; }
        public string NewVcsPath { get; set; }
    }

    public class PatchInfo {
        public long TimeStamp { get; set; }
        public List<PatchItem> Items { get; set; }
    }
}