namespace DXVcs2Git.Core {
    public enum SyncAction {
        Modify,
        New,
        Delete,
        Move,
    }
    public class SyncItem {
        public string LocalPath { get; set; }
        public string VcsPath { get; set; }
        public string NewLocalPath { get; set; }
        public string NewVcsPath { get; set; }
        public SyncAction SyncAction { get; set; }
        public string Comment { get; set; }
    }
}
