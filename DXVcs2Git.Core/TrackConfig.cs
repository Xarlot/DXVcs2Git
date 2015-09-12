using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DXVcs2Git.Core {
    public class TrackConfig {
        const string findBranchRegex = @"\$/(20)\d\d.\d/";
        public IList<TrackItem> TrackItems { get; private set; }
        Regex regex;
        public TrackConfig(string path) {
            regex = new Regex(findBranchRegex, RegexOptions.IgnoreCase);
            string[] lines = File.ReadAllLines(path);
            TrackItems = lines.Select(CreateTrackItem).ToList();
        }
        TrackItem CreateTrackItem(string path) {
            var match = regex.Match(path);
            string branchName = string.Empty;
            string branchPath = string.Empty;
            if (match.Success) {
                branchName = match.Value.Replace(@"$\/", string.Empty);
                int index = match.Index + match.Length;
                branchPath = path.Substring(index, path.Length - index);
            }
            return new TrackItem() {Branch = branchName, Path = branchPath};
        }
    }
}
