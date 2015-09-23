using System.Collections.Generic;
using System.Linq;
using Polenter.Serialization;

namespace DXVcs2Git.Core {
    public class TrackBranch {
        public static IList<TrackBranch> Deserialize(string path) {
            SharpSerializer serializer = new SharpSerializer();
            var branches = (IList<TrackBranch>)serializer.Deserialize(path);
            ProcessTrackItems(branches);
            return branches;
        }
        static void ProcessTrackItems(IList<TrackBranch> branches) {
            foreach (var branch in branches) {
                foreach (var item in branch.TrackItems)
                    item.Branch = branch.Name;
            }
        }

        public string Name { get; private set; }
        public IList<TrackItem> TrackItems { get; private set; }
        public string HistoryPath { get; private set; }

        public TrackBranch() {
        }
        public TrackBranch(string branchName, string historyPath, IEnumerable<TrackItem> trackItems) {
            Name = branchName;
            HistoryPath = historyPath;
            TrackItems = trackItems.ToList();
        }
    }
}
