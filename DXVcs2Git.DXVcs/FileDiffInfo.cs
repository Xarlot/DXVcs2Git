using System;
using System.Collections.Generic;
using System.Text;
using DXVCS;
using DXVcsTools.Data;

namespace DXVcsTools.DXVcsClient {
    public class FileDiffInfo {
        readonly DiffStringItem[][] deltas;
        readonly FileVersionInfoBase[] historyInfo;
        string[] baseData;
        SpacesAction spacesAction = SpacesAction.Compare;
        public FileDiffInfo(int historyCount) {
            if (historyCount < 1)
                throw new ArgumentException("history");

            deltas = new DiffStringItem[historyCount - 1][];
            historyInfo = new FileVersionInfoBase[historyCount];
        }

        public SpacesAction SpacesAction {
            get { return spacesAction; }
            set { spacesAction = value; }
        }

        public int LastRevision {
            get { return historyInfo[0].Version; }
        }

        public void AddItem(int index, FileVersionInfo info) {
            if (index == 0) {
                baseData = GetFileLines(info.Data);
                historyInfo[0] = new FileVersionInfoBase(info);
            }
            else {
                string[] revisionData = GetFileLines(info.Data);
                deltas[index - 1] = StringsDiff.DiffStringLines(revisionData, baseData, SpacesAction);
                historyInfo[index] = new FileVersionInfoBase(info);
                baseData = revisionData;
            }
        }

        public IList<IBlameLine> BlameAtRevision(int revision) {
            List<IBlameLine> lines = MakeLines(baseData, historyInfo[historyInfo.Length - 1]);
            for (int i = historyInfo.Length - 2; i >= 0 && historyInfo[i].Version <= revision; i--)
                ApplyDelta(lines, deltas[i], historyInfo[i]);
            return lines;
        }

        static string[] GetFileLines(byte[] data) {
            Encoding encoding = null;
            string text = DXVCSHelpers.ReadTextFromByte(data, out encoding);
            return StringsDiff.GetTextLines(text);
        }

        static List<IBlameLine> MakeLines(string[] fileLines, FileVersionInfoBase fileVersionInfo) {
            var result = new List<IBlameLine>();
            foreach (string line in fileLines) {
                result.Add(new BlameLine(fileVersionInfo, line));
            }
            return result;
        }

        static void ApplyDelta(List<IBlameLine> annotation, DiffStringItem[] delta, FileVersionInfoBase fileVersionInfo) {
            foreach (DiffStringItem item in delta) {
                if (item.DeletedA > 0)
                    annotation.RemoveRange(item.StartB, item.DeletedA);
                if (item.InsertedB > 0) {
                    annotation.InsertRange(item.StartB, new List<string>(item.Inserted).ConvertAll(x => (IBlameLine)new BlameLine(fileVersionInfo, x)));
                }
            }
        }
    }

    class BlameLine : IBlameLine {
        readonly string sourceLine;
        readonly FileVersionInfoBase versionInfo;
        public BlameLine(FileVersionInfoBase versionInfo, string sourceLine) {
            this.versionInfo = versionInfo;
            this.sourceLine = sourceLine;
        }
        public string SourceLine {
            get { return sourceLine; }
        }
        public string Comment {
            get { return versionInfo.Comment; }
        }
        public string User {
            get { return versionInfo.User; }
        }
        public int Revision {
            get { return versionInfo.Version; }
        }
        public DateTime CommitDate {
            get { return versionInfo.Date.ToLocalTime(); }
        }
    }
}