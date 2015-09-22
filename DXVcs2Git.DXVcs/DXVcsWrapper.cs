using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DXVcs2Git.Core;

namespace DXVcs2Git.DXVcs {
    public class DXVcsWrapper {
        readonly string server;
        string branchPath;
        string localPath;
        public DXVcsWrapper(string server, string branchPath, string localPath) {
            this.server = server;
            this.branchPath = branchPath;
            this.localPath = localPath;
        }
        public bool CheckOut(string vcsPath) {
            try {
                var repo = DXVcsConectionHelper.Connect(server);
                if (repo.IsUnderVss(vcsPath)) {
                    repo.CheckOutFile(vcsPath, repo.GetFileWorkingPath(vcsPath), "DXVcs2GitService: checkout for sync");
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
        public bool UndoCheckout(string vcsPath) {
            try {
                var repo = DXVcsConectionHelper.Connect(server);
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
            return CheckOut(item.VcsPath);
        }
        public bool ProcessItem(SyncItem item) {
            switch (item.SyncAction) {
                case SyncAction.New:
                    return CreateItem(item);
                case SyncAction.Modify:
                    return ProcessModify(item);
                case SyncAction.Delete:
                    return CheckOut(item);
                case SyncAction.Move:
                    return CheckOut(item);
                default:
                    throw new ArgumentException("SyncAction");
            }
        }
        bool ProcessModify(SyncItem item) {
            bool result = CheckOut(item);
            Log.Error($"Failed attempt to checkout modified file {item.VcsPath}");
            return result;
        }
        public bool RollbackItem(SyncItem item) {
            return UndoCheckout(item.VcsPath);
        }
        bool CreateItem(SyncItem item) {
            try {
                var repo = DXVcsConectionHelper.Connect(server);
                repo.AddFile(item.VcsPath, File.ReadAllBytes(item.LocalPath), "");
                repo.CheckOutFile(item.VcsPath, item.LocalPath, "");
                return true;
            }
            catch (Exception ex) {
                Log.Error($"Add new file {item.VcsPath} failed.", ex);
                return false;
            }
        }
        public bool ProcessCheckout(IEnumerable<SyncItem> items) {
            var list = items.ToList();
            if (!list.All(ProcessItem)) {
                Log.Message("Rollback changes after failed checkout.");
                list.All(RollbackItem);
                return false;
            }
            return true;
        }
    }
}
