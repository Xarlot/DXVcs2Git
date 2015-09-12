using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Core {
    public class ProjectExtractor {
        readonly IList<CommitItem> commits;
        readonly Action<string, DateTime> extractHandler;
        int index;
        public ProjectExtractor(IList<CommitItem> commits, Action<string, DateTime> extractHandler) {
            this.commits = commits;
            this.extractHandler = extractHandler;
        }
        public bool PerformExtraction() {
            if (index >= this.commits.Count)
                return false;
            CommitItem commitItem = this.commits[this.index];
            extractHandler(commitItem.Path, commitItem.TimeStamp);
            index++;
            return true;
        } 
    }
}
