using System;
using System.Collections.Generic;
using System.Linq;
using DXVcs2Git.Core;

namespace DXVcs2Git.DXVcs {
    public class DXVcsWrapper {
        readonly string server;
        public DXVcsWrapper(string server) {
            this.server = server;
        }
        public bool CheckOut(string vcsPath, string localPath, bool dontGetLocalCopy) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
                if (repo.IsUnderVss(vcsPath)) {
                    repo.CheckOutFile(vcsPath, localPath, "DXVcs2GitService: checkout for sync", dontGetLocalCopy);
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
        public bool CheckIn(string vcsPath, string localPath, string comment) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
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
        public bool UndoCheckout(string vcsPath) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
                if (repo.IsUnderVss(vcsPath) && repo.IsCheckedOutByMe(vcsPath)) {
                    repo.UndoCheckout(vcsPath, repo.GetFileWorkingPath(vcsPath));
                }
                else {
                    Log.Error($"File {vcsPath} is not under vss.");
                    return false;
                }
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Undo checkout file {vcsPath} failed. ", ex);
                return false;
            }
        }
        public bool CheckOut(SyncItem item) {
            return CheckOut(item.VcsPath, item.LocalPath, true);
        }
        public bool CheckIn(SyncItem item, string comment) {
            switch (item.SyncAction) {
                case SyncAction.New:
                    return CheckIn(item.VcsPath, item.LocalPath, comment);
                case SyncAction.Modify:
                    return CheckIn(item.VcsPath, item.LocalPath, comment);
                case SyncAction.Delete:
                case SyncAction.Move:
                    return true;
                default:
                    throw new ArgumentException("SyncAction");
            }
        }
        bool ModifyItem(SyncItem item) {
            bool result = CheckOut(item);
            if (!result)
                Log.Error($"Failed attempt to checkout modified file {item.VcsPath}");
            return result;
        }
        public bool RollbackItem(SyncItem item) {
            return UndoCheckout(item.VcsPath);
        }
        bool CreateItem(string vcsPath, string localPath) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
                if (repo.IsUnderVss(vcsPath))
                    return true;
                repo.AddFile(vcsPath, new byte[0], "");
                repo.CheckOutFile(vcsPath, localPath, "", true);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Add new file {vcsPath} failed.", ex);
                return false;
            }
        }
        bool DeleteItem(string vcsPath, string localPath) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
                if (!repo.IsUnderVss(vcsPath))
                    return true;
                if (repo.IsCheckedOut(vcsPath) && !repo.IsCheckedOutByMe(vcsPath)) {
                    Log.Error($"File {vcsPath} is checked out already.");
                    return false;
                }
                repo.DeleteFile(vcsPath);
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Add new file {vcsPath} failed.", ex);
                return false;
            }
        }
        bool MoveFile(string vcsPath, string newVcsPath, string localPath, string newLocalPath) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
                if (!repo.IsUnderVss(vcsPath)) {
                    Log.Error($"Move file failed. Can`t locate {vcsPath}.");
                    return false;
                }
                if (repo.IsUnderVss(newVcsPath)) {
                    Log.Error($"Move file error. File {vcsPath} already exist.");
                    return false;
                }
                string[] exist = repo.MoveFile(vcsPath, newVcsPath, string.Empty);
                if (exist == null || exist.Length == 0) {
                    CheckOut(newVcsPath, newLocalPath, true);
                    return true;
                }
                foreach (var file in exist)
                    Log.Error($"Move file error. File {file} already exist.");
                return false;
            }
            catch (Exception ex) {
                Log.Error($"Add new file {vcsPath} failed.", ex);
                return false;
            }
        }
        public bool ProcessCheckout(IEnumerable<SyncItem> items) {
            var list = items.ToList();
            if (!list.All(ProcessCheckoutItem)) {
                Log.Message("Rollback changes after failed checkout.");
                list.All(RollbackItem);
                return false;
            }
            return true;
        }
        bool ProcessCheckoutItem(SyncItem item) {
            switch (item.SyncAction) {
                case SyncAction.New:
                    return CreateItem(item.VcsPath, item.LocalPath);
                case SyncAction.Modify:
                    return ModifyItem(item);
                case SyncAction.Delete:
                    return DeleteItem(item.VcsPath, item.LocalPath);
                case SyncAction.Move:
                    return MoveFile(item.VcsPath, item.NewVcsPath, item.LocalPath, item.NewLocalPath);
                default:
                    throw new ArgumentException("SyncAction");
            }
        }
        public void CreateLabel(string vcsPath, string labelName, string comment = "") {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
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
        public bool ProcessUndoChechout(IEnumerable<SyncItem> items) {
            return true;
        }
    }
}
