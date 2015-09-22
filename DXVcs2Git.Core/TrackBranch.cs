using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Core {
    public class TrackBranch {
        public string Name { get; private set; }
        public IList<TrackItem> TrackItems { get; private set; }

        public TrackBranch(string branchName, IEnumerable<TrackItem> trackItems) {
            Name = branchName;
            TrackItems = trackItems.ToList();
        }
    }
}
