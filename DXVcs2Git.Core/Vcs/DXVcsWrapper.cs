using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using DXVcs2Git.Core;
using DXVCS;
using System.Text;

namespace DXVcs2Git.DXVcs {
    public class DXVcsWrapper {
        readonly string server;
        readonly string user;
        readonly string password;
        public DXVcsWrapper(string server, string user = null, string password = null) {
            this.server = server;
            this.user = $@"corp\{user}";
            this.password = password;
        }
        public bool CheckOutFile(string vcsPath, string localPath, bool dontGetLocalCopy, string comment, StringBuilder errorMessage) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (repo.IsUnderVss(vcsPath)) {
                    if (repo.IsCheckedOut(vcsPath)) {
                        if (repo.IsCheckedOutByMe(vcsPath))
                            return true;
                        var fileData = repo.GetFileData(vcsPath);
                        Log.Message($"File {vcsPath} is checked out by {fileData.CheckedOutUser} already. Check out failed.");
                        errorMessage.AppendLine(Log.LastErrorMessage);
                        return false;
                    }
                    repo.CheckOutFile(vcsPath, localPath, comment, dontGetLocalCopy);
                }
                else {
                    Log.Error($"File {vcsPath} is not under vss.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return false;
                }
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Checkout file {vcsPath} failed. ", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
        }
        public bool CheckInFile(string vcsPath, string localPath, string comment, StringBuilder errorMessage) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (repo.IsUnderVss(vcsPath) && repo.IsCheckedOutByMe(vcsPath)) {
                    repo.CheckInFile(vcsPath, localPath, comment);
                }
                else {
                    Log.Error($"File {vcsPath} is not under vss.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return false;
                }
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Checkin file {vcsPath} failed. ", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
        }
        public bool UndoCheckoutFile(string vcsPath, string localPath, StringBuilder errorMessage) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (repo.IsUnderVss(vcsPath)) {
                    if (repo.IsCheckedOutByMe(vcsPath)) {
                        repo.UndoCheckout(vcsPath, localPath);
                    }
                }
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Undo checkout file {vcsPath} failed. ", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
        }
        public bool CheckInNewFile(string vcsPath, string localPath, string comment, StringBuilder errorMessage) {
            Log.Message($"Check in for new file {vcsPath}");
            return CheckInFile(vcsPath, localPath, comment, errorMessage);
        }
        public bool CheckInChangedFile(string vcsPath, string localPath, string comment, StringBuilder errorMessage) {
            Log.Message($"Check in for changed file {vcsPath}");
            return CheckInFile(vcsPath, localPath, comment, errorMessage);
        }
        public bool CheckOut(SyncItem item, string comment, StringBuilder errorMessage) {
            return CheckOutFile(item.VcsPath, item.LocalPath, true, comment, errorMessage);
        }
        public bool CheckIn(SyncItem item, string comment, StringBuilder errorMessage) {
            if (item.State == ProcessState.Ignored)
                return true;

            switch (item.SyncAction) {
                case SyncAction.New:
                    return CheckInNewFile(item.VcsPath, item.LocalPath, comment, errorMessage);
                case SyncAction.Modify:
                    return CheckInChangedFile(item.VcsPath, item.LocalPath, comment, errorMessage);
                case SyncAction.Delete:
                    return CheckInDeletedFile(item.VcsPath, item.LocalPath, comment, errorMessage);
                case SyncAction.Move:
                    return CheckInMovedFile(item.VcsPath, item.NewVcsPath, item.LocalPath, item.NewLocalPath, comment, errorMessage);
                default:
                    throw new ArgumentException("SyncAction");
            }
        }
        bool CheckOutModifyFile(string vcsPath, string localFile, string comment, StringBuilder errorMessage) {
            Log.Message($"Checkout for modify file {vcsPath}");
            return CheckOutFile(vcsPath, localFile, true, comment, errorMessage);
        }
        public bool RollbackItem(SyncItem item, StringBuilder errorMessage) {
            if (item.State == ProcessState.Default)
                return true;
            return UndoCheckoutFile(item.VcsPath, item.LocalPath, errorMessage);
        }
        bool CheckOutCreateFile(string vcsPath, string localPath, string comment, StringBuilder errorMessage) {
            try {
                Log.Message($"Checkout for create file {vcsPath}");
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath))
                    repo.AddFile(vcsPath, new byte[0], comment);
                repo.CheckOutFile(vcsPath, localPath, comment, true);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Add new file {vcsPath} failed.", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
        }
        bool CheckOutDeleteFile(string vcsPath, string localPath, string comment, StringBuilder errorMessage) {
            try {
                Log.Message($"Checkout for deleted file {vcsPath}");
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath))
                    return true;
                if (repo.IsCheckedOut(vcsPath) && !repo.IsCheckedOutByMe(vcsPath)) {
                    Log.Error($"File {vcsPath} is checked out already.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return false;
                }
                repo.CheckOutFile(vcsPath, localPath, comment, true);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Add new file {vcsPath} failed.", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
        }
        bool CheckInDeletedFile(string vcsPath, string localPath, string comment, StringBuilder errorMessage) {
            try {
                Log.Message($"Checkin for deleted file {vcsPath}");
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath))
                    return true;
                if (repo.IsCheckedOut(vcsPath) && !repo.IsCheckedOutByMe(vcsPath)) {
                    Log.Error($"File {vcsPath} is checked out already.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return false;
                }
                UndoCheckoutFile(vcsPath, localPath, errorMessage);
                repo.DeleteFile(vcsPath, comment);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Remove file {vcsPath} failed.", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
        }
        bool CheckOutMoveFile(string vcsPath, string newVcsPath, string localPath, string newLocalPath, string comment, StringBuilder errorMessage) {
            try {
                Log.Message($"Checkout for move file {vcsPath}");
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath)) {
                    Log.Error($"Move file failed. Can`t locate {vcsPath}.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return false;
                }
                if (repo.IsUnderVss(newVcsPath)) {
                    Log.Error($"Move file error. File {vcsPath} already exist.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return false;
                }
                CheckOutFile(vcsPath, localPath, true, comment, errorMessage);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Add new file {vcsPath} failed.", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
        }
        bool CheckInMovedFile(string vcsPath, string newVcsPath, string localPath, string newLocalPath, string comment, StringBuilder errorMessage) {
            try {
                Log.Message($"Checkin for moved file {vcsPath}");
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath)) {
                    Log.Error($"Move file failed. Can`t locate {vcsPath}.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return false;
                }
                if (repo.IsUnderVss(newVcsPath)) {
                    Log.Error($"Move file error. File {vcsPath} already exist.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return false;
                }
                repo.UndoCheckout(vcsPath, comment);
                repo.MoveFile(vcsPath, newVcsPath, comment);
                CheckOutFile(newVcsPath, newLocalPath, true, comment, errorMessage);
                CheckInFile(newVcsPath, newLocalPath, comment, errorMessage);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Move file {vcsPath} failed.", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
        }
        public bool ProcessCheckout(IEnumerable<SyncItem> items, bool ignoreSharedFiles, StringBuilder errorMessage) {
            var list = items.ToList();
            list.ForEach(x => {
                TestFileResult result = ProcessBeforeCheckout(x, ignoreSharedFiles, errorMessage);
                x.State = CalcBeforeCheckoutState(result);
            });

            if (list.Any(x => x.State == ProcessState.Failed))
                return false;

            list.ForEach(x => {
                TestFileResult result = ProcessCheckoutItem(x, x.Comment.ToString(), errorMessage);
                x.State = CalcCheckoutStateAfterCheckout(result);
            });
            return list.All(x => x.State == ProcessState.Modified || x.State == ProcessState.Ignored);
        }
        static ProcessState CalcCheckoutStateAfterCheckout(TestFileResult result) {
            switch (result) {
                case TestFileResult.Ok:
                    return ProcessState.Modified;
                case TestFileResult.Fail:
                    return ProcessState.Failed;
                case TestFileResult.Ignore:
                    return ProcessState.Ignored;
                default:
                    throw new Exception("result");

            }
        }
        static ProcessState CalcBeforeCheckoutState(TestFileResult result) {
            switch (result) {
                case TestFileResult.Ok:
                    return ProcessState.Default;
                case TestFileResult.Fail:
                    return ProcessState.Failed;
                case TestFileResult.Ignore:
                    return ProcessState.Ignored;
                default:
                    throw new Exception("result");

            }
        }
        TestFileResult ProcessBeforeCheckout(SyncItem item, bool ignoreSharedFiles, StringBuilder errorMessage) {
            TestFileResult result;
            switch (item.SyncAction) {
                case SyncAction.New:
                    result = BeforeCheckOutCreateFile(item.VcsPath, item.LocalPath, ignoreSharedFiles, errorMessage);
                    break;
                case SyncAction.Modify:
                    result = BeforeCheckOutModifyFile(item.VcsPath, item.LocalPath, ignoreSharedFiles, errorMessage);
                    break;
                case SyncAction.Delete:
                    result = BeforeCheckOutDeleteFile(item.VcsPath, item.LocalPath, ignoreSharedFiles, errorMessage);
                    break;
                case SyncAction.Move:
                    result = BeforeCheckOutMoveFile(item.VcsPath, item.NewVcsPath, item.LocalPath, item.NewLocalPath, ignoreSharedFiles, errorMessage);
                    break;
                default:
                    throw new ArgumentException("SyncAction");
            }
            return result;
        }
        TestFileResult BeforeCheckOutMoveFile(string vcsPath, string newVcsPath, string localPath, string newLocalPath, bool ignoreSharedFiles, StringBuilder errorMessage) {
            if (!PerformHasFileTestBeforeCheckout(vcsPath, errorMessage)) {
                Log.Error($"Check move capability. Source file {vcsPath} is not found in vcs.");
                errorMessage.AppendLine(Log.LastErrorMessage);
                return TestFileResult.Fail;
            }
            if (PerformHasFileTestBeforeCheckout(newVcsPath, errorMessage)) {
                Log.Error($"Check move capability. Target file {newVcsPath} is found in vcs.");
                errorMessage.AppendLine(Log.LastErrorMessage);
                return TestFileResult.Fail;
            }

            var oldPathResult = PerformSimpleTestBeforeCheckout(vcsPath, ignoreSharedFiles, errorMessage);
            if (oldPathResult != TestFileResult.Ok)
                return oldPathResult;
            return PerformSimpleTestBeforeCheckout(newVcsPath, ignoreSharedFiles, errorMessage);
        }
        bool PerformHasFileTestBeforeCheckout(string vcsPath, StringBuilder errorMessage) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                return repo.IsUnderVss(vcsPath);
            }
            catch (Exception ex) {
                Log.Error($"Test file {vcsPath} before ckeckout failed.", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
        }
        TestFileResult BeforeCheckOutDeleteFile(string vcsPath, string localPath, bool ignoreSharedFiles, StringBuilder errorMessage) {
            return PerformSimpleTestBeforeCheckout(vcsPath, ignoreSharedFiles, errorMessage);
        }
        TestFileResult BeforeCheckOutModifyFile(string vcsPath, string localPath, bool ignoreSharedFiles, StringBuilder errorMessage) {
            if (!PerformHasFileTestBeforeCheckout(vcsPath, errorMessage)) {
                Log.Error($"Check modify capability. File {vcsPath} is not found in vcs.");
                errorMessage.AppendLine(Log.LastErrorMessage);
                return TestFileResult.Fail;
            }
            return PerformSimpleTestBeforeCheckout(vcsPath, ignoreSharedFiles, errorMessage);
        }
        TestFileResult BeforeCheckOutCreateFile(string vcsPath, string localPath, bool ignoreSharedFiles, StringBuilder errorMessage) {
            return PerformSimpleTestBeforeCheckout(vcsPath, ignoreSharedFiles, errorMessage);
        }
        TestFileResult PerformSimpleTestBeforeCheckout(string vcsPath, bool ignoreSharedFiles, StringBuilder errorMessage) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath))
                    return TestFileResult.Ok;

                bool hasLiveLinks = repo.HasLiveLinks(vcsPath);
                if (hasLiveLinks) {
                    if (ignoreSharedFiles)
                        return TestFileResult.Ignore;
                    Log.Error($"Can`t process shared file {vcsPath}. Destroy active links or sync this file manually using vcs.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return TestFileResult.Fail;
                }

                var filedata = repo.GetFileData(vcsPath);

                bool isFree = !filedata.CheckedOut || filedata.CheckedOutMe;
                if (!isFree) {
                    Log.Error($"Can`t process checked out file {vcsPath}. Contact {filedata.CheckedOutUser} or resert check out state.");
                    errorMessage.AppendLine(Log.LastErrorMessage);
                    return TestFileResult.Fail;
                }
                return TestFileResult.Ok;
            }
            catch (Exception ex) {
                Log.Error($"Test file {vcsPath} before checkout failed.", ex);
                errorMessage.AppendLine(Log.LastErrorMessage);
            }
            return TestFileResult.Fail;
        }
        TestFileResult ProcessCheckoutItem(SyncItem item, string comment, StringBuilder errorMessage) {
            if (item.State == ProcessState.Ignored)
                return TestFileResult.Ignore;

            switch (item.SyncAction) {
                case SyncAction.New:
                    return CheckOutCreateFile(item.VcsPath, item.LocalPath, comment, errorMessage) ? TestFileResult.Ok : TestFileResult.Fail;
                case SyncAction.Modify:
                    return CheckOutModifyFile(item.VcsPath, item.LocalPath, comment, errorMessage) ? TestFileResult.Ok : TestFileResult.Fail;
                case SyncAction.Delete:
                    return CheckOutDeleteFile(item.VcsPath, item.LocalPath, comment, errorMessage) ? TestFileResult.Ok : TestFileResult.Fail;
                case SyncAction.Move:
                    return CheckOutMoveFile(item.VcsPath, item.NewVcsPath, item.LocalPath, item.NewLocalPath, comment, errorMessage) ? TestFileResult.Ok : TestFileResult.Fail;
                default:
                    throw new ArgumentException("SyncAction");
            }
        }
        public void CreateLabel(string vcsPath, string labelName, string comment = "") {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                repo.CreateLabel(vcsPath, labelName, comment);
            }
            catch (Exception ex) {
                Log.Error($"Create label {labelName} failed.", ex);
            }
        }
        public bool ProcessCheckIn(IEnumerable<SyncItem> items, string comment, StringBuilder errorMessage) {
            var list = items.ToList();
            if (!list.All(x => CheckIn(x, comment, errorMessage))) {
                Log.Message("Check in changes failed.");
                errorMessage.AppendLine(Log.LastErrorMessage);
                return false;
            }
            Log.Message("Check in changes success.");
            return true;
        }
        public bool ProcessUndoCheckout(IEnumerable<SyncItem> items, StringBuilder errorMessage) {
            Log.Message("Rollback changes after failed checkout.");
            items.ToList().ForEach(x => {
                if (x.State == ProcessState.Modified)
                    RollbackItem(x, errorMessage);
            });
            return true;
        }
        public IList<HistoryItem> GenerateHistory(TrackBranch branch, DateTime from) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, user, password);
                var history = Enumerable.Empty<HistoryItem>();
                foreach (var trackItem in branch.TrackItems) {
                    var historyForItem = repo.GetProjectHistory(trackItem.Path, true, from).Select(x =>
                        new HistoryItem() {
                            ActionDate = x.ActionDate,
                            Comment = x.Comment,
                            Label = x.Label,
                            Message = x.Message,
                            Name = x.Name,
                            User = x.User,
                            Track = trackItem,
                        });
                    history = history.Concat(historyForItem);
                }
                return history.ToList();
            }
            catch (Exception ex) {
                Log.Error("History generation failed.", ex);
                throw;
            }
        }

        public IList<CommitItem> GenerateCommits(IEnumerable<HistoryItem> historyItems) {
            var grouped = historyItems.AsParallel().GroupBy(x => x.ActionDate);
            var commits = grouped.Select(x => new CommitItem() { Items = x.ToList(), TimeStamp = x.First().ActionDate }).OrderBy(x => x.TimeStamp);
            var totalCommits = commits.ToList();
            totalCommits = totalCommits.ToList();
            return totalCommits;
        }
        public IList<CommitItem> MergeCommits(IList<CommitItem> commits) {
            var result = new List<CommitItem>();
            CommitItem prevCommit = null;
            foreach (var commit in commits) {
                var canMerge =
                    prevCommit != null &&
                    commit.Items[0].User == prevCommit.Items[0].User &&
                    commit.Items[0].Comment == prevCommit.Items[0].Comment &&
                    (commit.Items[0].ActionDate - prevCommit.Items[0].ActionDate).TotalSeconds < 20;
                if (canMerge) {
                    foreach (var item in commit.Items)
                        prevCommit.Items.Add(item);
                    prevCommit.TimeStamp = commit.Items[0].ActionDate;
                    continue;
                }
                result.Add(commit);
                prevCommit = commit;
            }
            return result;
        }
        public void GetProject(string server, string vcsPath, string localPath, DateTime timeStamp) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                repo.GetProject(vcsPath, localPath, timeStamp);
                Log.Message($"Get project from vcs performed for {vcsPath}");
            }
            catch (Exception ex) {
                Log.Error("Get prroject from vcs failed.", ex);
                throw;
            }
        }
        public IEnumerable<CommitItem> GetCommits(DateTime timeStamp, IList<HistoryItem> items) {
            var changedProjects = items.GroupBy(x => x.Track.Path);
            foreach (var project in changedProjects) {
                var changeSet = project.GroupBy(x => x.User);
                foreach (var changeItem in changeSet) {
                    CommitItem item = new CommitItem();
                    var last = changeItem.Last();
                    item.Author = last.User;
                    item.Items = changeItem.ToList();
                    item.Track = last.Track;
                    item.TimeStamp = timeStamp;
                    yield return item;
                }
            }
        }
        public string GetFile(string historyPath, string local) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                string localPath = Path.GetTempFileName();
                repo.GetLatestFileVersion(historyPath, localPath);
                return localPath;
            }
            catch (Exception) {
                Log.Error($"Loading sync history from {historyPath} failed");
                return null;
            }
        }
        public HistoryItem FindCommit(TrackBranch branch, Func<HistoryItem, bool> func) {
            try {
                var history = GenerateHistory(branch, DateTime.Now.AddDays(-1));
                return history.Reverse().FirstOrDefault(func);
            }
            catch (Exception ex) {
                Log.Error($"Finc commit failed", ex);
                throw;
            }
        }
        public IEnumerable<UserInfo> GetUsers() {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, user, password);
                return repo.GetUsers();
            }
            catch (Exception ex) {
                Log.Error($"Get users from vcs failed.", ex);
                throw;
            }
        }
    }

    public enum TestFileResult {
        Ok,
        Fail,
        Ignore
    }
}
