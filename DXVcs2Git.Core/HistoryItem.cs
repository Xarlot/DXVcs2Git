using System;

namespace DXVcs2Git.Core {
    public class HistoryItem {
        public string User;
        public DateTime ActionDate;
        public string Message;
        public string Label;
        public string Comment;
        public TrackItem Track;
    }
}