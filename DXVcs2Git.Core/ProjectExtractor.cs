using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Core {
    public class ProjectExtractor {
        readonly IList<CommitItem> commits;
        readonly Action<DateTime> extractHandler;
        readonly Action<string> prepareHandler;
        int index;
        public ProjectExtractor(IList<CommitItem> commits, Action<DateTime> extractHandler, Action<string> prepareHandler) {
            this.commits = commits;
            this.extractHandler = extractHandler;
            this.prepareHandler = prepareHandler;
        }
        public bool PerformExtraction() {
            if (index >= this.commits.Count)
                return false;
            CommitItem commitItem = this.commits[this.index];
            prepareHandler(commitItem.Path);
            extractHandler(commitItem.TimeStamp);
            index++;
            return true;
        } 
    }
}
