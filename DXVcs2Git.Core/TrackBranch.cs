using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Core {
    public class TrackBranch {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public IList<TrackItem> TrackItems { get; private set; }

        public TrackBranch(string branchName, string branchPath, IEnumerable<TrackItem> trackItems) {
            Name = branchName;
            Path = branchPath;
            TrackItems = trackItems.ToList();
        }
    }
}
