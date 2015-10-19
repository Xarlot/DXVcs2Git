using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DXVcs2Git.Core;
using DXVcs2Git.Core.Serialization;

namespace DXVcs2Git.DXVcs {
    public static class HistoryGenerator {
        public static IList<HistoryItem> GenerateHistory(string server, TrackBranch branch, DateTime from) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
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
                var repo = DXVcsConnectionHelper.Connect(server);
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
        public static string GetFile(string server, string historyPath, string local) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
                string localPath = Path.GetTempFileName();
                repo.GetLatestFileVersion(historyPath, localPath);
                return localPath;
            }
            catch (Exception) {
                Log.Error($"Loading sync history from {historyPath} failed");
                return null;
            }
        }
        public static void SaveHistory(string server, string vcsFile, string localFile, SyncHistory history) {
            try {
                var repo = DXVcsConnectionHelper.Connect(server);
                repo.CheckOutFile(vcsFile, localFile, string.Empty);
                SyncHistory.Serialize(history, localFile);
                repo.CheckInFile(vcsFile, localFile, string.Empty);
            }
            catch (Exception ex) {
                Log.Error($"Save history to {vcsFile} failed.", ex);
                throw;
            }
        }
        public static HistoryItem FindCommit(string server, TrackBranch branch, Func<HistoryItem, bool> func) {
            try {
                var history = GenerateHistory(server, branch, DateTime.Now.AddDays(-1));
                return history.FirstOrDefault(func);
            }
            catch (Exception ex) {
                Log.Error($"Finc commit failed", ex);
                throw;
            }
        }
    }
}