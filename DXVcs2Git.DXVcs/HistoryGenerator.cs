using System;
using System.Collections.Generic;
using System.Linq;
using DXVcs2Git.Core;

namespace DXVcs2Git.DXVcs {
    public static class HistoryGenerator {
        public static IList<HistoryItem> GenerateHistory(string server, TrackBranch branch) {
            try {
                var repo = DXVcsConectionHelper.Connect(server);
                var history = Enumerable.Empty<HistoryItem>();
                foreach (var trackItem in branch.TrackItems) {
                    var historyForItem = repo.GetProjectHistory(trackItem.FullPath, true).Select(x =>
                        new HistoryItem() {
                            ActionDate = x.ActionDate,
                            Comment = x.Comment,
                            Label = x.Label,
                            Message = x.Message,
                            Name = x.Name,
                            User = x.User,
                            Track = trackItem,
                        });
                    history = history.Concat(historyForItem);
                }
                return history.ToList();
            }
            catch (Exception ex) {
                Log.Error("HistoryGenerator.GenerateHistory failed.", ex);
                return new List<HistoryItem>();
            }
        }
        public static IList<CommitItem> GenerateCommits(IEnumerable<HistoryItem> historyItems) {
            var grouped = historyItems.AsParallel().GroupBy(x => x.ActionDate);
            var commits = grouped.Select(x => {
                IList<HistoryItem> items = x.ToList();
                HistoryItem historyItem = items.First();
                return new CommitItem() { Author = historyItem.User, TimeStamp = historyItem.ActionDate, Items = items, Track = historyItem.Track };
            }).OrderBy(x => x.TimeStamp);
            var totalCommits = commits.ToList();
            int index = totalCommits.FindIndex(x => x.Items.Any(y => y.Message.ToLowerInvariant() == "create"));
            return totalCommits.Skip(index).ToList();
        }
    }
}
