using System;
using System.Collections.Generic;
using System.Linq;
using DXVcs2Git.Core;

namespace DXVcs2Git.DXVcs {
    public static class HistoryGenerator {
        public static IList<HistoryItem> GenerateHistory(string server, TrackBranch branch, DateTime from) {
            try {
                var repo = DXVcsConectionHelper.Connect(server);
                var history = Enumerable.Empty<HistoryItem>();
                foreach (var trackItem in branch.TrackItems) {
                    var historyForItem = repo.GetProjectHistory(trackItem.Path, true, from).Select(x =>
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
                throw;
            }
        }
        public static IList<CommitItem> GenerateCommits(IEnumerable<HistoryItem> historyItems) {
            var grouped = historyItems.AsParallel().GroupBy(x => x.ActionDate);
            var commits = grouped.Select(x => new CommitItem() {Items = x.ToList(), TimeStamp = x.First().ActionDate}).OrderBy(x => x.TimeStamp);
            var totalCommits = commits.ToList();
            int index = totalCommits.FindIndex(x => x.Items.Any(y => y.Message.ToLowerInvariant() == "create"));
            return totalCommits.Skip(index).ToList();
        }
        public static void GetProject(string server, string vcsPath, string localPath, DateTime timeStamp) {
            try {
                var repo = DXVcsConectionHelper.Connect(server);
                repo.GetProject(vcsPath, localPath, timeStamp);
                Log.Message($"HistoryGenerator.GetProject performed for {vcsPath}");
            }
            catch (Exception ex) {
                Log.Error("HistoryGenerator.GetProject failed.", ex);
                throw;
            }
        }
        public static IEnumerable<CommitItem> GetCommits(IList<HistoryItem> items) {
            var changedProjects = items.GroupBy(x => x.Track.Path);
            foreach (var project in changedProjects) {
                var changeSet = project.GroupBy(x => x.User);
                foreach (var changeItem in changeSet) {
                    CommitItem item = new CommitItem();
                    var first = changeItem.First();
                    item.Author = first.User;
                    item.Items = changeItem.ToList();
                    item.Track = first.Track;
                    item.TimeStamp = first.ActionDate;
                    yield return item;
                }
            }
        }
    }
}