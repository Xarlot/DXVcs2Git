using System;
using System.Linq;
using DXVcs2Git.DXVcs;

namespace DXVcs2Git.Core.Serialization {
    public class SyncHistoryWrapper {
        readonly SyncHistory history;
        readonly string vcsHistoryPath;
        readonly string localHistoryPath;
        readonly string server;
        public SyncHistoryWrapper(SyncHistory history, string server, string vcsHistoryPath, string localHistoryPath) {
            this.history = history;
            this.vcsHistoryPath = vcsHistoryPath;
            this.localHistoryPath = localHistoryPath;
            this.server = server;
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
                var repo = DXVcsConnectionHelper.Connect(server);
                repo.CheckOutFile(vcsHistoryPath, localHistoryPath, string.Empty);
                SyncHistory.Serialize(history, localHistoryPath);
                repo.CheckInFile(vcsHistoryPath, localHistoryPath, string.Empty);
            }
            catch (Exception ex) {
                Log.Error($"Save history to {vcsHistoryPath} failed.", ex);
                throw;
            }
        }
    }
}
