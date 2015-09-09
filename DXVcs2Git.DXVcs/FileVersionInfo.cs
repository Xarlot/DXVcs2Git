using System;

namespace DXVcsTools.DXVcsClient {
    public class FileVersionInfoBase {
        public FileVersionInfoBase(int version, DateTime date, string user, string comment) {
            if (version < 0)
                throw new ArgumentOutOfRangeException("version");

            // dx repository contains some revisions with no user. bug, actually, but we have to workaround it.
            //if (string.IsNullOrEmpty(user))
            //    throw new ArgumentException("user");

            Version = version;
            Date = date;
            User = user;
            Comment = comment;
        }
        public FileVersionInfoBase(FileVersionInfoBase copy) : this(copy.Version, copy.Date, copy.User, copy.Comment) {
        }
        public int Version { get; private set; }
        public DateTime Date { get; private set; }
        public string User { get; private set; }
        public string Comment { get; private set; }
    }

    public class FileVersionInfo : FileVersionInfoBase {
        public FileVersionInfo(int version, DateTime date, string user, string comment, byte[] data) : base(version, date, user, comment) {
            if (data == null)
                throw new ArgumentNullException("data");

            Data = data;
        }
        public byte[] Data { get; private set; }
    }
}