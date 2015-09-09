using System;
using System.Collections.Generic;
using System.Text;
using DXVCS;
using DXVCS.Properties;

namespace DXVcsTools.DXVcsClient {
    public enum SpacesAction {
        Compare,
        IgnoreChange,
        IgnoreAll
    }

    public class StringsDiff {
        static readonly string[] NewLines = new[] {"\r\n", "\r", "\n"};

        public static string[] GetTextLines(string text) {
            return text.Split(NewLines, StringSplitOptions.None);
        }

        public static DiffStringItem[] DiffStringLines(string[] dataA, string[] dataB, SpacesAction spacesAction) {
            IEqualityComparer<string> comparer = null;

            if (spacesAction == SpacesAction.IgnoreChange)
                comparer = new IgnoreComparer();
            else if (spacesAction == SpacesAction.IgnoreAll)
                comparer = new IgnoreAllComparer();

            var lineTable = new Dictionary<string, int>(comparer);
            var diffDataA = new DiffData(GetIndexList(dataA, lineTable));
            var diffDataB = new DiffData(GetIndexList(dataB, lineTable));
            return CreateStringDiffs(GetDiff(diffDataA, diffDataB), dataA, dataB);
        }
        static DiffItem[] GetDiff(DiffData diffDataA, DiffData diffDataB) {
            int max = diffDataA.Length + diffDataB.Length + 1;
            var downVector = new int[2 * max + 2];
            var upVector = new int[2 * max + 2];
            LongesCommonSubsequence(diffDataA, 0, diffDataA.Length, diffDataB, 0, diffDataB.Length, downVector, upVector);
            Optimize(diffDataA);
            Optimize(diffDataB);
            return CreateDiffs(diffDataA, diffDataB);
        }
        static DiffStringItem[] CreateStringDiffs(DiffItem[] items, string[] lineListA, string[] lineListB) {
            var result = new List<DiffStringItem>();
            for (int index = 0; index < items.Length; index++) {
                var item = new DiffStringItem();
                item.StartA = items[index].StartA;
                item.StartB = items[index].StartB;
                item.DeletedA = items[index].DeletedA;
                item.InsertedB = items[index].InsertedB;
                if (item.InsertedB > 0) {
                    item.Inserted = new string[item.InsertedB];
                    for (int i = 0; i < item.InsertedB; i++) {
                        item.Inserted[i] = (string)lineListB[item.StartB + i].Clone();
                    }
                }
                else {
                    item.Inserted = null;
                }
                if (item.DeletedA > 0) {
                    item.Deleted = new string[item.DeletedA];
                    for (int i = 0; i < item.DeletedA; i++) {
                        item.Deleted[i] = (string)lineListA[item.StartA + i].Clone();
                    }
                }
                else {
                    item.Deleted = null;
                }
                if (result.Count > 0) {
                    int prevDiffPos = result.Count - 1;
                    DiffStringItem prevDiff = result[prevDiffPos];
                    int nonDiffItemStartA = prevDiff.StartA + prevDiff.DeletedA;
                    int nonDiffItemLengthA = item.StartA - nonDiffItemStartA;
                    int nonDiffItemStartB = prevDiff.StartB + prevDiff.InsertedB;
                    int nonDiffItemLengthB = item.StartB - nonDiffItemStartB;
                    if (EmptyLinesOnly(lineListA, nonDiffItemStartA, nonDiffItemLengthA) && EmptyLinesOnly(lineListB, nonDiffItemStartB, nonDiffItemLengthB)) {
                        result.Add(GlueDiffStringItems(prevDiff, item, lineListA, lineListB));
                        result.RemoveAt(prevDiffPos);
                        continue;
                    }
                }
                result.Add(item);
            }
            return result.ToArray();
        }

