namespace DXVcs2Git.Core {
    public class SyncHistoryItem {
        public string GitCommitSha { get; set; }
        public long VcsCommitTimeStamp { get; set; }
        public string ID { get; set; }
    }
}
