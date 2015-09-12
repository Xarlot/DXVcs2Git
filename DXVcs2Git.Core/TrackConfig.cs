using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DXVcs2Git.Core {
    public class TrackConfig {
        const string FindBranchRegex = @"\$/(20)\d\d.\d/";
        public IList<TrackItem> TrackItems { get; private set; }
        public readonly Regex Regex = new Regex(FindBranchRegex, RegexOptions.IgnoreCase);
        public TrackConfig(string config) {
            TrackItems = config.Split(new[] {Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(CreateTrackItem).Where(x => !string.IsNullOrEmpty(x.Branch) && !string.IsNullOrEmpty(x.Path)).ToList();
        }
        TrackItem CreateTrackItem(string path) {
            var match = this.Regex.Match(path);
            string branchName = string.Empty;
            string branchPath = string.Empty;
            if (match.Success) {
                branchName = match.Value.Replace("$", string.Empty).Replace(@"\", string.Empty).Replace(@"/", string.Empty);
                int index = match.Index + match.Length;
                branchPath = path.Substring(index, path.Length - index);
            }
            return new TrackItem() {Branch = branchName, Path = branchPath};
        }
    }
}
