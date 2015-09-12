using System;
using System.Collections.Generic;

namespace DXVcs2Git.Core {
    public struct CommitItem {
        public DateTime TimeStamp { get; set; }
        public string Author { get; set; }
        public IList<HistoryItem> Items { get; set; }
        public string Path { get; set; }
    }
}
