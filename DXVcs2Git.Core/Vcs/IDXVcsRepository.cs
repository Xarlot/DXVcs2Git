using System;
using System.Collections.Generic;
using DXVCS;

namespace DXVcs2Git.DXVcs {
    public interface IDXVcsRepository {
        IList<ProjectHistoryInfo> GetProjectHistory(string vcsFile, bool resursive, DateTime? from = null, DateTime? to = null);
        FileVersionInfo[] GetFileHistory(string vcsFile);
        FileDiffInfo GetFileDiffInfo(string vcsFile, SpacesAction spacesAction = SpacesAction.IgnoreAll);
        FileDiffInfo GetFileDiffInfo(string vcsFile, Action<int, int> progressAction, SpacesAction spacesAction);
        void GetProject(string vcsPath, string localPath, DateTime timeStamp);
        void GetLatestFileVersion(string vcsFile, string fileName);
        void Get(string vcsFile, string fileName, int version);
        void CheckOutFile(string vcsFile, string localFile, string comment, bool dontGetLocalCopy = false);
        void CheckInFile(string vcsFile, string localFile, string comment);
        void UndoCheckout(string vcsFile, string localFile);
        string GetFileWorkingPath(string vcsFile);
        bool IsUnderVss(string vcsFile);
        bool IsCheckedOut(string vcsPath);
        bool IsCheckedOutByMe(string vcsPath);
        void AddFile(string vcsFile, byte[] fileBytes, string comment);
        void AddProject(string vcsPath, string comment);
        void CreateLabel(string vcsPath, string labelName, string comment);
        void DeleteFile(string vcsPath);
        void MoveFile(string vcsPath, string newVcsPath, string comment);
    }
}