namespace DXVcs2Git.Core {
    public enum SyncAction {
        Modify,
        New,
        Delete,
        Move,
    }

    public enum ProcessState {
        Default,
        Modified,
        Failed,
        Ignored,
    }
    public class SyncItem {
        public string LocalPath { get; set; }
        public string VcsPath { get; set; }
        public string NewLocalPath { get; set; }
        public string NewVcsPath { get; set; }
        public SyncAction SyncAction { get; set; }
        public CommentWrapper Comment { get; set; }
        public ProcessState State { get; set; }
        public bool SharedFile { get; set; }
        public bool SingleSharedFile { get; set; }
        public bool SingleSyncFile { get; set; }
        public TrackItem Track { get; set; }
    }
}
