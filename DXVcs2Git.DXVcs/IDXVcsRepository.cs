using System;
using DXVCS;

namespace DXVcs2Git.DXVcs {
    public interface IDXVcsRepository {
        ProjectHistoryInfo[] GetProjectHistory(string vcsFile, bool resursive);
        FileVersionInfo[] GetFileHistory(string vcsFile, out string fileName);
        FileDiffInfo GetFileDiffInfo(string vcsFile, SpacesAction spacesAction = SpacesAction.IgnoreAll);
        FileDiffInfo GetFileDiffInfo(string vcsFile, Action<int, int> progressAction, SpacesAction spacesAction);
        void GetLatestVersion(string vcsFile, string fileName);
        void Get(string vcsFile, string fileName, int version);
        void CheckOutFile(string vcsFile, string localFile, string comment);
        void CheckInFile(string vcsFile, string localFile, string comment);
        void UndoCheckout(string vcsFile, string localFile);
        string GetFileWorkingPath(string vcsFile);
        bool IsUnderVss(string vcsFile);
        void AddFile(string vcsFile, byte[] fileBytes, string comment);
    }
}