        static int[] GetIndexList(string[] lines, Dictionary<string, int> lineTable) {
            int lastCode = lineTable.Count;
            var indexList = new int[lines.Length];
            int length = lines.Length;
            for (int i = 0; i < length; i++) {
                int index;
                if (!lineTable.TryGetValue(lines[i], out index)) {
                    lastCode++;
                    indexList[i] = lastCode;
                    lineTable.Add(lines[i], lastCode);
                }
                else {
                    indexList[i] = index;
                }
            }
            return indexList;
        }
        static DiffCoord ShortestMiddleSnake(DiffData diffDataA, int lowerA, int upperA, DiffData diffDataB, int lowerB, int upperB, int[] downVector, int[] upVector) {
            int max = diffDataA.Length + diffDataB.Length + 1;
            int downK = lowerA - lowerB;
            int upK = upperA - upperB;
            int delta = (upperA - lowerA) - (upperB - lowerB);
            bool oddDelta = (delta & 1) != 0;
            int downOffset = max - downK;
            int upOffset = max - upK;
            int maxD = ((upperA - lowerA + upperB - lowerB) / 2) + 1;
            downVector[downOffset + downK + 1] = lowerA;
            upVector[upOffset + upK - 1] = upperA;
            for (int d = 0; d <= maxD; d++) {
                for (int k = downK - d; k <= downK + d; k += 2) {
                    int x;
                    int z = downVector[downOffset + k + 1];
                    if (k == downK - d) {
                        x = z;
                    }
                    else {
                        x = downVector[downOffset + k - 1] + 1;
                        if ((k < downK + d) && (z >= x)) {
                            x = z;
                        }
                    }
                    int y = x - k;
                    while ((x < upperA) && (y < upperB) && (diffDataA.Data[x] == diffDataB.Data[y])) {
                        x++;
                        y++;
                    }
                    downVector[downOffset + k] = x;
                    if (oddDelta && (upK - d < k) && (k < upK + d)) {
                        if (upVector[upOffset + k] <= x) {
                            return new DiffCoord(x, x - k);
                        }
                    }
                }
                for (int k = upK - d; k <= upK + d; k += 2) {
                    int x;
                    int z = upVector[upOffset + k - 1];
                    if (k == upK + d) {
                        x = z;
                    }
                    else {
                        x = upVector[upOffset + k + 1] - 1;
                        if ((k > upK - d) && (z < x))
                            x = z;
                    }
                    int y = x - k;
                    while ((x > lowerA) && (y > lowerB) && (diffDataA.Data[x - 1] == diffDataB.Data[y - 1])) {
                        x--;
                        y--;
                    }
                    upVector[upOffset + k] = x;
                    if (!oddDelta && (downK - d <= k) && (k <= downK + d)) {
                        y = downVector[downOffset + k];
                        if (x <= y)
                            return new DiffCoord(y, y - k);
                    }
                }
            }
            throw new Exception(Resources.AlgorithmError);
        }
        static void LongesCommonSubsequence(DiffData diffDataA, int lowerA, int upperA, DiffData diffDataB, int lowerB, int upperB, int[] downVector, int[] upVector) {
            while (lowerA < upperA && lowerB < upperB && diffDataA.Data[lowerA] == diffDataB.Data[lowerB]) {
                lowerA++;
                lowerB++;
            }
            while (lowerA < upperA && lowerB < upperB && diffDataA.Data[upperA - 1] == diffDataB.Data[upperB - 1]) {
                --upperA;
                --upperB;
            }
            if (lowerA == upperA) {
                while (lowerB < upperB)
                    diffDataB.modified[lowerB++] = true;
            }
            else if (lowerB == upperB) {
                while (lowerA < upperA)
                    diffDataA.modified[lowerA++] = true;
            }
            else {
                DiffCoord coord = ShortestMiddleSnake(diffDataA, lowerA, upperA, diffDataB, lowerB, upperB, downVector, upVector);
                LongesCommonSubsequence(diffDataA, lowerA, coord.X, diffDataB, lowerB, coord.Y, downVector, upVector);
                LongesCommonSubsequence(diffDataA, coord.X, upperA, diffDataB, coord.Y, upperB, downVector, upVector);
            }
        }
        static void Optimize(DiffData diffData) {
            int start, end;
            start = 0;
            while (start < diffData.Length) {
                while ((start < diffData.Length) && (diffData.modified[start] == false))
                    start++;
                end = start;
                while ((end < diffData.Length) && diffData.modified[end])
                    end++;
                if ((end < diffData.Length) && (diffData.Data[start] == diffData.Data[end])) {
                    diffData.modified[start] = false;
                    diffData.modified[end] = true;
                }
                else {
                    start = end;
                }
            }
        }
        static DiffItem[] CreateDiffs(DiffData diffDataA, DiffData diffDataB) {
            var list = new List<DiffItem>();
            DiffItem item;
            int startA, startB;
            int lineA, lineB;
            lineA = 0;
            lineB = 0;
            while (lineA < diffDataA.Length || lineB < diffDataB.Length) {
                if ((lineA < diffDataA.Length) && (!diffDataA.modified[lineA]) && (lineB < diffDataB.Length) && (!diffDataB.modified[lineB])) {
                    lineA++;
                    lineB++;
                }
                else {
                    startA = lineA;
                    startB = lineB;
                    while (lineA < diffDataA.Length && (lineB >= diffDataB.Length || diffDataA.modified[lineA])) {
                        lineA++;
                    }
                    while (lineB < diffDataB.Length && (lineA >= diffDataA.Length || diffDataB.modified[lineB])) {
                        lineB++;
                    }
                    if ((startA < lineA) || (startB < lineB)) {
                        item = new DiffItem();
                        item.StartA = startA;
                        item.StartB = startB;
                        item.DeletedA = lineA - startA;
                        item.InsertedB = lineB - startB;
                        list.Add(item);
                    }
                }
            }
            return list.ToArray();
        }
        static DiffStringItem GlueDiffStringItems(DiffStringItem prevDiff, DiffStringItem diff, string[] linesA, string[] linesB) {
            var result = new DiffStringItem();
            result.DeletedA = diff.StartA + diff.DeletedA - prevDiff.StartA;
            result.Deleted = new string[result.DeletedA];
            Array.Copy(linesA, prevDiff.StartA, result.Deleted, 0, result.DeletedA);
            result.InsertedB = diff.StartB + diff.InsertedB - prevDiff.StartB;
            result.Inserted = new string[result.InsertedB];
            Array.Copy(linesB, prevDiff.StartB, result.Inserted, 0, result.InsertedB);
            result.StartA = prevDiff.StartA;
            result.StartB = prevDiff.StartB;
            return result;
        }
        static bool EmptyLinesOnly(string[] lines, int start, int length) {
            for (int i = start; i < start + length; i++) {
                if (lines[i] != string.Empty) {
                    return false;
                }
            }
            return true;
        }

