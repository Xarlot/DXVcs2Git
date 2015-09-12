using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Core {
    public class ProjectExtractor {
        readonly IList<CommitItem> commits;
        readonly Action<DateTime> extractHandler;
        int index = 0
        public ProjectExtractor(IList<CommitItem> commits, Action<DateTime> extractHandler) {
            this.commits = commits;
            this.extractHandler = extractHandler;
        }
        public bool PerformExtraction() {
            if (index >= this.commits.Count)
                return false;
            extractHandler(commits[index++].TimeStamp);
            return true;
        } 
    }
}
