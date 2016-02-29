using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using DXVcs2Git.Core;
using DXVCS;

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
        public bool CheckOutFile(string vcsPath, string localPath, bool dontGetLocalCopy, string comment) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (repo.IsUnderVss(vcsPath)) {
                    if (repo.IsCheckedOut(vcsPath)) {
                        if (repo.IsCheckedOutByMe(vcsPath))
                            return true;
                        var fileData = repo.GetFileData(vcsPath);
                        Log.Message($"File {vcsPath} is checked out by {fileData.CheckedOutUser} already. Check out failed.");
                        return false;
                    }
                    repo.CheckOutFile(vcsPath, localPath, comment, dontGetLocalCopy);
                }
                else {
                    Log.Error($"File {vcsPath} is not under vss.");
                    return false;
                }
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Checkout file {vcsPath} failed. ", ex);
                return false;
            }
        }
        public bool CheckInFile(string vcsPath, string localPath, string comment) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (repo.IsUnderVss(vcsPath) && repo.IsCheckedOutByMe(vcsPath)) {
                    repo.CheckInFile(vcsPath, localPath, comment);
                }
                else {
                    Log.Error($"File {vcsPath} is not under vss.");
                    return false;
                }
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Checkin file {vcsPath} failed. ", ex);
                return false;
            }
        }
        public bool UndoCheckoutFile(string vcsPath, string localPath) {
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
                return false;
            }
        }
        public bool CheckInNewFile(string vcsPath, string localPath, string comment) {
            Log.Message($"Check in for new file {vcsPath}");
            return CheckInFile(vcsPath, localPath, comment);
        }
        public bool CheckInChangedFile(string vcsPath, string localPath, string comment) {
            Log.Message($"Check in for changed file {vcsPath}");
            return CheckInFile(vcsPath, localPath, comment);
        }
        public bool CheckOut(SyncItem item, string comment) {
            return CheckOutFile(item.VcsPath, item.LocalPath, true, comment);
        }
        public bool CheckIn(SyncItem item, string comment) {
            switch (item.SyncAction) {
                case SyncAction.New:
                    return CheckInNewFile(item.VcsPath, item.LocalPath, comment);
                case SyncAction.Modify:
                    return CheckInChangedFile(item.VcsPath, item.LocalPath, comment);
                case SyncAction.Delete:
                    return CheckInDeletedFile(item.VcsPath, item.LocalPath, comment);
                case SyncAction.Move:
                    return CheckInMovedFile(item.VcsPath, item.NewVcsPath, item.LocalPath, item.NewLocalPath, comment);
                default:
                    throw new ArgumentException("SyncAction");
            }
        }
        bool CheckOutModifyFile(string vcsPath, string localFile, string comment) {
            Log.Message($"Checkout for modify file {vcsPath}");
            return CheckOutFile(vcsPath, localFile, true, comment);
        }
        public bool RollbackItem(SyncItem item) {
            if (item.State == ProcessState.Default)
                return true;
            return UndoCheckoutFile(item.VcsPath, item.LocalPath);
        }
        bool CheckOutCreateFile(string vcsPath, string localPath, string comment) {
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
                return false;
            }
        }
        bool CheckOutDeleteFile(string vcsPath, string localPath, string comment) {
            try {
                Log.Message($"Checkout for deleted file {vcsPath}");
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath))
                    return true;
                if (repo.IsCheckedOut(vcsPath) && !repo.IsCheckedOutByMe(vcsPath)) {
                    Log.Error($"File {vcsPath} is checked out already.");
                    return false;
                }
                repo.CheckOutFile(vcsPath, localPath, comment, true);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Add new file {vcsPath} failed.", ex);
                return false;
            }
        }
        bool CheckInDeletedFile(string vcsPath, string localPath, string comment) {
            try {
                Log.Message($"Checkin for deleted file {vcsPath}");
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath))
                    return true;
                if (repo.IsCheckedOut(vcsPath) && !repo.IsCheckedOutByMe(vcsPath)) {
                    Log.Error($"File {vcsPath} is checked out already.");
                    return false;
                }
                UndoCheckoutFile(vcsPath, localPath);
                repo.DeleteFile(vcsPath, comment);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Remove file {vcsPath} failed.", ex);
                return false;
            }
        }
        bool CheckOutMoveFile(string vcsPath, string newVcsPath, string localPath, string newLocalPath, string comment) {
            try {
                Log.Message($"Checkout for move file {vcsPath}");
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath)) {
                    Log.Error($"Move file failed. Can`t locate {vcsPath}.");
                    return false;
                }
                if (repo.IsUnderVss(newVcsPath)) {
                    Log.Error($"Move file error. File {vcsPath} already exist.");
                    return false;
                }
                CheckOutFile(vcsPath, localPath, true, comment);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Add new file {vcsPath} failed.", ex);
                return false;
            }
        }
        bool CheckInMovedFile(string vcsPath, string newVcsPath, string localPath, string newLocalPath, string comment) {
            try {
                Log.Message($"Checkin for moved file {vcsPath}");
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                if (!repo.IsUnderVss(vcsPath)) {
                    Log.Error($"Move file failed. Can`t locate {vcsPath}.");
                    return false;
                }
                if (repo.IsUnderVss(newVcsPath)) {
                    Log.Error($"Move file error. File {vcsPath} already exist.");
                    return false;
                }
                repo.UndoCheckout(vcsPath, comment);
                repo.MoveFile(vcsPath, newVcsPath, comment);
                CheckOutFile(newVcsPath, newLocalPath, true, comment);
                CheckInFile(newVcsPath, newLocalPath, comment);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Move file {vcsPath} failed.", ex);
                return false;
            }
        }
        public bool ProcessCheckout(IEnumerable<SyncItem> items) {
            var list = items.ToList();
            list.ForEach(x => {
                bool result = ProcessBeforeCheckout(x);
                x.State = result ? x.State : ProcessState.Failed;
            });

            if (list.Any(x => x.State != ProcessState.Default))
                return false;

            list.ForEach(x => {
                bool result = ProcessCheckoutItem(x, x.Comment.ToString());
                x.State = result ? ProcessState.Modified : ProcessState.Failed;
            });
            return list.All(x => x.State == ProcessState.Modified);
        }
        bool ProcessBeforeCheckout(SyncItem item) {
            switch (item.SyncAction) {
                case SyncAction.New:
                    return BeforeCheckOutCreateFile(item.VcsPath, item.LocalPath);
                case SyncAction.Modify:
                    return BeforeCheckOutModifyFile(item.VcsPath, item.LocalPath);
                case SyncAction.Delete:
                    return BeforeCheckOutDeleteFile(item.VcsPath, item.LocalPath);
                case SyncAction.Move:
                    return BeforeCheckOutMoveFile(item.VcsPath, item.NewVcsPath, item.LocalPath, item.NewLocalPath);
                default:
                    throw new ArgumentException("SyncAction");
            }
        }
        bool BeforeCheckOutMoveFile(string vcsPath, string newVcsPath, string localPath, string newLocalPath) {
            return PerformSimpleTestBeforeCheckout(vcsPath) && PerformSimpleTestBeforeCheckout(newVcsPath);
        }
        bool BeforeCheckOutDeleteFile(string vcsPath, string localPath) {
            return PerformSimpleTestBeforeCheckout(vcsPath);
        }
        bool BeforeCheckOutModifyFile(string vcsPath, string localPath) {
            return PerformSimpleTestBeforeCheckout(vcsPath);
        }
        bool BeforeCheckOutCreateFile(string vcsPath, string localPath) {
            return PerformSimpleTestBeforeCheckout(vcsPath);
        }
        bool PerformSimpleTestBeforeCheckout(string vcsPath) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server, this.user, this.password);
                bool hasLiveLinks = repo.HasLiveLinks(vcsPath);
                if (hasLiveLinks) {
                    Log.Error($"Can`t process shared file {vcsPath}. Destroy active links or sync this file manually using vcs.");
                    return false;
                }

                return !repo.IsUnderVss(vcsPath) || !repo.IsCheckedOut(vcsPath) || repo.IsCheckedOutByMe(vcsPath);
            }
            catch (Exception ex) {
                Log.Error($"Test file {vcsPath} before ckeckout failed.", ex);
            }
            return true;

        }
        bool ProcessCheckoutItem(SyncItem item, string comment) {
            switch (item.SyncAction) {
                case SyncAction.New:
                    return CheckOutCreateFile(item.VcsPath, item.LocalPath, comment);
                case SyncAction.Modify:
                    return CheckOutModifyFile(item.VcsPath, item.LocalPath, comment);
                case SyncAction.Delete:
                    return CheckOutDeleteFile(item.VcsPath, item.LocalPath, comment);
                case SyncAction.Move:
                    return CheckOutMoveFile(item.VcsPath, item.NewVcsPath, item.LocalPath, item.NewLocalPath, comment);
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
        public bool ProcessCheckIn(IEnumerable<SyncItem> items, string comment) {
            var list = items.ToList();
            if (!list.All(x => CheckIn(x, comment))) {
                Log.Message("Check in changes failed.");
                return false;
            }
            Log.Message("Check in changes success.");
            return true;
        }
        public bool ProcessUndoCheckout(IEnumerable<SyncItem> items) {
            Log.Message("Rollback changes after failed checkout.");
            items.ToList().ForEach(x => {
                if (x.State == ProcessState.Modified)
                    RollbackItem(x);
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
}
