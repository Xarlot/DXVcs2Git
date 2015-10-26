using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXVCS.Properties;
using DXVCSClient;

namespace DXVcs2Git.DXVcs {
    public class ConsoleContactWithUser : IContactWithUser, IContactWithUserAuthorizedLogon {
        bool silentMode;
        bool overwrite;
        bool merge;
        bool storeVersionInfo;
        bool keepWritable;
        string user;
        string password;
        public ConsoleContactWithUser(bool silentMode, bool overwrite, bool merge, bool storeVersionInfo, bool makeWritable, string user, string password) {
            this.silentMode = silentMode;
            this.overwrite = overwrite;
            this.merge = merge;
            this.storeVersionInfo = storeVersionInfo;
            this.keepWritable = makeWritable;
            this.user = user;
            this.password = password;
        }
        public FormMessageBoxResult AddFilesConfirm(string project, string[] files) {
            throw new NotImplementedException();
        }
        public AddToSourceControlResult AddToSourceControl(VcsClientCore vcsClientCore, string name) {
            throw new NotImplementedException();
        }
        public DXVCSDialogResult ChooseProject(VcsClientCore vcsClientCore, string currentProject, out FileLocation fileLocation) {
            throw new NotImplementedException();
        }
        public FormMessageBoxResult DeleteCheckedOutFile(string filePath) {
            throw new NotImplementedException();
        }
        public FormMessageBoxResult DeleteCheckedOutFileOne() {
            throw new NotImplementedException();
        }
        public FormMessageBoxResult DestroyItem(string item) {
            throw new NotImplementedException();
        }
        public FormMessageBoxResult FileCheckOutToAnotherFolder(string fileName, string checkOutFolder, string proceedFolder) {
            if (silentMode) {
                return overwrite ? FormMessageBoxResult.YesAll : FormMessageBoxResult.NoAll;
            }
            Console.WriteLine("Resources.CheckedOutToAnotherFolder", fileName, checkOutFolder, proceedFolder, "Y", "YA", "N", "NA", "C");
            do {
                string strResult = Console.ReadLine().ToUpperInvariant();
                switch (strResult) {
                    case "Y":
                        return FormMessageBoxResult.Yes;
                    case "YA":
                        return FormMessageBoxResult.YesAll;
                    case "N":
                        return FormMessageBoxResult.No;
                    case "NA":
                        return FormMessageBoxResult.NoAll;
                    case "C":
                        return FormMessageBoxResult.Cancel;
                }
                Console.Write("Resources.WrongAnswer");
            } while (true);
        }
        public DXVCSDialogResult CheckOutWritableFile(VcsClientCore vcsClientCore, string path, string vcsPath, FileBaseInfoState fileState, bool disableLeaveOption, ref ActionToWritableCopy action) {
            if (silentMode) {
                if (overwrite)
                    action = new ActionToWritableCopy(ActionWithCopy.Replace, true);
                else if (merge) {
                    if (fileState == FileBaseInfoState.NeedMerge)
                        action = new ActionToWritableCopy(ActionWithCopy.Merge, false);
                    else
                        action = new ActionToWritableCopy(ActionWithCopy.CheckOut, false);
                }
                else
                    action = new ActionToWritableCopy(ActionWithCopy.Leave, true);
                return DXVCSDialogResult.OK;
            }
            Console.WriteLine("Resources.ContainsWriteableCopy", path);
            if (!disableLeaveOption)
                Console.WriteLine("Resources.GetLatestLeave", 'L', "LA");
            Console.WriteLine("Resources.GetLatestReplace", 'R', "RA");
            Console.WriteLine("Resources.GetLatestCancel", 'C');
            Console.Write("?");
            do {
                string strResult = Console.ReadLine().ToUpperInvariant();
                switch (strResult) {
                    case "L":
                        if (!disableLeaveOption) {
                            action = new ActionToWritableCopy(ActionWithCopy.Leave, false);
                            return DXVCSDialogResult.OK;
                        }
                        break;
                    case "LA":
                        if (!disableLeaveOption) {
                            action = new ActionToWritableCopy(ActionWithCopy.Leave, true);
                            return DXVCSDialogResult.OK;
                        }
                        break;
                    case "R":
                        action = new ActionToWritableCopy(ActionWithCopy.Replace, false);
                        return DXVCSDialogResult.OK;
                    case "RA":
                        action = new ActionToWritableCopy(ActionWithCopy.Replace, true);
                        return DXVCSDialogResult.OK;
                    case "C":
                        action = null;
                        return DXVCSDialogResult.Cancel;
                }
                Console.Write("Resources.WrongAnswer");
            } while (true);
        }
        public DXVCSDialogResult GetLatestCheckedOutFile(VcsClientCore vcsClientCore, string path, string vcsPath, FileBaseInfoState fileState, bool disableLeaveOption, ref ActionToWritableCopy action) {
            if (silentMode) {
                action = new ActionToWritableCopy(ActionWithCopy.Leave, true);
                return DXVCSDialogResult.OK;
            };
            Console.Write("Resources.CheckedOutFileReplaceQuestion", path, 'R', "RA", 'L', "LA", 'C');
            do {
                string strResult = Console.ReadLine().ToUpperInvariant();
                switch (strResult) {
                    case "R":
                        action = new ActionToWritableCopy(ActionWithCopy.Replace, false);
                        return DXVCSDialogResult.OK;
                    case "RA":
                        action = new ActionToWritableCopy(ActionWithCopy.Replace, true);
                        return DXVCSDialogResult.OK;
                    case "L":
                        action = new ActionToWritableCopy(ActionWithCopy.Leave, false);
                        return DXVCSDialogResult.OK;
                    case "LA":
                        action = new ActionToWritableCopy(ActionWithCopy.Leave, true);
                        return DXVCSDialogResult.OK;
                    case "C":
                        return DXVCSDialogResult.Cancel;
                }
                Console.Write("Resources.WrongAnswer");
            }
            while (true);
        }
        public DXVCSDialogResult GetLatestExistingFile(VcsClientCore vcsClientCore, string path, string vcsPath, FileBaseInfoState fileState, bool fileCheckOut, bool disableLeaveOption, ref ActionToWritableCopy action) {
            if (silentMode) {
                if (overwrite)
                    action = new ActionToWritableCopy(ActionWithCopy.Replace, true);
                else
                    action = new ActionToWritableCopy(ActionWithCopy.Leave, true);
                return DXVCSDialogResult.OK;
            }
            Console.WriteLine("Resources.ContainsWriteableCopy", path);
            if (!disableLeaveOption) {
                Console.WriteLine("Resources.GetLatestLeave", 'L', "LA");
            }
            Console.WriteLine("Resources.GetLatestReplace", 'R', "RA");
            if (!fileCheckOut) {
                Console.WriteLine("Resources.GetLatestCheckOut", 'K', "KA");
            }
            Console.WriteLine("Resources.GetLatestCancel", 'C');
            Console.Write("?");
            do {
                string strResult = Console.ReadLine().ToUpperInvariant();
                switch (strResult) {
                    case "L":
                        if (!disableLeaveOption) {
                            action = new ActionToWritableCopy(ActionWithCopy.Leave, false);
                            return DXVCSDialogResult.OK;
                        }
                        break;
                    case "LA":
                        if (!disableLeaveOption) {
                            action = new ActionToWritableCopy(ActionWithCopy.Leave, true);
                            return DXVCSDialogResult.OK;
                        }
                        break;
                    case "R":
                        action = new ActionToWritableCopy(ActionWithCopy.Replace, false);
                        return DXVCSDialogResult.OK;
                    case "RA":
                        action = new ActionToWritableCopy(ActionWithCopy.Replace, true);
                        return DXVCSDialogResult.OK;
                    case "K":
                        if (!fileCheckOut) {
                            action = new ActionToWritableCopy(ActionWithCopy.CheckOut, false);
                            return DXVCSDialogResult.OK;
                        }
                        break;
                    case "KA":
                        if (!fileCheckOut) {
                            action = new ActionToWritableCopy(ActionWithCopy.CheckOut, true);
                            return DXVCSDialogResult.OK;
                        }
                        break;
                    case "C":
                        action = null;
                        return DXVCSDialogResult.Cancel;
                }
                Console.Write("Resources.WrongAnswer");
            } while (true);
        }
        public FormMessageBoxResult ShareFileExists(string fileName) {
            throw new NotImplementedException();
        }
        public DXVCSDialogResult ShowFolderBrowserDialog(string startFolder, out string resultFolder) {
            throw new NotImplementedException();
        }
        class ConsoleMessageBoxItem {
            public string ConsoleKey;
            public FormMessageBoxResult Result;
            public ConsoleMessageBoxItem(string consoleKey, FormMessageBoxResult result) {
                ConsoleKey = consoleKey;
                Result = result;
            }
        }
        ConsoleMessageBoxItem[] allFormMessageBoxItem = new ConsoleMessageBoxItem[] {
            new ConsoleMessageBoxItem("Y", FormMessageBoxResult.Yes),
            new ConsoleMessageBoxItem("YA", FormMessageBoxResult.YesAll),
            new ConsoleMessageBoxItem("N", FormMessageBoxResult.No),
            new ConsoleMessageBoxItem("NA", FormMessageBoxResult.NoAll),
            new ConsoleMessageBoxItem("C", FormMessageBoxResult.Cancel),
            new ConsoleMessageBoxItem("E", FormMessageBoxResult.Error),
            new ConsoleMessageBoxItem("H", FormMessageBoxResult.Help)
        };
        public FormMessageBoxResult ShowFormMessageBox(string message, string caption, FormMessageBoxResult buttons, DXVCSMessageBoxIcon icon) {
            if (silentMode) {
                if ((buttons & (FormMessageBoxResult.Yes | FormMessageBoxResult.No | FormMessageBoxResult.YesAll | FormMessageBoxResult.NoAll)) == buttons)
                    return overwrite ? FormMessageBoxResult.Yes : FormMessageBoxResult.No;
                else
                    throw new Exception("Interaction need");
            }
            List<ConsoleMessageBoxItem> showItems = new List<ConsoleMessageBoxItem>();
            foreach (ConsoleMessageBoxItem item in allFormMessageBoxItem) {
                if ((item.Result & buttons) == item.Result)
                    showItems.Add(item);
            }
            Console.WriteLine("{0} - {1}", caption, message);
            foreach (ConsoleMessageBoxItem item in showItems) {
                Console.WriteLine("\t{0} : {1}", item.Result.ToString(), item.ConsoleKey);
            }
            Console.Write("?");
            do {
                string strResult = Console.ReadLine().ToUpperInvariant();
                foreach (ConsoleMessageBoxItem item in showItems) {
                    if (strResult == item.ConsoleKey)
                        return item.Result;
                }
                Console.Write("Resources.WrongAnswer");
            }
            while (true);
        }
        public FormRecoveryExistingFileResult ShowFormRecoveryExistingFile(string fileName) {
            throw new NotImplementedException();
        }
        public void ShowMessage(string message, string caption, DXVCSMessageBoxIcon icon) {
            Console.WriteLine("{0} - {1}", caption, message);
            if (!silentMode)
                Console.ReadKey();
        }
        public FormMessageBoxResult UndoModifiedFile(string filePath) {
            if (silentMode) {
                return overwrite ? FormMessageBoxResult.YesAll : FormMessageBoxResult.NoAll;
            }
            Console.WriteLine("Resources.UndoModifiedFile", filePath, "Y", "YA", "N", "NA", "C");
            do {
                string strResult = Console.ReadLine().ToUpperInvariant();
                switch (strResult) {
                    case "Y":
                        return FormMessageBoxResult.Yes;
                    case "YA":
                        return FormMessageBoxResult.YesAll;
                    case "N":
                        return FormMessageBoxResult.No;
                    case "NA":
                        return FormMessageBoxResult.NoAll;
                    case "C":
                        return FormMessageBoxResult.Cancel;
                }
                Console.Write("Resources.WrongAnswer");
            } while (true);
        }
        public FormMessageBoxResult FileCheckOutToAnotherHost(string fileName, string host, bool mayProceed) {
            if (mayProceed) {
                if (silentMode) {
                    return overwrite ? FormMessageBoxResult.YesAll : FormMessageBoxResult.NoAll;
                }
                Console.WriteLine("Resources.CheckedOutToAnotherHostProceed", host);
                do {
                    string strResult = Console.ReadLine().ToUpperInvariant();
                    switch (strResult) {
                        case "Y":
                            return FormMessageBoxResult.Yes;
                        case "YA":
                            return FormMessageBoxResult.YesAll;
                        case "N":
                            return FormMessageBoxResult.No;
                        case "NA":
                            return FormMessageBoxResult.NoAll;
                    }
                    Console.Write("Resources.WrongAnswer");
                } while (true);
            }
            else {
                Console.WriteLine("Resources.CheckedOutToAnotherHostSkip", host);
                return FormMessageBoxResult.OK;
            }
        }
        public bool CheckCaps(ContactWithUserCapabilities caps) {
            if ((caps | ContactWithUserCapabilities.VersionStorage) == ContactWithUserCapabilities.VersionStorage) {
                return storeVersionInfo;
            }
            if ((caps | ContactWithUserCapabilities.KeepWritableAlways) == ContactWithUserCapabilities.KeepWritableAlways) {
                return keepWritable;
            }
            return false;
        }
        public bool StartCustomMerge(string leftTitle, string baseTitle, string rightTitle, byte[] leftData, byte[] baseData, byte[] rightData, string resultPath) {
            return false;
        }

        public FormMessageBoxResult FileCheckOut(string fileName, bool mayProceed) {
            throw new NotImplementedException();
        }

        public FormAddFileResult AddFile(string fileName, bool removeLocalCopyDefault) {
            return new FormAddFileResult(string.Empty, false, true);
        }


        #region IContactWithUser Members


        public bool LogOn(ref string auxPath) {
            throw new Exception("Can't connect to DXVCSService");
        }

        #endregion

        #region IContactWithUser Members


        public void ShowException(Exception ex) {
            throw new NotImplementedException();
        }

        #endregion

        #region IContactWithUserAuthorizedLogon Members

        int logOnCounter = 0;
        public bool LogOn(ref string auxPath, ref string user, ref string password) {
            if (logOnCounter != 0) {
                throw new Exception("Can't connect to DXVCSService");
            }
            user = this.user;
            password = this.password;
            logOnCounter++;
            return true;
        }

        #endregion
    }

}
