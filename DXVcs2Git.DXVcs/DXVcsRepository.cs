using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using DXVCS;
using DXVCS.Properties;
using DXVCSClient;

namespace DXVcs2Git.DXVcs {
    class DXVcsRepository : IDXVcsRepository {
        readonly string serviceUrl;
        readonly DXVcsServiceProvider serviceProvider;
        readonly FileSystem fileSystem;

        IDXVCSService Service {
            get { return serviceProvider.GetService(serviceUrl); }
        }

        internal DXVcsRepository(string serviceUrl) {
            this.serviceProvider = new DXVcsServiceProvider();
            this.serviceUrl = serviceUrl;
            fileSystem = new FileSystem();

            ValidateService();
        }
        void ValidateService() {
            bool isAdmin;
            var service = Service;
            if (service == null)
                throw new ArgumentNullException("service");
            if (!service.IsCorrectUser(out isAdmin))
                throw new ApplicationException("Invalid user name");
        }

        public FileVersionInfo[] GetFileHistory(string vcsFile, out string fileName) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");

            fileName = Path.GetFileName(vcsFile);

            var fileHistory = new FileHistory(vcsFile, Service);
            var result = new List<FileVersionInfo>(fileHistory.Count);
            result.AddRange(fileHistory);
            result.Reverse();
            return result.ToArray();
        }

        public FileDiffInfo GetFileDiffInfo(string vcsFile, SpacesAction spacesAction = SpacesAction.IgnoreAll) {
            return GetFileDiffInfo(vcsFile, null, spacesAction);
        }

        public FileDiffInfo GetFileDiffInfo(string vcsFile, Action<int, int> progressAction, SpacesAction spacesAction) {
            var history = new FileHistory(vcsFile, Service);
            FileDiffInfo diffInfo = new FileDiffInfo(history.Count);
            diffInfo.SpacesAction = spacesAction;

            int index = 0;
            foreach (FileVersionInfo fileVersionInfo in history) {
                if (progressAction != null)
                    progressAction(index, history.Count);

                diffInfo.AddItem(index, fileVersionInfo);
                index++;
            }
            return diffInfo;
        }
        public IList<ProjectHistoryInfo> GetProjectHistory(string vcsPath, bool recursive, DateTime? from = null, DateTime? to = null) {
            if (string.IsNullOrEmpty(vcsPath))
                throw new ArgumentException("vcsPath");

            int maxState;
            string id = Service.GetProjectHistoryAsync(vcsPath, recursive, out maxState);
            var request = PrepareProjectHistoryRequest(from ?? DateTime.MinValue, to ?? DateTime.Now, string.Empty, string.Empty, HistoryItems.AllItems, true);
            List<ProjectHistoryInfo> infos = new List<ProjectHistoryInfo>();
            Service.GetProjectHistoryRequest(id, request);
            int state;
            while (true) {
                ProjectHistoryInfo[] info;
                bool last = Service.GetProjectHistoryNext(id, out info, out state);
                if (info != null) {
                    infos.AddRange(info);
                }
                if (!last)
                    break;
            }
            return infos;
        }
        ProjectHistoryRequest PrepareProjectHistoryRequest(DateTime from, DateTime to, string findUser, string findComment, HistoryItems whatItems, bool showFileHistory) {
            ProjectHistoryRequest request = new ProjectHistoryRequest();
            request.FindUser = string.IsNullOrEmpty(findUser) ? null : findUser;
            request.FindComment = string.IsNullOrEmpty(findComment) ? null : findComment;
            request.HideProjectHistory = whatItems == HistoryItems.Labels;
            request.ShowFileHistory = !request.HideProjectHistory && showFileHistory;
            request.ShowLabels = whatItems != HistoryItems.WithoutLabels;
            request.From = from;
            request.To = to;
            return request;
        }
        public enum HistoryItems {
            AllItems = 0x00,
            Labels = 0x01,
            WithoutLabels = 0x02
        };
        public void GetLatestFileVersion(string vcsFile, string fileName) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("path");

