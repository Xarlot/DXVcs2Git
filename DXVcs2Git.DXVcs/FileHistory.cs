using System;
using System.Collections;
using System.Collections.Generic;
using DXVCS;

namespace DXVcsTools.DXVcsClient {
    class FileHistory : IEnumerable<FileVersionInfo> {
        readonly IDXVCSService service;
        readonly string vcsFile;
        byte[][] data;
        FileHistoryInfo[] history;
        byte[] previousData;
        int[] versions;
        public FileHistory(string vcsFile, IDXVCSService service) {
            this.vcsFile = vcsFile;
            this.service = service;
        }

        FileHistoryInfo[] History {
            get {
                if (history == null) {
                    history = service.GetFileHistory(vcsFile, null, null, null, false);
                    Array.Sort(history, (x, y) => y.Version - x.Version);

                    service.GetFileDiffHistory(vcsFile, out data, out versions);
                }
                return history;
            }
        }

        public int Count {
            get { return History.Length; }
        }

        public IEnumerator<FileVersionInfo> GetEnumerator() {
            int i = 0;
            bool previousIsBranch = false;
            foreach (FileHistoryInfo historyInfo in History) {
                if (historyInfo.Type != HistoryInfoType.File)
                    continue;

                if (!previousIsBranch) {
                    byte[] revisionData = DXVCSHelpers.TryToDecompressData(data[i]);
                    int version;
                    if (Diff.IsDiffs(revisionData, out version)) {
                        byte[] curSplitter;
                        DiffByteItem[] diffs = Diff.BytesToDiffs(revisionData, out curSplitter);
                        revisionData = Diff.GetDataFromDiff(previousData, diffs, curSplitter);
                    }
                    previousData = revisionData;
                }
                previousIsBranch = historyInfo.Message == "Branch";
                i++;

                yield return new FileVersionInfo(historyInfo.Version, historyInfo.ActionDate, historyInfo.User, historyInfo.Comment, previousData);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}