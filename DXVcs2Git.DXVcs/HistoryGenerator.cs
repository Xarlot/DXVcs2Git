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
                            Branch = branch.Path
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
            var commits = grouped.Select(x => new CommitItem() { Author = x.First().User, TimeStamp = x.First().ActionDate, Items = x.ToList() }).OrderBy(x => x.TimeStamp);
            return commits.ToList();
        }
    }
}
