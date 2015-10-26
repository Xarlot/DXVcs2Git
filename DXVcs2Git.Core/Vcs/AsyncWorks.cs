using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DXVCS;
using DXVCS.Properties;

namespace DXVCSClient {
	public class AsyncWorks {
        public VcsClientCore vcsClientCore;
		public AsyncWorks(VcsClientCore vcsClientCore) {
			this.vcsClientCore = vcsClientCore;
		}
        public Thread DeleteProjectThreaded(string path) {
            return DeleteProjectThreaded(path, true);
        }
        public Thread DeleteProjectThreaded(string path, bool immediatelyStart) {
            if(vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.DeleteProject(path);
            };
            return CreateThread(work, immediatelyStart);
        }
		public Thread DestroyThreaded(string path) {
			return DestroyThreaded(path, true);
		}
		public Thread DestroyThreaded(string path, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.Destroy(path);
			};
            return CreateThread(work, immediatelyStart);
		}
		public Thread DestroyThreaded(string[] path) {
            return DestroyThreaded(path, true);
		}
		public Thread DestroyThreaded(string[] path, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.Destroy(path);
			};
            return CreateThread(work, immediatelyStart);
		}
		public Thread ShareProjectThreaded(string projectPath, string shareProjectPath, string newProjectName, string comment) {
            return ShareProjectThreaded(projectPath, shareProjectPath, newProjectName, comment, true);
		}
		public Thread ShareProjectThreaded(string projectPath, string shareProjectPath, string newProjectName, string comment, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.ShareProject(projectPath, shareProjectPath, newProjectName, comment);
			};
            return CreateThread(work, immediatelyStart);

		}

		public Thread AddFilesThreaded(string projectPath, string[] fileLocalPaths) {
            return AddFilesThreaded(projectPath, fileLocalPaths, true);
		}
		public Thread AddFilesThreaded(string projectPath, string[] fileLocalPaths, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.AddFiles(projectPath, fileLocalPaths);
			};
            return CreateThread(work, immediatelyStart);

		}

		public Thread ShareAndBranchProjectThreaded(string projectPath, string shareProjectPath, string newName, bool isRecursive, string comment) {
            return ShareAndBranchProjectThreaded(projectPath, shareProjectPath, newName, isRecursive, comment, true);
		}
        public Thread ShareAndBranchProjectThreaded(string projectPath, string shareProjectPath, string newName, bool isRecursive, string comment, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.BranchProject(projectPath, shareProjectPath, newName, isRecursive, comment);
			};
            return CreateThread(work, immediatelyStart);

		}

        public Thread BranchFilesThreaded(string[] filePaths, string comment) {
            return BranchFilesThreaded(filePaths, comment, true);
        }
        public Thread BranchFilesThreaded(string[] filePaths, string comment, bool immediatelyStart) {
            if(vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.BranchFiles(filePaths, comment);
            };
            return CreateThread(work, immediatelyStart);

        }


        public Thread ShareAndBranchFilesThreaded(string[] filePaths, string shareProjectPath, string comment) {
            return ShareAndBranchFilesThreaded(filePaths, shareProjectPath, comment, true);
		}
        public Thread ShareAndBranchFilesThreaded(string[] filePaths, string shareProjectPath, string comment, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.ShareAndBranchFiles(filePaths, shareProjectPath, comment);
			};
            return CreateThread(work, immediatelyStart);

		}
        public Thread ShareProjectRecursiveThreaded(string projectPath, string shareProjectPath, string newProjectName, string comment) {
            return ShareProjectRecursiveThreaded(projectPath, shareProjectPath, newProjectName, comment, true);
		
        }

        public Thread ShareProjectRecursiveThreaded(string projectPath, string shareProjectPath, string newProjectName, string comment, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.ShareProjectRecursive(projectPath, shareProjectPath, newProjectName, comment);
			};
			return CreateThread(work, immediatelyStart);
		}

        public Thread AddFilesThreaded(FileDataLocation[] filesInfo, string commonComment) {
            return AddFilesThreaded(filesInfo, commonComment, true);
		}
        public Thread AddFilesThreaded(FileDataLocation[] filesInfo, string commonComment, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.AddFiles(filesInfo, commonComment);
			};
            return CreateThread(work, immediatelyStart);
		}

        public Thread AddFilesRecursiveThreaded(List<string> localFolderPath, string rootLocalPath, string projectPath, string commonComment, IFileSystem fileSystem) {
            return AddFilesRecursiveThreaded(localFolderPath, rootLocalPath, projectPath, commonComment, fileSystem, true);
		}
        public Thread AddFilesRecursiveThreaded(List<string> localFolderPath, string rootLocalPath, string projectPath, string commonComment, IFileSystem fileSystem, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.AddFilesRecursive(rootLocalPath, projectPath, commonComment, fileSystem);
			};
            return CreateThread(work, immediatelyStart);
		}

		public Thread DeleteFilesThreaded(string[] filePaths) {
            return DeleteFilesThreaded(filePaths, true);
		}
		public Thread DeleteFilesThreaded(string[] filePaths, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.DeleteFiles(filePaths);
			};
            return CreateThread(work, immediatelyStart);

		}


		public Thread CheckInThreaded(FileLocation[] filesInfo, string comment, CheckinOption option) {
            return CheckInThreaded(filesInfo, comment, option, true);
		}
		public Thread CheckInThreaded(FileLocation[] filesInfo, string comment, CheckinOption option, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.CheckIn(filesInfo, comment, option);
			};
            return CreateThread(work, immediatelyStart);
		}

		public Thread GetLatestVersionThreaded(string localProjectPath, string projectPath, bool buildTree, object option, bool recursive, bool makeWriteable, ReplaceWriteable replaceWriteableState, FileTime fileTimeState) {
			return GetLatestVersionThreaded(localProjectPath, projectPath, buildTree, option, recursive, makeWriteable, replaceWriteableState, fileTimeState, true);
		}
        public Thread GetLatestVersionThreaded(string localProjectPath, string projectPath, bool buildTree, object option, bool recursive, bool makeWriteable, ReplaceWriteable replaceWriteableState, FileTime fileTimeState, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.GetLatestVersion(localProjectPath, projectPath, buildTree, option, recursive, makeWriteable, replaceWriteableState, fileTimeState);
			};
            return CreateThread(work, immediatelyStart);
		}

        public Thread GetLatestVersionFilesThreaded(FileLocation[] filesLocation, bool makeWriteable, ReplaceWriteable replaceWriteableState, FileTime fileTimeState) {
            return GetLatestVersionFilesThreaded(filesLocation, makeWriteable,replaceWriteableState, fileTimeState, true);
		}
        public Thread GetLatestVersionFilesThreaded(FileLocation[] filesLocation, bool makeWriteable, ReplaceWriteable replaceWriteableState, FileTime fileTimeState, bool immediatelyStart) {
			if (vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = null;
                return vcsClientCore.GetLatestVersionFiles(filesLocation, makeWriteable, replaceWriteableState, fileTimeState);
			};
            return CreateThread(work, immediatelyStart);
		}
        public Thread GetProjectHistoryRequestThreaded(ProjectHistorySessionInfo session, ProjectHistoryRequest request) {
            return GetProjectHistoryRequestThreaded(session, request, true);
        }

        public Thread GetProjectHistoryRequestThreaded(ProjectHistorySessionInfo session, ProjectHistoryRequest request, bool immediatelyStart) {
            if(vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                data = vcsClientCore.GetProjectHistoryRequest(session, request);
                return true;
            };
            return CreateThread(work, immediatelyStart);
        }

        public Thread GetCheckedOutFilesThreaded(string rootProject, bool isRecursive, string checkedOutUserToFilter, bool immediatelyStart) {
            if(vcsClientCore == null) throw new NullReferenceException();
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                FileBaseInfo[] filesToShow = vcsClientCore.GetFileBaseInfoCheckedOut(rootProject, checkedOutUserToFilter, isRecursive);
                data = filesToShow;
                return true;
            };
            return CreateThread(work, immediatelyStart);
        }
        public Thread GetCheckedOutFilesThreaded(FileBaseInfo[] files, string checkedOutUserToFilter, bool immediatelyStart) {
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                FileBaseInfo[] filesToShow = vcsClientCore.GetFileBaseInfoCheckedOut(files, checkedOutUserToFilter);
                data = filesToShow;
                return true;
            };
            return CreateThread(work, immediatelyStart);
        }
        public Thread GetFilesWithWildcardThreaded(bool isRecursive, string rootProject, string wildcard, bool immediatelyStart) {
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                FileBaseInfo[] filesToShow = vcsClientCore.GetFileBaseInfoWithWildcard(vcsClientCore.GetFileBaseInfo(rootProject, isRecursive), DXVCSHelpers.CreatePattern(wildcard));
                data = filesToShow;
                return true;
            };
            return CreateThread(work, immediatelyStart);
        }

        public Thread GetFilesBaseInfo(string[] filePaths, bool immediatelyStart) {
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                FileBaseInfo[] filesToShow = vcsClientCore.GetFileBaseInfo(filePaths);
                data = filesToShow;
                return true;
            };
            return CreateThread(work, immediatelyStart);
        }

        public Thread GetFileMergeList(string projectLeft, string projectRight, bool includeDirs, bool recursive, bool immediatelyStart) {
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                List<FileMergeInfo> filesToShow = vcsClientCore.GetFileMergeList(projectLeft, projectRight, includeDirs, recursive);
                data = filesToShow.ToArray();
                return true;
            };
            return CreateThread(work, immediatelyStart);
        }

        public Thread GetFileMergeStatus(Dictionary<string, string> fileFullVCSPaths, bool immediatelyStart) {
            ProgressEvents.ThreadWorkHandler work = delegate(out object data) {
                List<FileMergeStatus> filesToShow = new List<FileMergeStatus>(vcsClientCore.GetMergeStatus(fileFullVCSPaths));
                data = filesToShow.ToArray();
                return true;
            };
            return CreateThread(work, immediatelyStart);
        }

        public Thread CreateThread(ProgressEvents.ThreadWorkHandler work) {
            return CreateThread(work, true);
		}
        public Thread CreateThread(ProgressEvents.ThreadWorkHandler work, bool immediatelyStart) {
			Thread thread = new Thread(delegate() {
                try {
                    object data;
                    bool result = work(out data);
                    if(ProgressEvents.OnThreadOperationCompleted != null) {
                        ProgressEvents.OnThreadOperationCompleted(this, new OperationCompletedEventArgs(result, data));
                    }
                } 
                catch(Exception ex) {
                    if(ProgressEvents.OnProcessThreadException != null) {
                        ProgressEvents.OnProcessThreadException(this, new ThreadExceptionEventArgs(ex));
                    }
                }
			});
            if (immediatelyStart) {
				thread.Start();
			}
			return thread;
		}
	}
}
