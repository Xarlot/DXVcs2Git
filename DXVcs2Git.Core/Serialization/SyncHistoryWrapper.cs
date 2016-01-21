using System;
using System.Linq;
using DXVcs2Git.DXVcs;

namespace DXVcs2Git.Core.Serialization {
    public class SyncHistoryWrapper {
        readonly int historyLimit = 10;
        readonly SyncHistory history;
        readonly string vcsHistoryPath;
        readonly string localHistoryPath;
        readonly DXVcsWrapper vcsWrapper;
        public SyncHistoryWrapper(SyncHistory history, DXVcsWrapper vcsWrapper, string vcsHistoryPath, string localHistoryPath) {
            this.history = history;
            this.vcsHistoryPath = vcsHistoryPath;
            this.localHistoryPath = localHistoryPath;
            this.vcsWrapper = vcsWrapper;
        }
        public void Add(string sha, long timeStamp, string token, SyncHistoryStatus status = SyncHistoryStatus.Success) {
            history.Items.Add(new SyncHistoryItem() {
                GitCommitSha = sha,
                VcsCommitTimeStamp = timeStamp,
                Token = token,
                Status = status,
            });
        }
        public SyncHistoryItem GetHead() {
            return history.Items.LastOrDefault();
        }
        public SyncHistoryItem GetPrevious(SyncHistoryItem item) {
            int index = this.history.Items?.FindIndex(x => x == item) ?? -1;
            return index < 1 ? null : this.history.Items[index - 1];
        }
        public string CreateNewToken() {
            int token = this.history.Items.Max(x => {
                int result;
                if (Int32.TryParse(x.Token, out result))
                    return result;
                return 0;
            });
            return (token + 1).ToString();
        }
        public void Save() {
            try {
                this.vcsWrapper.CheckOutFile(this.vcsHistoryPath, this.localHistoryPath, true, string.Empty);
                SyncHistory.Serialize(this.history.Clone(this.historyLimit), localHistoryPath);
                this.vcsWrapper.CheckInFile(vcsHistoryPath, localHistoryPath, string.Empty);
            }
            catch (Exception ex) {
                Log.Error($"Save history to {vcsHistoryPath} failed.", ex);
                throw;
            }
        }
        public SyncHistoryItem GetHistoryHead() {
            var head = GetHead();
            do {
                if (head == null) {
                    Log.Error("Failed sync. Can`t find history item with success status.");
                    return null;
                }
                if (head.Status == SyncHistoryStatus.Failed) {
                    Log.Error("Failed sync detected. Repair repo.");
                    return null;
                }
                if (head.Status == SyncHistoryStatus.Success)
                    break;
                head = GetPrevious(head);
            }
            while (true);
            return head;
        }
    }
}