        struct DiffCoord {
            public readonly int X;
            public readonly int Y;
            public DiffCoord(int x, int y) {
                X = x;
                Y = y;
            }
        }

        class DiffData {
            public readonly int[] Data;
            public readonly bool[] modified;
            public DiffData(int[] initData) {
                Data = initData;
                modified = new bool[initData.Length];
            }
            public int Length {
                get { return Data.Length; }
            }
        }

        class IgnoreAllComparer : IEqualityComparer<string> {
            public bool Equals(string x, string y) {
                return string.Equals(PrepeareString(x), PrepeareString(y));
            }
            public int GetHashCode(string obj) {
                return PrepeareString(obj).GetHashCode();
            }
            static string PrepeareString(string s) {
                var sb = new StringBuilder(s.Length);
                for (int i = 0; i < s.Length; i++) {
                    char c = s[i];
                    if (c != ' ' && c != '\t') {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }
        }

        class IgnoreComparer : IEqualityComparer<string> {
            public bool Equals(string x, string y) {
                return string.Equals(PrepeareString(x), PrepeareString(y));
            }
            public int GetHashCode(string obj) {
                return PrepeareString(obj).GetHashCode();
            }
            static string PrepeareString(string s) {
                var sb = new StringBuilder(s.Length);
                bool prevIsSpace = false;
                for (int i = 0; i < s.Length; i++) {
                    char c = s[i];
                    if (c == ' ' || c == '\t') {
                        if (!prevIsSpace) {
                            sb.Append(' ');
                            prevIsSpace = true;
                        }
                    }
                    else {
                        sb.Append(c);
                        prevIsSpace = false;
                    }
                }
                return sb.ToString();
            }
        }
    }
}