            byte[] data = DXVCSHelpers.TryToDecompressData(Service.GetFileData(vcsFile, null));
            File.WriteAllBytes(fileName, data);
        }

        public void Get(string vcsFile, string fileName, int version) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("path");

            if (version <= 0)
                throw new ArgumentException("version");

            byte[] data = DXVCSHelpers.TryToDecompressData(Service.GetFileData(vcsFile, version));
            File.WriteAllBytes(fileName, data);
        }
        public void GetProject(string vcsPath, string localPath, DateTime timeStamp) {
            if (string.IsNullOrEmpty(vcsPath))
                throw new ArgumentException("vcsFile");
            using (VcsClientCore vcsClientCore = new VcsClientCore(new FileSystem(), new ConsoleContactWithUser(true, true, false, true, true, null, null))) {
                if (!VcsLogOn(vcsClientCore))
                    return;
                AsyncWorks async = new AsyncWorks(vcsClientCore);
                Thread thread = async.GetLatestVersionThreaded(localPath, vcsPath, true, timeStamp.ToUniversalTime(), true, true, ReplaceWriteable.Default, FileTime.Current, true);
                thread.Join();
                //vcsClientCore.GetLatestVersion(localPath, vcsPath, true, timeStamp, true, true, ReplaceWriteable.Replace, FileTime.Modification, new VcsClientBatchState());
            }
        }
        bool VcsLogOn(VcsClientCore vcsClientCore) {
            try {
                vcsClientCore.LogOn(DefaultConfig.Config.AuxPath, false, false);
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        public void CheckOutFile(string vcsFile, string localFile, string comment) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");

            if (string.IsNullOrEmpty(localFile))
                throw new ArgumentException("localFile");

            bool getLocalCopy = !File.Exists(localFile) || !Service.GetFile(vcsFile).CheckedOutMe;
            Service.CheckOut(Environment.MachineName, new[] { vcsFile }, new[] { Path.GetDirectoryName(localFile) }, new[] { comment }, null);

            if (File.Exists(localFile))
                File.SetAttributes(localFile, FileAttributes.Normal);

            if (getLocalCopy) {
                GetLatestFileVersion(vcsFile, localFile);
            }
        }

        public void CheckInFile(string vcsFile, string localFile, string comment) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");

            if (string.IsNullOrEmpty(localFile))
                throw new ArgumentException("localFile");

            if (!Service.GetFile(vcsFile).CheckedOutMe)
                throw new InvalidOperationException("Can't check-in: the file is not checked out: " + vcsFile);

            var data = new byte[1][] { File.ReadAllBytes(localFile) };
            string result = Service.CheckIn(new[] { vcsFile }, data, new[] { File.GetLastWriteTimeUtc(localFile) }, new[] { comment }, false);
            File.SetAttributes(localFile, File.GetAttributes(localFile) | FileAttributes.ReadOnly);
        }

        public string GetFileWorkingPath(string vcsFile) {
            string workingFolder = GetWorkingFolder(Path.GetDirectoryName(vcsFile).Replace("\\", "/"));
            if (string.IsNullOrEmpty(workingFolder))
                return null;

            return Path.Combine(workingFolder, Path.GetFileName(vcsFile));
        }
        string GetWorkingFolder(string vcsProject) {
            if (string.IsNullOrEmpty(vcsProject))
                throw new ArgumentException("vcsProject");

            return Service.GetWorkingFolder(Environment.MachineName, vcsProject);
        }
        public void UndoCheckout(string vcsFile, string localFile) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");
            if (string.IsNullOrEmpty(localFile))
                throw new ArgumentException("localFile");

            if (!Service.GetFile(vcsFile).CheckedOutMe)
                throw new InvalidOperationException("Can't undo check out: the file is not checked out: " + vcsFile);
            Service.UndoCheckOut(new[] { vcsFile }, new[] { false });
        }
        public void AddFile(string vcsFile, byte[] fileBytes, string comment) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");
            var folders = vcsFile.Split(@"/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var queue = new Queue<string>(folders);
            string temp = queue.Dequeue();
            while (queue.Count > 0) {
                string folder = queue.Dequeue();
                if (queue.Count == 0)
                    CreateFile(temp, folder, fileBytes, comment);
                else
                    CreateProject(temp, folder, comment);
                temp += @"/" + folder;
            }
        }
        void CreateFile(string vcsFile, string fileName, byte[] fileBytes, string comment) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("fileName");
            Service.CreateFile(vcsFile, fileName, fileBytes, DateTime.Now, comment);
        }
        void CreateProject(string vcsFile, string name, string comment) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");
            if (!IsUnderVss(vcsFile))
                Service.CreateProject(vcsFile, name, comment, false);
        }
        public bool IsUnderVss(string vcsFile) {
            if (string.IsNullOrEmpty(vcsFile))
                throw new ArgumentException("vcsFile");

            var project = Service.FindProject(vcsFile);
            if (!project.IsNull)
                return true;
            var file = Service.FindFile(vcsFile);
            return !file.IsNull;
        }
    }
}