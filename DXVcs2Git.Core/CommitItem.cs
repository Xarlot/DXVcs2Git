using System;
using System.Collections.Generic;

namespace DXVcs2Git.Core {
    public class CommitItem {
        public string Author { get; set; }
        public TrackItem Track { get; set; }
        public DateTime TimeStamp { get; set; }
        public IList<HistoryItem> Items { get; set; }
    }
}
