using System.Collections.Generic;
using System.Linq;

namespace DXVcs2Git.Core {
    public class Tracker {
        public IList<TrackBranch> Branches { get; }
        public Tracker(IEnumerable<TrackItem> trackItems) {
            var grouped = trackItems.GroupBy(x => x.Branch);
            Branches = grouped.Select(x => {
                string branch = x.First().Branch;
                return new TrackBranch(branch, $"$/{branch}", x);
            }).ToList();
        }
        public TrackBranch FindBranch(string name) {
            return Branches.FirstOrDefault(x => x.Name == name);
        }
    }
}
