using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DXVcs2Git.DXVcs;
using Polenter.Serialization;

namespace DXVcs2Git.Core {
    public class TrackBranch {
        public static IList<TrackBranch> Deserialize(string path, DXVcsWrapper vcsWrapper) {
            if (!File.Exists(path)) {
                Log.Error($"Track branch config with path {path} was not found.");
                return new List<TrackBranch>();
            }
            SharpSerializer serializer = new SharpSerializer();
            var branches = (IList<TrackBranch>)serializer.Deserialize(path);
            ProcessTrackItems(branches, vcsWrapper);
            return branches;
        }
        static void ProcessTrackItems(IList<TrackBranch> branches, DXVcsWrapper vcsWrapper) {
            foreach (var branch in branches) {
                foreach (var trackItem in branch.TrackItems)
                    trackItem.Branch = branch;
                branch.TrackItems = CalcTrackItems(branch, branch.TrackItems, vcsWrapper);
            }
        }
        static IList<TrackItem> CalcTrackItems(TrackBranch branch, IEnumerable<TrackItem> trackItems, DXVcsWrapper vcsWrapper) {
            List<TrackItem> list = new List<TrackItem>();
            foreach (var trackItem in trackItems) {
                list.AddRange(vcsWrapper.GenerateTrackItems(branch, trackItem));
            }
            return list;
        }

        public string Name { get; private set; }
        public IList<TrackItem> TrackItems { get; private set; }
        public string HistoryPath { get; private set; }
        public string RepoRoot { get; private set; }

        public TrackBranch() {
        }
        public TrackBranch(string branchName, string historyPath, string repoRoot, IEnumerable<TrackItem> trackItems) {
            Name = branchName;
            HistoryPath = historyPath;
            TrackItems = trackItems.ToList();
            RepoRoot = repoRoot;
        }
        public string GetLocalRoot(TrackItem trackItem, string localPath) {
            return Path.Combine(localPath, trackItem.ProjectPath);
        }
        public string GetRepoRoot(TrackItem trackItem) {
            if (trackItem.Branch.Name != Name)
                throw new ArgumentException("invalid branch");
            string result = string.IsNullOrEmpty(trackItem.AdditionalOffset) ? trackItem.ProjectPath : Path.Combine(trackItem.AdditionalOffset, trackItem.ProjectPath);
            return result;
        }
        public string GetVcsPath(TrackItem trackItem, string path) {
            if (trackItem.IsFile)
                return GetTrackRoot(trackItem);
            return $"{GetTrackRoot(trackItem)}/{path}";
        }
        public string GetTrackRoot(TrackItem trackItem) {
            if (trackItem.Branch.Name != Name)
                throw new ArgumentException("invalid branch");
            string result = string.IsNullOrEmpty(trackItem.AdditionalOffset) ? Path.Combine(RepoRoot, trackItem.Path) : Path.Combine(RepoRoot, trackItem.AdditionalOffset, trackItem.Path);
            return result.Replace("\\", "/");
        }
        public bool IsTrackingVcsPath(string vcsPath) {
            return TrackItems.Any(x => {
                string trackingPath = GetTrackRoot(x);
                return vcsPath.StartsWith(trackingPath.TrimEnd('/') + "/");
            });
        }
    }
}
