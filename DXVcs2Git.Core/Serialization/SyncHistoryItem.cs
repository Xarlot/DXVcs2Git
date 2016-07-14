using System;

namespace DXVcs2Git.Core {
    public enum SyncHistoryStatus {
        Success,
        Failed,
        Mixed,
    }
    public class SyncHistoryItem {
        public string GitCommitSha { get; set; }
        public long VcsCommitTimeStamp { get; set; }
        public DateTime ReadableVcsCommitTime => new DateTime(VcsCommitTimeStamp).ToLocalTime();
        public string Token { get; set; }
        public SyncHistoryStatus Status { get; set; }
    }
}
