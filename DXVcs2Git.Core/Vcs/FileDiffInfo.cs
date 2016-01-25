using System;
using System.Collections.Generic;
using System.Text;
using DXVCS;
using DXVcsTools.Data;

namespace DXVcs2Git.DXVcs {
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