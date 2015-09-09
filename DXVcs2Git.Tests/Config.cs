using System;
using System.IO;
using System.Reflection;
using LibGit2Sharp;

namespace DXVcs2Git.Tests {
    public static class Config {
        static string defaultFolder;
        public static string DefaultFolder { get { return defaultFolder ?? (defaultFolder = Path.GetTempPath() + Path.DirectorySeparatorChar + Guid.NewGuid()); } }

        public static readonly Identity Identity = new Identity("DXVcs2GitTester", "maximatorr3@gmail.com");
    }
}
